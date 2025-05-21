# GetSession

- 암호화 방식의 로그인을 처리하는 기능으로 아래와 같은 시퀀스를 가진다.

1. wpf앱은 clickonce로 배포한다. 이때 배포된 바이너리 속에 public key가 포함되어 있다. 이 public key로 ID/PW, 그리고 Client에서 생성한 AES 키를 포함해 Memorypack으로 serialize한뒤, 암호화 한다. 
2. apigateway에 credential.getSession api를 http로 호출하면서 암호화된 데이터를 전송한다. 
3. ApiGateway는 SessionId가 헤더에 없기 때문에 url을 보고, 원문 그대로 credential에게 전달한다.
4. Credential은 url의 getSession을 보고 getSession method를 호출한다. 
5. GetSession 에서는 private key로 본문을 복호화 하고, 그중, ID, PW를 Active Directory에 인증 처리를 한다. 만약 정상 인증이라면 SessionId를 생성하고, AES Key와 UserId, SessionId를 Credential DB의 Session table에 넣는다. 이때 AES는 서버 전체의 암호화를 위한 Master Key로 암호화 해 넣는다. 
6. Http Status는 200으로 반환하면서, SessionId, UserId를 전달한다. 이때 Client에서 전달 받은 대칭키로 암호화해 Apigateway로 보내고 이것을 Client에 전달한다. 
7. Client에서는 status 200일때, 본문을 가져와 생성했던 AES 키로 복호화 해, Session Id를 Client의 Preference에 저장한다.

```mermaid
sequenceDiagram
    participant WPF Client
    participant API Gateway
    participant Credential Server
    participant Active Directory
    participant Credential DB

    %% Step 1: Client - 암호화 준비
    WPF Client->>WPF Client: ClickOnce 배포로 PublicKey 포함
    WPF Client->>WPF Client: ID/PW + 생성한 AES Key -> MemoryPack serialize
    WPF Client->>WPF Client: Serialize된 데이터 -> PublicKey로 암호화

    %% Step 2: API Gateway에 요청
    WPF Client->>API Gateway: POST /credential/getSession (암호화된 본문)

    %% Step 3: Gateway가 Credential 서버로 전달
    API Gateway->>Credential Server: POST /credential/getSession (원문 그대로 전달)

    %% Step 4: Credential 서버가 요청 분석
    Credential Server->>Credential Server: URL 분석 -> getSession 메서드 호출
    Credential Server->>Credential Server: PrivateKey로 암호화된 본문 복호화

    %% Step 5: AD 인증 및 세션 저장
    Credential Server->>Active Directory: ID/PW 인증 요청
    Active Directory-->>Credential Server: 인증 성공
    Credential Server->>Credential Server: SessionId 생성
    Credential Server->>Credential DB: UserId, SessionId, AES(Key는 MasterKey로 암호화) 저장

    %% Step 6: 결과 응답 암호화 및 전달
    Credential Server->>Credential Server: SessionId + UserId -> Client AES Key로 암호화
    Credential Server-->>API Gateway: HTTP 200 + 암호화된 본문
    API Gateway-->>WPF Client: HTTP 200 + 암호화된 본문

    %% Step 7: Client 응답 처리
    WPF Client->>WPF Client: AES 키로 복호화
    WPF Client->>WPF Client: SessionId -> Preference 저장

```
