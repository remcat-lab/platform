// Rust client for file sync using tar + brotli (cross-platform, ureq HTTP client)
use std::fs::{self, File};
use std::io::{BufWriter, Write, Read};
use std::path::{Path, PathBuf};
use std::time::{SystemTime, UNIX_EPOCH};

use brotli::CompressorWriter;
use rayon::prelude::*;
use serde::Deserialize;
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
        .par_bridge()
        .filter_map(|entry| {
            let path = entry.path();
            if let Ok(metadata) = fs::metadata(path) {
                let modified = metadata.modified().unwrap_or(SystemTime::UNIX_EPOCH);
                let mtime = modified.duration_since(UNIX_EPOCH).unwrap().as_millis();
                Some(FileMeta {
                    path: normalize_path(path, base_dir),
                    size: metadata.len(),
                    mtime,
                })
            } else {
                None
            }
        })
        .collect()
}

fn write_csv(file_metas: &[FileMeta], csv_path: &Path) -> std::io::Result<()> {
    let mut file = File::create(csv_path)?;
    writeln!(file, "path,size,mtime")?;
    for f in file_metas {
        writeln!(file, "{},{},{}", f.path, f.size, f.mtime)?;
    }
    Ok(())
}

#[derive(Debug, Deserialize)]
struct NeededFiles {
    needed_files: Vec<String>,
}

// ureq로 multipart/form-data 파일 업로드를 구현하는 함수
fn post_multipart_file(url: &str, field_name: &str, file_path: &Path) -> Result<String, Box<dyn std::error::Error>> {
    use std::fs;
    use std::io::Cursor;
    use ureq::multipart::{FormData, Part};

    let file_bytes = fs::read(file_path)?;
    let file_name = file_path.file_name().unwrap().to_string_lossy();

    let form = FormData::new()
        .part(field_name, Part::bytes(file_bytes).file_name(file_name.to_string()));

    let resp = ureq::post(url)
        .set("Content-Type", "multipart/form-data") // ureq이 자동으로 세팅하지만 명시해도 무방
        .send_form(form)?;

    if resp.status() != 200 {
        return Err(format!("HTTP error status: {}", resp.status()).into());
    }
    let text = resp.into_string()?;
    Ok(text)
}

fn request_needed_files(server_url: &str, csv_path: &Path) -> Result<Vec<String>, Box<dyn std::error::Error>> {
    // POST multipart CSV to server and parse JSON response
    let url = format!("{}/apis/api/check_csv", server_url);
    let resp_text = post_multipart_file(&url, "csv", csv_path)?;

    let needed: NeededFiles = serde_json::from_str(&resp_text)?;
    Ok(needed.needed_files)
}

fn upload_tar(server_url: &str, tar_path: &Path) -> Result<(), Box<dyn std::error::Error>> {
    let url = format!("{}/apis/api/upload", server_url);
    let _resp_text = post_multipart_file(&url, "tar", tar_path)?;
    println!("Upload successful");
    Ok(())
}

fn create_tar_brotli(base_dir: &Path, files: &[String], output: &Path) -> std::io::Result<()> {
    let tar_file = File::create(output)?;
    let buf = BufWriter::new(tar_file);
    let brotli_writer = CompressorWriter::new(buf, 4096, 5, 22);
    let mut tar = tar::Builder::new(brotli_writer);

    for f in files {
        let full_path = base_dir.join(f);
        if full_path.exists() {
            tar.append_path_with_name(&full_path, f)?;
        }
    }

    tar.finish()?;
    Ok(())
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let base_dir = PathBuf::from("./test-data"); // 작업 디렉토리
    let csv_path = PathBuf::from("./file_list.csv");
    let tar_path = PathBuf::from("./upload.tar.br");
    let server_url = "http://server.com:8080";

    let files = collect_files(&base_dir);
    write_csv(&files, &csv_path)?;

    let needed = request_needed_files(server_url, &csv_path)?;

    if needed.is_empty() {
        println!("No files to upload");
        return Ok(());
    }

    create_tar_brotli(&base_dir, &needed, &tar_path)?;
    upload_tar(server_url, &tar_path)?;

    Ok(())
}
