# gRPC + MessagePack 기반 시스템 설계 요약

---

## 1. 통신 구조

- Backend: ASP.NET Core (gRPC 서비스)  
- Frontend: WPF 클라이언트 (gRPC Client, MessagePack 직렬화 사용)  
- Gateway: gRPC 요청 라우팅 및 Microservice 호출 중계  

---

## 2. 프로토콜 및 직렬화

- **통신 프로토콜**: gRPC (HTTP/2 기반)  
- **직렬화**: 기본 protobuf 대신 **MessagePack** 또는 **MemoryPack** 사용 가능  
  - ASP.NET Core에서 `Marshaller<T>`를 커스텀하여 MessagePack Serializer 적용  
  - Gateway와 Microservice 간에도 동일한 직렬화 방식 공유  

---

## 3. Gateway 처리 방식

- Gateway는 미리 Microservice별로 `GrpcChannel` 생성 및 재사용  
- 들어오는 gRPC 요청을 라우팅 테이블 기준으로 내부 Microservice의 gRPC 클라이언트에 연결(프록시)  
- gRPC 스트리밍 지원 가능 (양방향, 서버/클라이언트 스트림 모두)  
- HTTP/1.1 REST API와 gRPC는 Content-Type 및 HTTP/2 여부로 자동 분리 처리  

---

## 4. gRPC vs HTTP 구분

- gRPC 요청: HTTP/2 + POST + Content-Type: `application/grpc`  
- HTTP 요청 (예: REST): HTTP/1.1 또는 HTTP/2 + JSON 등  
- ASP.NET Core `MapGrpcService<T>()`는 gRPC 요청만 처리  
- `MapPost`, `MapGet` 등 Minimal API는 나머지 HTTP 요청 처리  

---

## 5. gRPCChannel 관리

- gRPC 채널은 TCP 소켓 연결 하나를 내부에서 멀티플렉싱하여 여러 요청 처리  
- 채널은 비용이 크므로 요청마다 생성하지 않고, 미리 생성 후 재사용  
- 각 Microservice 별로 하나 이상의 채널 생성 및 재사용 권장  

---

## 6. TCP 멀티플렉싱 개념

- gRPC는 HTTP/2 멀티플렉싱으로 **하나의 TCP 연결에서 다수 스트림 처리**  
- 클라이언트 100명 → Gateway 100 TCP 연결  
- Gateway → Microservice는 적은 수의 TCP 연결(보통 1개)을 공유해 다중 스트림 처리  

---

## 7. 소켓 레벨 멀티플렉싱 구현 원리 (참고)

- TCP 소켓 하나 위에 여러 논리 스트림 구분 위한 **스트림 ID 포함한 패킷 헤더 설계**  
- 패킷 단위로 스트림별 구분, 재조립 후 처리  
- 직접 구현 시 버퍼 관리, 스레드 안전, 패킷 완전성 체크 필요  
- gRPC / HTTP/2가 이를 표준화하여 제공하므로 직접 구현 권장하지 않음  

---

## 8. 소켓 통신 특성

- TCP 소켓은 **Full-Duplex** 지원, 클라이언트와 서버가 동시에 데이터를 주고받을 수 있음  
- 송수신 버퍼 분리되어 독립적으로 처리됨  

---

## 9. 대체 기술과 비교

| 기술                  | 특징                                     |
|-----------------------|------------------------------------------|
| RESTful HTTP/JSON     | 호환성 우수, 편리하지만 속도·기능 제한       |
| GraphQL               | 유연한 쿼리, 복잡성 존재                   |
| Apache Thrift         | 효율적 바이너리 RPC, 다양한 언어 지원        |
| MessagePack + TCP     | 경량 직렬화, 직접 프로토콜 설계 필요          |
| gRPC                  | HTTP/2 기반, 멀티플렉싱, 강력한 생태계 지원  |

---

## 10. 결론

- **gRPC + MessagePack 조합은 고성능, 확장성, 효율성을 위해 매우 적합**  
- HTTP/2 멀티플렉싱, 채널 재사용, 스트리밍 지원 등 현대 분산 시스템 요구사항 충족  
- Gateway → Microservice 간 효율적 라우팅 및 프로토콜 일관성 유지 가능  

