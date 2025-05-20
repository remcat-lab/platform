# Platform

## 1. 개요

본 문서는 WPF 기반 클라이언트와 서버 간 통신 구조를 설명하고, 고가용성과 확장성을 고려한 서버 아키텍처 설계를 기술한다.  
서버 구조는 이중화된 API Gateway와 App 서버, master-master 복제 기반 DB를 통해 구성되어 있으며,  
사용자 인증 및 서비스 접근 제어를 포함한 통합 서비스 플랫폼을 제공한다.

---

## 2. 시스템 구성도

``` mermaid

graph TD
    %% 클라이언트
    subgraph Client
        WPF[WPF Client]
    end

    %% L4 로드밸런서
    subgraph Network
        L4[L4 Load Balancer]
    end

    %% 그룹 A
    subgraph GroupA["그룹 A"]

        subgraph WebServerA["Web Server A"]
            GW_A[API Gateway A]
        end

        subgraph AppServerA["App Server A"]
            CRED_A[Credential Service A]
            SVC_A[Services A]
            subgraph SVC_A_Sub[Service 1 ~ 10 A]
                SVC_A1[Service 1]
                SVC_A2[Service 2]
                SVC_A10[...Service 10]
            end
        end

        subgraph DBServerA["DB Server A"]
            DB_A[(DB A)]
        end

    end

    %% 그룹 B
    subgraph GroupB["그룹 B"]

        subgraph WebServerB["Web Server B"]
            GW_B[API Gateway B]
        end

        subgraph AppServerB["App Server B"]
            CRED_B[Credential Service B]
            SVC_B[Services B]
            subgraph SVC_B_Sub[Service 1 ~ 10 B]
                SVC_B1[Service 1]
                SVC_B2[Service 2]
                SVC_B10[...Service 10]
            end
        end

        subgraph DBServerB["DB Server B"]
            DB_B[(DB B)]
        end

    end

    %% 연결 관계
    WPF --> L4
    L4 --> GW_A
    L4 --> GW_B

    GW_A --> CRED_A
    GW_A --> SVC_A

    GW_B --> CRED_B
    GW_B --> SVC_B

    CRED_A --> DB_A
    SVC_A1 --> DB_A
    SVC_A2 --> DB_A

    CRED_B --> DB_B
    SVC_B1 --> DB_B
    SVC_B2 --> DB_B

    DB_A <--> DB_B


```

## 3. 주요 구성 요소

### ✅ WPF Client
- 데스크탑 애플리케이션
- 인증은 서버의 공개키로 암호화해 전달하고, 세션Id를 받아 메모리에서 관리한다.
- 서버와의 통신은 L4 로드밸런서를 통해 수행하며 세션Id와 AES로 암호화된 본문을 전달한다.
- 서비스 요청 및 결과 시각화

### ✅ L4 Load Balancer
- Web 서버 A/B로 트래픽 분산 (라운드로빈 또는 커스텀 알고리즘)
- 서버 장애 시 자동 Failover 기능

### ✅ API Gateway (Web 서버 내 포함)
- 클라이언트 요청 수신 및 라우팅(api gateway db의 service table을 통해 처리)
- 세션 인증(Credential) 및 AES 복호화 처리
- 요청/응답 로깅 처리
- A, B 그룹 모두에서 독립적으로 운영되며, DB를 통해 동기화된 라우팅 및 인증 정보를 공유

### ✅ App 서버
- **Credential 서비스**
  - 사용자 인증 및 권한 관리
  - 사용자별 서비스 접근 제어 정보 제공
- **서비스 1~10**
  - 각각 독립적인 비즈니스 로직 처리
  - 최종 접근 권한 판단은 서비스 내에서 수행
  - 서비스 간 완전한 분리 및 장애 격리 구조

### ✅ DB 서버
- 모든 서버 구성 요소(API Gateway, Credential, 서비스)와 연동
- A/B 그룹 간 **Master-Master Replication** 구성
  - 실시간 데이터 동기화
  - 장애 시 무중단 전환
  - 변경 충돌 최소화를 위한 서비스별 Key 또는 Timestamp 기반 정책 필요

---

## 4. 인증 및 권한 관리 흐름

1. 클라이언트 로그인 요청 → L4 → API Gateway A or B로 분기  
2. API Gateway는 App 서버의 Credential 서비스로 요청 전달  
3. Credential 서비스는 사용자 인증 후 세션 발급 및 권한 정보 반환  
4. 이후 요청은 API Gateway → 각 서비스로 전달  
5. 각 서비스는 Credential의 정보를 바탕으로 자체적으로 권한 검증 수행  

---

## 5. Master-Master DB 복제

### ✅ 동기화 대상
- API Gateway 라우팅 설정  
- 사용자 인증/세션 정보  
- 서비스별 비즈니스 데이터
- Auto increment의 step을 2로 하고 odd/even 설정으로 고유번호의 충돌이 없도록 설계


---

## 6. 장점 요약

| 항목 | 설명 |
|------|------|
| 고가용성 | Web, App, DB 이중화 및 마스터-마스터 복제로 무중단 서비스 제공 |
| 확장성 | 서비스 단위 수평 확장 가능 |
| 보안성 | 중앙 Credential 서비스를 통한 접근 제어 |
| 유연성 | API Gateway에서 동적 라우팅 및 로그 처리 가능 |
| 격리성 | 서비스 간 장애 격리 및 독립 운영 가능 |

---

## 7. 향후 고려 사항

- Credential 서비스 HA 구성 및 상태 모니터링  
- DB 복제 충돌 처리 정책 수립  
- 로그의 중앙 수집 및 보안성 확보  
- 배포 및 운영 자동화 도구 적용 (ex. CI/CD, Health Monitoring)
