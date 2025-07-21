``` mermaid

sequenceDiagram
    autonumber
    participant Browser/OS as "Browser / OS"
    participant Launcher as "Launcher.exe (ConsoleApp)"
    participant WPF as "WPF App"
    participant PipeServer as "WPF PipeServer"
    participant ClickOnce as "ClickOnce Runtime"

    Browser/OS->>Launcher: 사용자 클릭 (myapp://some/path)

    Note over Launcher: 실행됨<br>args[0] = URL

    Launcher->>PipeServer: NamedPipe 연결 시도
    alt WPF 실행 중
        PipeServer-->>Launcher: 연결 성공
        Launcher->>PipeServer: URL 전달
        Launcher-->>Launcher: 종료
        PipeServer->>WPF: HandleUrl(url)
        WPF->>WPF: CheckForUpdate()
        alt 업데이트 필요
            WPF->>ClickOnce: Update + Restart
        else 최신 상태
            WPF->>WPF: Navigate(url)
        end
    else WPF 실행 안됨
        Launcher-->>Launcher: Pipe 연결 실패
        Launcher->>WPF: 로컬 exe 실행 + URL 전달
        WPF->>WPF: MainWindow 띄움
        WPF->>PipeServer: NamedPipe 리스닝 시작
        WPF->>WPF: HandleUrl(url)
        WPF->>WPF: CheckForUpdate()
        alt 업데이트 필요
            WPF->>ClickOnce: Update + Restart
        else 최신 상태
            WPF->>WPF: Navigate(url)
        end
    end

```
