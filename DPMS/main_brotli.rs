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

/// 파일 정보를 담는 구조체
#[derive(Debug)]
struct FileMeta {
    path: String,  // 기준 경로 기준 상대 경로 ("/" 구분자)
    size: u64,     // 파일 크기(바이트)
    mtime: u128,   // 수정 시간 (UNIX epoch ms 단위)
}

/// 절대경로에서 base 경로를 제거하고 "/" 구분자로 된 상대경로 문자열로 변환
fn normalize_path(path: &Path, base: &Path) -> String {
    path.strip_prefix(base)
        .unwrap()
        .components()
        .map(|c| c.as_os_str().to_string_lossy())
        .collect::<Vec<_>>()
        .join("/")
}

/// base_dir 기준으로 하위 모든 파일 탐색 후 FileMeta 리스트 수집
fn collect_files(base_dir: &Path) -> Vec<FileMeta> {
    WalkDir::new(base_dir)
        .into_iter()
        .filter_map(Result::ok)                  // WalkDir 오류 무시
        .filter(|e| e.file_type().is_file())    // 파일만 선택
        .map(|entry| {
            let path = entry.path();
            let metadata = fs::metadata(path).unwrap();
            // 수정 시간 취득 (없으면 UNIX_EPOCH)
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

/// 서버에서 필요한 파일 리스트를 받기 위한 JSON 역직렬화 구조체
#[derive(Debug, Deserialize)]
struct NeededFiles {
    needed_files: Vec<String>,
}

/// 서버에 CSV 파일을 multipart/form-data로 POST 하여 필요한 파일 목록을 받아오는 비동기 함수
async fn request_needed_files(server_url: &str, csv_path: &Path) -> Result<Vec<String>, Box<dyn std::error::Error>> {
    let client = reqwest::Client::new();

    // CSV 파일을 multipart 폼에 첨부
    let form = reqwest::multipart::Form::new()
        .file("csv", csv_path)?;

    // 서버에 POST 요청 및 응답 JSON 파싱
    let resp = client.post(&format!("{}/apis/api/check_csv", server_url))
        .multipart(form)
        .send()
        .await?;

    let needed: NeededFiles = resp.json().await?;
    Ok(needed.needed_files)
}

/// base_dir 내 files 리스트를 tar로 묶고 brotli 압축하여 tokio::io::DuplexStream의
/// 쓰기 측에 기록하고, 읽기 측을 반환하는 비동기 함수
/// 반환된 reader는 HTTP 바디 스트림으로 활용 가능
async fn create_tar_brotli_stream(base_dir: &Path, files: &[String]) -> tokio::io::DuplexStream {
    // 듀플렉스 스트림 생성, 버퍼 크기 64KB
    let (mut writer, reader) = tokio::io::duplex(64 * 1024);

    // 소유권 복사
    let base_dir = base_dir.to_path_buf();
    let files = files.to_owned();

    // 별도 비동기 태스크에서 tar + brotli 생성 및 쓰기 수행
    tokio::spawn(async move {
        // tokio::io::DuplexStream의 쓰기 쪽은 동기 Write를 지원하지 않으므로 BufWriter로 감싸서 버퍼링
        let buf_writer = BufWriter::new(writer);

        // brotli 압축기 생성 (버퍼 크기 4KB, 압축 레벨 5, lgwin=22)
        let mut brotli_writer = CompressorWriter::new(buf_writer, 4096, 5, 22);

        // tar 아카이브 작성기 생성, 압축기 앞단에 연결
        let mut tar_builder = Builder::new(&mut brotli_writer);

        // 파일 목록을 순회하며 tar에 추가
        for (idx, f) in files.iter().enumerate() {
            let full_path = base_dir.join(f);
            if full_path.exists() {
                // 100개 단위로 진행 상태 출력
                if idx % 100 == 0 {
                    println!("Processing file {}/{}: {}", idx + 1, files.len(), f);
                }
                // tar에 파일 경로와 이름을 지정해 추가
                if let Err(e) = tar_builder.append_path_with_name(&full_path, f) {
                    eprintln!("tar append error: {}", e);
                }
            } else {
                eprintln!("File not found: {}", full_path.display());
            }
        }

        // tar 아카이브 마무리
        tar_builder.finish().unwrap();

        // brotli 스트림 버퍼 플러시
        brotli_writer.flush().unwrap();

        // writer가 drop되면 reader 쪽 스트림 종료됨
    });

    // reader 쪽 반환
    reader
}

/// 서버에 tar + brotli 압축 스트림을 HTTP POST 요청으로 비동기 업로드
async fn upload_tar_stream(server_url: &str, base_dir: &Path, files: &[String]) -> Result<(), Box<dyn std::error::Error>> {
    // tar + brotli 스트림 생성 (비동기 DuplexStream)
    let stream = create_tar_brotli_stream(base_dir, files).await;

    // DuplexStream(AsyncRead)을 ReaderStream 으로 감싸서 reqwest Body용 Stream<Item=Result<Bytes, _>>로 변환
    let body = reqwest::Body::wrap_stream(ReaderStream::new(stream));

    let client = reqwest::Client::new();

    // POST 요청 전송, body에 스트림을 실시간 전송
    let res = client.post(&format!("{}/apis/api/upload", server_url))
        .body(body)
        .send()
        .await?;

    println!("Upload response: {}", res.status());
    Ok(())
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    // 기준 경로 지정
    let base_dir = PathBuf::from("./test-data");
    // CSV 파일 경로
    let csv_path = PathBuf::from("./file_list.csv");
    // 서버 URL
    let server_url = "http://server.com:8080";

    // 기준 경로 하위 파일 메타데이터 수집
    let files = collect_files(&base_dir);

    // 수집된 파일 정보를 CSV로 생성
    {
        let mut file = std::fs::File::create(&csv_path)?;
        writeln!(file, "path,size,mtime")?;
        for f in &files {
            writeln!(file, "{},{},{}", f.path, f.size, f.mtime)?;
        }
    }

    // CSV 파일을 서버에 보내서 필요한 파일 목록 요청
    let needed = request_needed_files(server_url, &csv_path).await?;

    if needed.is_empty() {
        println!("No files to upload");
        return Ok(());
    }

    // 필요한 파일만 tar + brotli 스트림으로 묶어 서버로 업로드
    upload_tar_stream(server_url, &base_dir, &needed).await?;

    Ok(())
}
