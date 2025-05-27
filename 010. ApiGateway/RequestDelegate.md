# RequestDelegate

1. RequestDelegate는 ApiGateway의 WebApplication의 Route Map에 연결된 대리자이다.
2. Client에서 보낸 url의 두번째 segment가 serviceId인데, 이것으로 ApiGateway DB의 Route Table에서 Route Row를 가져온다.
3. 만약 Route Row가 없을때는 501 status를 반환하고, 없는 serviceId 요청이라고 알려준다.



``` mermaid
flowchart TD
    A[Client에서 API 요청 <br>- 예: /api/serviceId/endpoint] --> B[ApiGateway의 RequestDelegate 처리]
    B --> C[URL에서 serviceId 추출]
    C --> D[RouteDB에서 serviceId로 Route 조회]
    D --> E{Route 정보 존재함?}
    E -- 예 --> F[정상 처리 - 해당 서비스로 프록시 요청]
    E -- 아니오 --> G[501 Not Implemented 반환 - 존재하지 않는 serviceId 알림]


```
