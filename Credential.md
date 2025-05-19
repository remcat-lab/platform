# Credential 서비스

## 1. Api List

### GetSession
 - WPF Client에서 사용자의 ID/PW를 이용해 로그인을 하고 AES를 위한 대칭키를 찾기 위한 SessionId를 발급한다.
 - ID/PW는 WPF Client에 embed 되어 있는 public key를 사용해서 암호화 해 서버로 전달한다.
 - ID/PW와 함께 client에서 AES를 만들어 전송하기 때문에 서버에서는 ID/PW로 Active Directory에서 인증이 정상적으로 되면 SessionId를 발급하고 AES와 함께 Table에 저장한다.
 - public key는 배포시 변경이 되므로, 업데이트를 하지 않은 client와 최신 client 간 public key가 동일하지 않은 문제를 해결하기 위해 RSA 키 쌍을 서버에 가지고 있다가 처리한다.
 - 배포시 public key가 변경될 때는 public key의 version이 있어 GetSession에 요청할때, 해당 version을 전달한다. 그리고 RSA 키는 2회까지만 유지하고 그 이전의 키들은 disable 시킨다.

### ValidateionSession
 - SessionId와 AES로 암호화된 본문을 해석해 정상적인 세션으로 접근을 하는지 확인한다.
 - AES내부에는 현재 client에 저장된 사용자Id, Sequence를 함께 보내며, 서버에서는 sessionId로 AES와 사용자Id를 찾고, AES 복호화 한 뒤 사용자 Id와 비교해 맞으면 Sequence를 반환한다. 그러면 Client에서는 보냈던 Sequence를 비교해 정상적인지 확인한다.
