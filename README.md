# pen


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
