# RequestDelegate

1. RequestDelegate는 ApiGateway의 WebApplication의 Route Map에 연결된 대리자이다.
2. Client에서 보낸 url의 두번째 segment가 serviceId인데, 이것으로 ApiGateway DB의 Route Table에서 Route Row를 가져온다.
3. 만약 Route Row가 없을때는 501 status를 반환하고, 없는 serviceId 요청이라고 알려준다.
4. Route.Allow가 1인 경우 Url을 그대로 해당 서비스에 라우팅 한다.
5. Route.Allow가 0인 경우 Header의 SessionSeq를 확인한다.
6. SessionSeq가 없다면 401 을 Client에 반환하고 종료한다.
7. SessionSeq가 있다면 Header에서 EncryptedSessionId, EncryptedUserId를 가져와 함께 Credential.GetSession을 호출해 Session을 가져온다.
8. GetSession 내부에서 Credential DB의 Session Table에 일치하는 Session이 없다면 401을 Client에 반환하고, Permission이 없다면 403을 Client에 반환하는데, 이 status가 200이 아닌 경우, client에 그대로 전달한다.
9. Session이 있다면 AesKey, AesIv를 masterKey로 복호화 한다.
10. 복호화 된 AES로 본문을 복호화 한뒤 Service에 라우팅 한다.
11. Service에서 200이 반환되면 정상적인 처리가 된것으로 보고, Client에 응답 본문을 Aes로 암호화하고 전달한다.


``` mermaid
sequenceDiagram
    participant Client
    participant RequestDelegate
    participant RouteDB as ApiGateway DB (Route Table)
    participant Credential
    participant SessionDB as Credential DB (Session Table)
    participant Service

    Client->>RequestDelegate: Request(url, EncryptedBody, Headers: EncryptedSessionId, EncryptedUserId, SessionSeq)

    RequestDelegate->>RequestDelegate: Extract serviceId from URL (2nd segment)
    RequestDelegate->>RouteDB: Get RouteRow by serviceId
    alt RouteRow not found
        RouteDB-->>RequestDelegate: null
        RequestDelegate-->>Client: 501 Not Implemented ("Unknown serviceId")
    else RouteRow found
        RouteDB-->>RequestDelegate: RouteRow (Allow)
        alt Route.Allow == 1
            RequestDelegate->>Service: Forward request as-is
            Service-->>RequestDelegate: Response (200 OK)
            RequestDelegate-->>Client: Response (200 OK)
        else Route.Allow == 0
            alt SessionSeq is missing in header
                RequestDelegate-->>Client: 401 Unauthorized
            else SessionSeq exists
                RequestDelegate->>Credential: GetSession(EncryptedSessionId, EncryptedUserId)
                Credential->>SessionDB: Lookup Session

                alt No session found
                    SessionDB-->>Credential: Not Found
                    Credential-->>RequestDelegate: 401 Unauthorized
                    RequestDelegate-->>Client: 401 Unauthorized
                else No permission
                    SessionDB-->>Credential: No Permission
                    Credential-->>RequestDelegate: 403 Forbidden
                    RequestDelegate-->>Client: 403 Forbidden
                else Valid session
                    SessionDB-->>Credential: Session Info (EncryptedAesKey, AesIv)
                    Credential-->>RequestDelegate: Session Info

                    RequestDelegate->>RequestDelegate: Decrypt AesKey and AesIv using MasterKey
                    RequestDelegate->>RequestDelegate: Decrypt Body with AES Key

                    RequestDelegate->>Service: Request (Decrypted Body)
                    Service-->>RequestDelegate: Response (200 OK)

                    RequestDelegate->>RequestDelegate: Encrypt Response with AES
                    RequestDelegate-->>Client: Encrypted Response (200 OK)
                end
            end
        end
    end
```
