
# 오래된 Linux 장비에서 Rust 실행을 위한 체크리스트

Rust 프로그램을 오래된 Linux 시스템에서 실행하려면 아래 항목을 점검하세요.

---

## ✅ 1. CPU 아키텍처 확인

Rust는 보통 `x86_64`, `aarch64` 아키텍처를 지원합니다.

```bash
uname -m
```

출력 예시:
- `x86_64` → OK
- `i686`, `i386` → 구버전, 지원 제한적
- `armv7l`, `aarch64` → 크로스 컴파일 필요할 수 있음

---

## ✅ 2. glibc 버전 확인

Rust 바이너리는 `glibc`에 의존합니다.

```bash
ldd --version
```

- 권장 버전: `glibc 2.17` 이상 (`2.28+` 권장)
- `musl`로 빌드하면 glibc 없이도 실행 가능

---

## ✅ 3. OS 및 커널 버전 확인

```bash
uname -r             # 커널 버전
cat /etc/*release    # 배포판 버전
```

- CentOS 6.x, Ubuntu 14.04 이하는 Rust 실행 어려울 수 있음

---

## ✅ 4. 동적 라이브러리 의존성 확인

```bash
ldd your_rust_binary
```

- `not found` 항목이 없도록 확인
- 주요 라이브러리: `libstdc++.so.6`, `libc.so.6` 등

---

## ✅ 5. 바이너리 포맷 확인

```bash
file your_rust_binary
```

출력 예시:
```
ELF 64-bit LSB executable, x86-64, dynamically linked ...
```

- 시스템이 32bit인데 바이너리가 64bit면 실행 불가
- `statically linked` 되어 있으면 호환성 높음

---

## ✅ 6. musl 정적 링크 빌드 (권장)

```bash
rustup target add x86_64-unknown-linux-musl
cargo build --release --target x86_64-unknown-linux-musl
```

- glibc 없이 실행 가능
- 하나의 실행 파일로 배포 가능

---

## ✅ 7. 최종 체크리스트

| 항목 | 확인 명령어 | 주의사항 |
|------|-------------|----------|
| CPU 아키텍처 | `uname -m` | 64bit인지 확인 |
| glibc 버전 | `ldd --version` | 2.17 이상 권장 |
| OS 버전 | `cat /etc/*release` | CentOS 6 이하는 불안정 |
| 커널 버전 | `uname -r` | 3.x 이상 권장 |
| 실행 가능 여부 | `ldd your_binary`, `file your_binary` | 동적 의존성 및 비트 수 확인 |
| 정적 빌드 여부 | `cargo build --target x86_64-unknown-linux-musl` | 호환성 증가 |

---

필요 시, 시스템의 명령어 출력 결과를 분석하여 실행 가능 여부를 도와드립니다.
