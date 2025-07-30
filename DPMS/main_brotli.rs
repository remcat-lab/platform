use std::fs;
use std::io::{BufWriter};
use std::path::{Path, PathBuf};
use std::time::{SystemTime, UNIX_EPOCH};

use brotli::CompressorWriter;
use futures_util::stream::{self, StreamExt};
use serde::Deserialize;
use tar::Builder;
use tokio::io::{AsyncReadExt, DuplexStream};
use tokio_util::io::ReaderStream;
use walkdir::WalkDir;

#[derive(Debug)]
struct FileMeta {
    path: String,
    size: u64,
    mtime: u128,
}

fn normalize_path(path: &Path, base: &Path) -> String {
    path.strip_prefix(base)
        .unwrap()
        .components()
        .map(|c| c.as_os_str().to_string_lossy())
        .collect::<Vec<_>>()
        .join("/")
}

fn collect_files(base_dir: &Path) -> Vec<FileMeta> {
    WalkDir::new(base_dir)
        .into_iter()
        .filter_map(Result::ok)
        .filter(|e| e.file_type().is_file())
        .map(|entry| {
            let path = entry.path();
            let metadata = fs::metadata(path).unwrap();
            let modified = metadata.modified().unwrap_or(SystemTime::UNIX_EPOCH);
            let mtime = modified.duration_since(UNIX_EPOCH).unwrap().as_millis();

            FileMeta {
                path: normalize_path(path, base_dir),
                size: metadata.len(),
                mtime,
            }
        })
        .collect()
}

#[derive(Debug, Deserialize)]
struct NeededFiles {
    needed_files: Vec<String>,
}

async fn request_needed_files(server_url: &str, csv_path: &Path) -> Result<Vec<String>, Box<dyn std::error::Error>> {
    let client = reqwest::Client::new();
    let form = reqwest::multipart::Form::new()
        .file("csv", csv_path)?;

    let resp = client.post(&format!("{}/apis/api/check_csv", server_url))
        .multipart(form)
        .send()
        .await?;

    let needed: NeededFiles = resp.json().await?;
    Ok(needed.needed_files)
}

/// tar + brotli를 async 스트림으로 만들어 DuplexStream의 쓰기쪽에 쓰고, 읽기쪽을 reqwest 바디로 사용
async fn create_tar_brotli_stream(base_dir: &Path, files: &[String]) -> tokio::io::DuplexStream {
    let (mut writer, reader) = tokio::io::duplex(64 * 1024);

    // tar + brotli를 쓰는 별도 태스크
    let base_dir = base_dir.to_path_buf();
    let files = files.to_owned();
    tokio::spawn(async move {
        let buf_writer = BufWriter::new(writer);
        let mut brotli_writer = CompressorWriter::new(buf_writer, 4096, 5, 22);
        let mut tar_builder = Builder::new(&mut brotli_writer);

        for f in files {
            let full_path = base_dir.join(&f);
            if full_path.exists() {
                if let Err(e) = tar_builder.append_path_with_name(&full_path, &f) {
                    eprintln!("tar append error: {}", e);
                }
            }
        }
        tar_builder.finish().unwrap();
        brotli_writer.flush().unwrap();
        // writer drop -> 스트림 종료
    });

    reader
}

async fn upload_tar_stream(server_url: &str, base_dir: &Path, files: &[String]) -> Result<(), Box<dyn std::error::Error>> {
    let stream = create_tar_brotli_stream(base_dir, files).await;

    let body = reqwest::Body::wrap_stream(ReaderStream::new(stream));

    let client = reqwest::Client::new();
    let res = client.post(&format!("{}/apis/api/upload", server_url))
        .body(body)
        .send()
        .await?;

    println!("Upload response: {}", res.status());
    Ok(())
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let base_dir = PathBuf::from("./test-data");
    let csv_path = PathBuf::from("./file_list.csv");
    let server_url = "http://server.com:8080";

    let files = collect_files(&base_dir);

    // CSV 생성
    {
        let mut file = std::fs::File::create(&csv_path)?;
        writeln!(file, "path,size,mtime")?;
        for f in &files {
            writeln!(file, "{},{},{}", f.path, f.size, f.mtime)?;
        }
    }

    // 서버에 CSV 보내서 필요한 파일 리스트 받아오기
    let needed = request_needed_files(server_url, &csv_path).await?;

    if needed.is_empty() {
        println!("No files to upload");
        return Ok(());
    }

    // tar + brotli 스트림 생성 + 업로드
    upload_tar_stream(server_url, &base_dir, &needed).await?;

    Ok(())
}
