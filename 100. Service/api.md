#api

1. clientip를 header에서 가져온다
2. sessionSeq, encryptedSessionId, encrypteduserid를 헤더에서 가져와 apiProvider를 만든다
3. provider로 Credential.GetSession을 호출해 Session을 가져온다. 이때 params에 clientip를 넣어준다
8. GetSession 내부에서 Credential DB의 Session Table에 일치하는 Session이 없다면 401을 Client에 반환하고, Permission이 없다면 403을 Client에 반환하는데, 이 status가 200이 아닌 경우, client에 그대로 전달한다.
9. Session이 있다면 AesKey, AesIv를 masterKey로 복호화 한다.
10. 복호화 된 AES로 본문을 복호화 한뒤 Service에 라우팅 한다.
11. Service에서 200이 반환되면 정상적인 처리가 된것으로 보고, Client에 응답 본문을 Aes로 암호화하고 전달한다.
