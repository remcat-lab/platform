# Rust 기반 반도체 테스트 장비 데이터 전송 시스템 설계

## 1. 개요

Rust를 활용하여 반도체 테스트 장비에서 데이터를 서버로 전송하는 `uploader`와 이를 최신 상태로 유지하는 `updater`를 분리하여 구성합니다. 이 설계는 보안성, 관리 효율성, 성능 측면에서 최적화된 구조입니다.

## 2. 시스템 구성

```
/usr/local/bin/
├── updater       (Rust 실행파일)
├── uploader      (Rust 실행파일, 버전 업데이트 대상)
```

## 3. 업데이트 순서

1. `updater` 실행
2. 동일 폴더 내 `uploader`의 SHA-256 해시 계산
3. 해시 값을 API 서버로 전송 (`POST /check-uploader-version`)
4. 서버에서 최신 여부 판단 후, 필요 시 최신 uploader 바이너리 응답
5. 다운로드 후 기존 uploader 백업 → 새 uploader 저장
6. 저장 완료 시 실행 (`std::process::Command`)

## 4. 장점

| 항목 | 설명 |
|------|------|
| 중앙 업데이트 관리 | 서버에서 버전 검증 및 바이너리 제공 |
| 보안성 강화 | 해시 기반 무결성 검증 |
| 외부 의존성 제거 | curl, jq 등 필요 없음 |
| 일관성 있는 장비 운영 | 모든 장비가 동일 로직으로 최신 유지 |
| 디버깅 용이 | 로그 기반 예외 처리 가능 |
| 배포 효율성 | `uploader`만 교체, `updater`는 고정 |

## 5. 구현 예시

### SHA-256 해시 계산 (Rust)
```rust
use std::fs::File;
use std::io::{BufReader, Read};
use sha2::{Sha256, Digest};

fn compute_sha256(path: &str) -> Result<String, std::io::Error> {
    let file = File::open(path)?;
    let mut reader = BufReader::new(file);
    let mut hasher = Sha256::new();
    let mut buffer = [0; 4096];

    loop {
        let bytes_read = reader.read(&mut buffer)?;
        if bytes_read == 0 {
            break;
        }
        hasher.update(&buffer[..bytes_read]);
    }

    Ok(format!("{:x}", hasher.finalize()))
}
```

### API 예시

#### 요청
```json
POST /check-uploader-version
{
  "group_id": "ABC123",
  "client_sha256": "9cfe42d38c1aa7...",
  "os": "linux-x86_64"
}
```

#### 응답
```json
{
  "status": "update_required",
  "download_url": "https://internal/nas/uploader_linux_x86_64"
}
```

### Rust로 다운로드 및 실행
```rust
// 다운로드
let response = reqwest::get(download_url).await?;
let bytes = response.bytes().await?;
std::fs::write("/usr/local/bin/uploader", &bytes)?;
std::fs::set_permissions("/usr/local/bin/uploader", fs::Permissions::from_mode(0o755))?;

// 실행
use std::process::Command;
let mut child = Command::new("./uploader")
    .arg("--run")
    .spawn()?;
let exit_status = child.wait()?;
```

## 6. 보안 고려 사항

| 항목 | 설명 |
|------|------|
| 해시 검증 | 서버에 uploader 해시 저장 후 비교 |
| 서명 도입 (옵션) | 서버 서명 + 클라이언트 공개키 검증 |
| HTTPS 사용 | reqwest + 인증서 기반 암호화 통신 |
| 업데이트 이력 | 클라이언트 로컬 로그 또는 서버 기록 |

## 7. 결론

이 방식은 반도체 테스트 장비와 같은 고신뢰 환경에 적합한 구조로, 보안성, 성능, 배포 편의성을 모두 충족합니다.

> updater는 고정된 바이너리로 유지하고, uploader만 중앙에서 교체 가능하게 하여 유지보수 부담을 크게 줄일 수 있습니다.
