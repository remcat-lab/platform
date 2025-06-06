# Api Gateway

``` mermaid
sequenceDiagram
    participant Client
    participant APIGateway
    participant Credential
    participant Service

    Note over Client: Session 기반 요청<br/>Payload는 AES로 암호화됨
    Client->>APIGateway: SessionId + EncryptedPayload

    Note over APIGateway: 세션 검증 요청
    APIGateway->>Credential: ValidateSession(SessionId)
    Credential-->>APIGateway: UserId, AESKey

    Note over APIGateway: AESKey로 복호화
    APIGateway->>APIGateway: Decrypt(EncryptedPayload, AESKey)

    Note over APIGateway: 평문 전달
    APIGateway->>Service: UserId + DecryptedPayload

    Service->>Service: Handle DecryptedPayload

    Note over Client: 비세션 요청 (예: 로그인)
    Client->>APIGateway: PlainPayload
    APIGateway->>Service: PlainPayload
    Service->>Service: Handle PlainPayload
```

## 1. 기능 설명
 ### Ciphers
 - Api Gateway와 Client간의 암호화 처리를 위한 기능으로 버전별 RSA 키를 보관, 관리한다.
 ### Services 
 - 서비스의 라우팅을 처리하는 것으로 생성되는 각 서비스의 name과 endpoint, access여부를 관리한다.

## 2. Cipher
### 2-1. RSA