---

필요 시 C# 예제, gRPC + MessagePack Marshaller 구현법, 채널 재사용 패턴 등 추가 자료 제공 가능합니다.


# gRPC와 지속 연결의 서버 부하 관계

---

## 🔥 1. HTTP 지속 연결이 부하를 주던 이유 (과거)

### ▶️ HTTP/1.0
- 요청-응답마다 TCP 연결을 열고 닫음
- 연결 설정(3-way handshake) 및 종료 오버헤드 큼
- 연결을 유지하면 오히려 리소스 낭비라고 판단

### ▶️ HTTP/1.1
- Keep-Alive 도입
- 하지만 **동시에 하나의 요청만 처리 가능**
- 병렬 요청을 위해 TCP 연결을 여러 개 유지해야 함
- 서버 입장에서는 연결이 많아져 오히려 부하

---

## 🔥 2. HTTP/2(gRPC)에서는 왜 지속 연결이 유리한가?

### ▶️ HTTP/2의 핵심
- **멀티플렉싱:** 하나의 TCP 연결에 여러 스트림 동시 처리
- TCP 연결 개수는 클라이언트 수와 무관 (소수만 유지)
- 요청-응답 순서 무관, 스트림별 독립 처리

### ▶️ 서버 리소스 관점

| 항목                 | HTTP/1.1                        | HTTP/2 (gRPC)                    |
|----------------------|----------------------------------|-----------------------------------|
| TCP 연결 수           | 요청 병렬화 위해 다수 필요       | 소수의 연결로 수십~수백 요청 처리 |
| 연결 오버헤드         | 연결 설정/종료 빈번             | 연결 한번으로 지속 사용            |
| OS 소켓 리소스 소비   | 클라이언트 수만큼 증가          | 훨씬 적음                         |
| KeepAlive의 부하      | 상대적으로 높음                 | 오히려 성능 최적화 요소           |

→ ✅ TCP 연결을 유지하는 것이 더 효율적이고 서버 부하가 줄어든다.

---

## 🔥 3. HTTP와 gRPC 연결 상태 차이

| 프로토콜     | 연결 상태                | 특징                                       |
|----------------|--------------------------|--------------------------------------------|
| HTTP/1.x       | 연결당 요청 1개          | KeepAlive는 있지만 동시 요청 어려움         |
| HTTP/2 (gRPC)  | 연결 유지 + 멀티플렉싱    | TCP 연결 하나로 수백개 스트림 처리 가능     |
| HTTP/3 (QUIC)  | UDP 기반 연결 유지        | 더 빠르고 신뢰성 있는 지속 연결 (진화형)    |

---

## 🔥 4. 그럼 부하가 없는가? → 상대적

- 부하는 **TCP 연결 수에서 세션 상태 관리로 이동**
- 서버는 연결 유지 동안:
  - Ping/Pong
  - Timeout 관리
  - Flow Control
  - KeepAlive 신호 유지
- 현대 OS는 수천~수만 개 TCP 연결을 효율적으로 관리 가능

---

## 🔥 5. 최종 결론

- ✅ HTTP/1.x에서는 지속 연결이 부담이었다.
- ✅ HTTP/2 (gRPC)는 **지속 연결 + 멀티플렉싱이 오히려 서버 부하를 줄인다.**
- ✅ 요청마다 TCP 연결을 여닫는 것이 더 비효율적이다.
- ✅ 연결 유지 오버헤드는 있지만, 현대 서버 환경에서는 전체적으로 훨씬 더 효율적이다.

---

## 🔥 현실적인 판단 기준

| 상황                 | 권장 방식                       |
|----------------------|---------------------------------|
| 내부 Microservice 간 | ✅ gRPC, 지속 연결 강력 추천    |
| 외부 API             | REST, 필요 시 연결 재활용       |
| 브라우저             | gRPC-Web 또는 HTTP              |

---

## 🔥 추가로 탐색 가능

- 서버 커널 수준 TCP 연결 관리 (epoll, io_uring, IOCP)
- gRPC KeepAlive 설정 최적화
- 대규모 서버에서 수만 연결을 감당하는 전략
