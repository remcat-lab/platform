use std::{
    collections::HashMap,
    fs::{self, File},
    io::{BufRead, BufReader, Write},
    path::Path,
    time::{SystemTime, UNIX_EPOCH},
};

use walkdir::{DirEntry, WalkDir};
use tar::Builder;
use flate2::write::GzEncoder;
use flate2::Compression;
use reqwest::blocking::{Client, multipart};
use anyhow::{Result, Context};

const CONFIG_FILE: &str = "uploadedFile.cfg";
const ARCHIVE_NAME: &str = "upload.tar.gz";
const UPLOAD_URL: &str = "http://server.com:8080/apis/";

fn is_hidden(entry: &DirEntry) -> bool {
    entry
        .file_name()
        .to_str()
        .map(|s| s.starts_with('.'))
        .unwrap_or(false)
}

#[derive(Debug, Clone)]
struct FileInfo {
    mtime_ms: u128,
    size: u64,
}

fn scan_files() -> Result<HashMap<String, FileInfo>> {
    let mut files = HashMap::new();
    for entry in WalkDir::new(".").into_iter().filter_entry(|e| !is_hidden(e)) {
        let entry = entry?;
        let path = entry.path();

        if path.is_file() {
            let meta = fs::metadata(path)?;
            let mtime = meta.modified()?.duration_since(UNIX_EPOCH)?.as_millis();
            let size = meta.len();
            files.insert(
                path.to_string_lossy().into_owned(),
                FileInfo { mtime_ms: mtime, size },
            );
        }
    }
    Ok(files)
}

fn load_previous_cfg() -> Result<HashMap<String, FileInfo>> {
    let mut map = HashMap::new();
    if !Path::new(CONFIG_FILE).exists() {
        return Ok(map);
    }

    let file = File::open(CONFIG_FILE)?;
    for line in BufReader::new(file).lines() {
        let line = line?;
        let parts: Vec<&str> = line.split('\t').collect();
        if parts.len() == 3 {
            if let (Ok(mtime_ms), Ok(size)) = (parts[1].parse::<u128>(), parts[2].parse::<u64>()) {
                map.insert(
                    parts[0].to_string(),
                    FileInfo { mtime_ms, size },
                );
            }
        }
    }
    Ok(map)
}

fn save_cfg(map: &HashMap<String, FileInfo>) -> Result<()> {
    let mut file = File::create(CONFIG_FILE)?;
    for (k, v) in map {
        writeln!(file, "{}\t{}\t{}", k, v.mtime_ms, v.size)?;
    }
    Ok(())
}

fn create_tar(changed_files: &[String]) -> Result<()> {
    let tar_gz = File::create(ARCHIVE_NAME)?;
    let enc = GzEncoder::new(tar_gz, Compression::default());
    let mut tar = Builder::new(enc);

    for path in changed_files {
        tar.append_path(path)?;
    }

    tar.finish()?;
    Ok(())
}

fn upload_tar() -> Result<()> {
    let file = File::open(ARCHIVE_NAME)?;
    let part = multipart::Part::reader(file).file_name(ARCHIVE_NAME);
    let form = multipart::Form::new().part("file", part);

    let client = Client::new();
    let res = client.post(UPLOAD_URL).multipart(form).send()?;
    if !res.status().is_success() {
        Err(anyhow::anyhow!("Upload failed: {}", res.status()))
    } else {
        println!("Upload successful.");
        Ok(())
    }
}

fn main() -> Result<()> {
    let current = scan_files().context("Scanning current files failed")?;
    let previous = load_previous_cfg().context("Loading previous cfg failed")?;

    let changed: Vec<String> = current
        .iter()
        .filter(|(path, info)| {
            match previous.get(*path) {
                Some(prev) => prev.mtime_ms != info.mtime_ms || prev.size != info.size,
                None => true,
            }
        })
        .map(|(k, _)| k.clone())
        .collect();

    if changed.is_empty() {
        println!("No files changed.");
    } else {
        println!("Changed files:");
        for f in &changed {
            println!("{}", f);
        }

        create_tar(&changed)?;
        upload_tar()?;
        save_cfg(&current)?;
    }

    Ok(())
}
