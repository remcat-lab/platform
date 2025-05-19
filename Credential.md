# 🛡️ Credential 서비스 설계 문서

## 1. API 목록

### 🔐 GetSession
- WPF Client에서 사용자의 ID/PW를 RSA public key로 암호화하여 서버에 전송
- Client에서 생성한 AES 키도 함께 전송
- 서버는 RSA key version에 맞는 private key로 복호화 후, AD 인증
- 인증 성공 시 `SessionId`를 발급하고, AES 키와 함께 세션 테이블에 저장
- RSA key는 version 정보를 기반으로 관리하며, 2회 버전까지만 유지하고 이전 키는 disable 처리

---

### ✅ ValidateionSession
- **Client**:
  - DPAPI로 보호된 AES 키를 복호화하여 메모리에 유지
  - 사용자 ID, timestamp, sequence를 AES로 암호화한 payload 생성
  - sessionId와 함께 서버에 전송
- **Server**:
  - sessionId로 세션 테이블에서 AES, 사용자 ID, lastSequence 조회
  - payload 복호화 → 사용자 ID 일치 여부 확인
  - timestamp가 현재 시간과 ±2분 이내인지 확인
  - sequence가 lastSequence보다 클 경우에만 유효
  - 응답: AES로 암호화된 Sequence 반환
- **Client**:
  - 복호화된 응답의 Sequence가 보낸 값과 동일한지 확인

---

## 2. 장점 요약

| 항목 | 설명 |
|------|------|
| ✅ RSA 기반 로그인 | Public Key만으로 안전한 로그인 |
| ✅ AES Client 생성 | 클라이언트에서 키 생성으로 외부 노출 최소화 |
| ✅ Session Table 관리 | 서버 상태 기반의 명확한 인증 유지 |
| ✅ DPAPI 사용 | AES 키를 안전하게 클라이언트에 저장 가능 |
| ✅ Replay Attack 방지 | Timestamp + Sequence 검증 |
| ✅ RSA Key Rotation | Version 관리로 클라이언트 호환성 확보 |

---
