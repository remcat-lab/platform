# ConfigureAwait 사용 권장사항 정리

| 플랫폼          | SynchronizationContext 존재 여부          | ConfigureAwait 권장 설정       | 이유 및 설명                                         |
|-----------------|------------------------------------------|-------------------------------|-----------------------------------------------------|
| **ASP.NET Core** | 기본적으로 **SynchronizationContext 없음** | `ConfigureAwait(false)` 권장   | Context 복귀가 없으므로 불필요한 컨텍스트 캡처 비용 제거로 성능 향상 가능 |
| **WPF (UI 앱)**   | **SynchronizationContext (DispatcherContext) 존재** | `ConfigureAwait(true)` 권장 (기본값) | UI 스레드 복귀가 필요하며, UI 관련 코드가 안전하게 실행되도록 하기 위해서 |

---

## 부가 설명

- ASP.NET Core는 `SynchronizationContext`가 없기 때문에,  
  `ConfigureAwait(false)` 사용 시 컨텍스트 전환 비용을 줄여 성능 최적화가 가능하다.

- WPF는 UI 스레드에서만 UI 작업이 안전하게 실행되므로,  
  기본값인 `ConfigureAwait(true)`로 UI 스레드 복귀를 보장하는 것이 안전하다.

- WPF에서 UI와 무관한 백그라운드 작업만 수행할 경우에는  
  `ConfigureAwait(false)`를 사용해도 무방하며, 성능 최적화에 도움될 수 있다.

---


## httpResponse의 BodyWriter Async는 aborted가 있어야 한다.
- 클라이언트가 요청을 중단하면 서버도 작업을 중단해야 자원을 낭비하지 않게 됩니다.
- 이를 위해 ASP.NET Core는 context.RequestAborted라는 CancellationToken을 제공합니다.
- 이 토큰을 WriteAsync 같은 비동기 I/O 작업에 넘기지 않으면, 클라이언트가 연결을 끊었을 때도 서버가 계속 데이터를 쓰려 할 수 있습니다 → 자원 낭비 또는 예외 발생 가능.

```code
await context.Response.BodyWriter.WriteAsync(result, context.RequestAborted);
```
