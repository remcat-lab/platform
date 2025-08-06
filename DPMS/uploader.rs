use std::{
    collections::{HashMap, HashSet},
    fs::{self, File},
    io::{self, BufWriter, Write},
    path::{Path, PathBuf},
    time::{SystemTime, UNIX_EPOCH},
};

use chrono::{DateTime, Utc};
use flate2::write::GzEncoder;
use flate2::Compression;
use reqwest::blocking::Client;
use serde::{Deserialize, Serialize};
use tar::Builder;
use walkdir::WalkDir;

// --- Metadata 구조체 ---

#[derive(Debug, Serialize, Deserialize, Clone)]
struct FileMeta {
    path: String,        // 절대 경로 또는 기준 폴더 상대 경로
    size: u64,
    mtime: u64,          // UNIX timestamp in millis
}

type MetadataMap = HashMap<String, FileMeta>; // key: path

// --- 유틸 함수: mtime 추출 ---

fn get_file_mtime_millis(metadata: &fs::Metadata) -> io::Result<u64> {
    let mtime = metadata.modified()?;
    let duration = mtime.duration_since(UNIX_EPOCH).unwrap();
    Ok(duration.as_millis() as u64)
}

// --- 폴더 전체 순회하며 metadata 수집 ---

fn scan_folder_metadata(base_path: &Path) -> io::Result<MetadataMap> {
    let mut map = MetadataMap::new();

    for entry in WalkDir::new(base_path).into_iter().filter_map(Result::ok).filter(|e| e.file_type().is_file()) {
        let path = entry.path();
        let meta = entry.metadata()?;
        let mtime = get_file_mtime_millis(&meta)?;
        let size = meta.len();

        // 상대 경로로 저장 (base_path 기준)
        let rel_path = path.strip_prefix(base_path).unwrap().to_string_lossy().to_string();

        map.insert(
            rel_path.clone(),
            FileMeta {
                path: rel_path,
                size,
                mtime,
            },
        );
    }

    Ok(map)
}

// --- 변경된 파일 찾기 ---
// 이전 metadata와 비교해 변경 또는 새 파일 찾기

fn diff_metadata(old: &MetadataMap, new: &MetadataMap) -> Vec<String> {
    let mut changed_files = Vec::new();

    for (path, new_meta) in new.iter() {
        match old.get(path) {
            Some(old_meta) => {
                // mtime 또는 size가 다르면 변경됨
                if old_meta.mtime != new_meta.mtime || old_meta.size != new_meta.size {
                    changed_files.push(path.clone());
                }
            }
            None => {
                // 새 파일
                changed_files.push(path.clone());
            }
        }
    }

    changed_files
}

// --- 변경 파일을 tar.gz로 압축 ---

fn create_tar_gz(base_path: &Path, files: &[String], output_path: &Path) -> io::Result<()> {
    let tar_gz = File::create(output_path)?;
    let enc = GzEncoder::new(tar_gz, Compression::default());
    let mut tar = Builder::new(enc);

    for file_rel_path in files {
        let full_path = base_path.join(file_rel_path);
        tar.append_path_with_name(&full_path, file_rel_path)?;
    }

    tar.finish()?;
    Ok(())
}

// --- 예시: 서버와 통신 (동기 blocking POST, JSON metadata + tar.gz file) ---

fn send_backup(
    server_url: &str,
    project_id: &str,
    folder: &str,
    metadata: &MetadataMap,
    tar_gz_path: &Path,
) -> Result<(), Box<dyn std::error::Error>> {
    let client = Client::new();

    // metadata JSON 직렬화
    let metadata_json = serde_json::to_string(metadata)?;

    // multipart form 전송 준비
    let form = reqwest::blocking::multipart::Form::new()
        .text("project_id", project_id.to_string())
        .text("folder", folder.to_string())
        .text("metadata", metadata_json)
        .file("backup_tar_gz", tar_gz_path)?;

    let res = client.post(server_url).multipart(form).send()?;

    if res.status().is_success() {
        println!("백업 성공");
    } else {
        println!("백업 실패: {}", res.status());
    }

    Ok(())
}

// --- main 예제 ---

fn main() -> Result<(), Box<dyn std::error::Error>> {
    // 예시: 서버에서 이전 백업 metadata JSON을 받았다고 가정
    // 실제로는 서버 API 호출 후 JSON 파싱 필요
    let previous_metadata: MetadataMap = HashMap::new();

    let project_id = "my_project";
    let folder = "./data";
    let base_path = Path::new(folder);

    // 1) 현재 폴더 상태 스캔
    let current_metadata = scan_folder_metadata(base_path)?;

    // 2) 변경/추가 파일 목록 추출
    let changed_files = diff_metadata(&previous_metadata, &current_metadata);

    if changed_files.is_empty() {
        println!("변경된 파일 없음, 백업 불필요");
        return Ok(());
    }

    println!("변경된 파일 {}개 있음, tar.gz 생성 중...", changed_files.len());

    // 3) tar.gz 생성
    let tar_gz_path = Path::new("backup_chunk.tar.gz");
    create_tar_gz(base_path, &changed_files, tar_gz_path)?;

    // 4) 서버에 전송
    let server_url = "http://backup-server.example.com/api/backup";
    send_backup(server_url, project_id, folder, &current_metadata, tar_gz_path)?;

    Ok(())
}
