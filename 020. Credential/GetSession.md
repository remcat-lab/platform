# GetSession

- 암호화 방식의 로그인을 처리하는 기능으로 아래와 같은 시퀀스를 가진다.

1. wpf앱은 clickonce로 배포한다. 이때 배포된 바이너리 속에 public key와 KeySeq가 포함되어 있다. 이 public key로 ID/PW, 그리고 Client에서 생성한 AES 키를 각각 암호화 한 뒤, KeySeq와 함께 Memorypack으로 serialize한다. 
2. apigateway에 credential.getSession api를 http로 호출하면서 암호화된 데이터를 전송한다. 
3. ApiGateway는 SessionId가 헤더에 없기 때문에 url을 보고, 원문 그대로 credential에게 전달한다.
4. Credential은 url의 getSession을 보고 getSession method를 호출한다. 
5. GetSession 에서는 KeySeq를 보고 private Key를 찾아, private key로 본문을 복호화 하고, 그 중, ID, PW를 Active Directory에 인증 처리를 한다.
   만약 정상 인증이라면 SessionId를 생성하고, AES Key와 UserId, SessionId를 Credential DB의 Session table에 넣는다. 이때 AES는 서버 전체의 암호화를 위한 Master Key로 암호화 해 넣는다. 만약 비정상이면 401을 반환한다.
7. Http Status는 200으로 반환하면서, SessionId, UserId를 전달한다. 이때 Client에서 전달 받은 대칭키로 암호화해 Apigateway로 보내고 이것을 Client에 전달한다. 
8. Client에서는 status 200일때, 본문을 가져와 생성했던 AES 키로 복호화 해, Session Id를 Client의 Preference에 저장한다.

```mermaid
sequenceDiagram
    participant Client
    participant ApiGateway
    participant CredentialServer
    participant ActiveDirectory
    participant CredentialDB

    Note over Client: ClickOnce로 배포된 앱에<br/>PublicKey + KeySeq 포함됨

    Client->>Client: AES 키 생성
    Client->>Client: ID/PW, AES 키를<br/>PublicKey(KeySeq)에 의해 RSA 암호화
    Client->>Client: MemoryPack으로 암호화<br/>+ KeySeq 포함

    Client->>ApiGateway: POST /credential/getSession<br/>암호화된 Payload 전송 (HTTP)

    ApiGateway->>CredentialServer: 전달 (URL 및 본문 그대로)

    CredentialServer->>CredentialServer: KeySeq로 PrivateKey 찾음
    CredentialServer->>CredentialServer: RSA 복호화 (ID/PW, AES)
    CredentialServer->>ActiveDirectory: ID/PW 인증 요청

    alt 인증 성공
        ActiveDirectory-->>CredentialServer: OK
        CredentialServer->>CredentialServer: SessionId 생성
        CredentialServer->>CredentialDB: SessionId, UserId, AES를<br/>MasterKey로 암호화해 저장
        CredentialServer->>CredentialServer: AES로 UserId, SessionId 암호화
        CredentialServer->>ApiGateway: status 200 + 암호화된 응답
        ApiGateway->>Client: 전달
        Client->>Client: AES로 복호화하여 SessionId 저장
    else 인증 실패
        ActiveDirectory-->>CredentialServer: 인증 실패
        CredentialServer->>ApiGateway: status 401
        ApiGateway->>Client: 전달
    end

```
