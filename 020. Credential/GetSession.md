# GetSession

1. Client는 Preference에 있는 EncryptedSessionId를 DPAPI로 복호화하여 SessionId를 얻는다.
2. Client는 Preference에 있는 평문으로 보관된 SessionSeq를 Header에 담는다.
3. Client는 Preference에 있는 SessionId와 UserId를 DPAPI로 복호화 한뒤, Preference에 있는 AES로 암호화 하여 Header에 담는다.
4. Client는 요청 본문도 같은 AES 키로 암호화한다.
5. Client는 GetInitializeDevicePage API를 호출한다.
6. API Gateway는 SessionSeq를 확인하고, 있다면 Credential의 GetSession을 호출하고 결과를 받아 URL로 라우팅을 수행한다.
7. Credential API는 전달받은 Header의 SessionSeq를 이용해 Credential DB에서 Session Row를 조회한다.
8. 조회된 Row에서 얻은 AES 키로 Header의 SessionId와 UserId를 복호화한다.
9. 복호화된 SessionId와 UserId가 DB에서 조회한 Row의 값과 일치하는지 확인한다.
10. 일치한다면 해당 Session Row를 기반으로 Session 객체를 만들어 반환한다.
11. 반환된 Session 객체가 비어 있는 경우는 http status 401 권한 필요를 Client에게 반환한다.
12. ValidatePermission을 호출해, 현재 사용자와 url이 접근 권한이 있는지 판단해, 만약 권한이 없다면 403 forbidden을 반환한다.
13. ApiGateway는 반환된 Session 객체에 포함된 AES 키로 요청 본문을 복호화한다.
14. 평문으로 만들어진 데이터를 Header와 함께 ServiceA로 요청한다.
15. ServiceA에서 로직을 처리하고 ApiGateway에 응답을 전달한다. ApiGateway에서는 Client로 보낼 때, 압축 및 Session 객체를 이용해 암호화를 적용한다. 

``` mermaid
sequenceDiagram
    participant Client
    participant ApiGateway
    participant CredentialAPI
    participant AuthzService
    participant ServiceA

    Client->>Client: 1. EncryptedSessionId DPAPI 복호화 → SessionId 획득
    Client->>Client: 2. 평문 SessionSeq → Header에 담음
    Client->>Client: 3. SessionId, UserId DPAPI 복호화 → AES 암호화 → Header에 담음
    Client->>Client: 4. 본문도 AES 키로 암호화
    Client->>ApiGateway: 5. GetInitializeDevicePage 호출 (암호화된 Header, Body 포함)

    ApiGateway->>ApiGateway: 6. SessionSeq 확인
    ApiGateway->>CredentialAPI: GetSession(SessionSeq, Encrypted SessionId/UserId)

    CredentialAPI->>CredentialAPI: 7. SessionSeq로 DB에서 Session Row 조회
    CredentialAPI->>CredentialAPI: 8. 조회된 AES 키로 Header의 SessionId, UserId 복호화
    CredentialAPI->>CredentialAPI: 9. 복호화된 값이 DB의 SessionId, UserId와 일치하는지 확인
    CredentialAPI-->>ApiGateway: 10. Session 객체 반환 (없으면 null)

    alt Session 없음
        ApiGateway-->>Client: 11. HTTP 401 Unauthorized 반환
    else Session 있음
        ApiGateway->>AuthzService: 12. ValidatePermission(Session.UserId, 요청 URL)
        alt 권한 없음
            ApiGateway-->>Client: 12. HTTP 403 Forbidden 반환
        else 권한 있음
            ApiGateway->>ApiGateway: 13. Session 객체의 AES 키로 본문 복호화
            ApiGateway->>ServiceA: 14. 평문 데이터 + Header 전달
            ServiceA->>ServiceA: 15. 로직 처리
            ServiceA-->>ApiGateway: 응답 반환
            ApiGateway->>ApiGateway: 응답 압축 + AES 키로 암호화
            ApiGateway-->>Client: 암호화된 응답 반환
        end
    end
```
