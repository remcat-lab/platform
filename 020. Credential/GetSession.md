# GetSession

1. Client는 Preference에 있는 EncryptedSessionId를 DPAPI로 복호화하여 SessionId를 얻는다.
2. Client는 Preference에 있는 평문으로 보관된 SessionSeq를 Header에 담는다.
3. Client는 Preference에 있는 SessionId와 UserId를 DPAPI로 복호화 한뒤, Preference에 있는 AES로 암호화 하여 Header에 담는다.
4. Client는 요청 본문도 같은 AES 키로 암호화한다.
5. Client는 GetInitializeDevicePage API를 호출한다.
6. API Gateway는 URL을 보고 라우팅만 수행하며, 전체 요청을 ServiceA로 전달한다.
7. ServiceA는 Header만 추출하여 Credential API의 GetSession을 호출한다.
8. Credential API는 전달받은 Header의 SessionSeq를 이용해 Credential DB에서 Session Row를 조회한다.
9. 조회된 Row에서 얻은 AES 키로 Header의 SessionId와 UserId를 복호화한다.
10. 복호화된 SessionId와 UserId가 DB에서 조회한 Row의 값과 일치하는지 확인한다.
11. 일치한다면 해당 Session Row를 기반으로 Session 객체를 만들어 ServiceA에 반환한다.
12. ServiceA는 반환된 Session 객체에 포함된 AES 키로 요청 본문을 복호화한다.
13. ServiceA는 복호화된 본문 데이터를 바탕으로 로직을 수행한다.

``` mermaid
sequenceDiagram
    participant Client
    participant ApiGateway
    participant ServiceA
    participant CredentialAPI
    participant CredentialDB

    Client->>Client: EncryptedSessionId DPAPI 복호화 → SessionId
    Client->>Client: SessionSeq(평문), UserId 복호화
    Client->>Client: SessionId, UserId를 AES로 암호화
    Client->>Client: 본문도 AES로 암호화
    Client->>ApiGateway: GetInitializeDevicePage 요청 (Header + 암호화 본문)
    ApiGateway->>ServiceA: 요청 전체 전달

    ServiceA->>CredentialAPI: Header 전달 → GetSession 호출
    CredentialAPI->>CredentialDB: SessionSeq로 Session Row 조회
    CredentialDB-->>CredentialAPI: SessionId, UserId, AES Key 반환
    CredentialAPI->>CredentialAPI: AES Key로 Header의 SessionId, UserId 복호화
    CredentialAPI->>CredentialAPI: 복호화 값과 DB 값 일치 여부 확인
    CredentialAPI-->>ServiceA: Session 객체 반환 (AES Key 포함)

    ServiceA->>ServiceA: AES Key로 본문 복호화
    ServiceA->>ServiceA: 로직 수행
```
