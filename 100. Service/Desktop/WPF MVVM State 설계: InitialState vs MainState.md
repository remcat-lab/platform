# WPF MVVM State 설계: InitialState vs MainState

## 배경
WPF MVVM 구조에서 페이지는 외부 입력 (예: URL)과 사용자 입력 (예: UI)을 동시에 다루어야 합니다. 이에 따라 상태(State)의 역할을 분리할 필요가 생깁니다.

## 입력(Input)과 출력(Output)의 분류

### Input의 분류 기준

| 분류 기준 | 예시 | 설명 |
|-----------|------|------|
| Source 기준 | External Input | URL, QueryString, 서버에서 받은 초기값 등 |
|            | Internal Input | 사용자의 UI 입력 (TextBox, ComboBox 등) |
| Timing 기준 | Initial Input | 페이지 진입 시 필요한 입력 (초기 데이터) |
|            | Dynamic Input | 사용자 액션에 따라 변화하는 입력 |
| Persistence 기준 | Volatile Input | 임시 입력 (TextBox에 입력 중인 값 등) |
|                | Persistent Input | 저장 가능한 입력 (Form 저장 등) |

### Output의 분류 기준

| 분류 기준 | 예시 | 설명 |
|-----------|------|------|
| 목적 기준 | View 변경 | 다른 페이지로 전환, 상태 변화 |
|          | API 호출 | 데이터 저장, 삭제, 상태 변경 |
|          | UI Feedback | 알림, 메시지, 유효성 검사 표시 등 |

---

## State 구조 제안

### 1. InitialState (또는 ExternalState)

- **역할**: URL 등 외부 입력을 기반으로 초기 API를 호출하기 위한 파라미터 보관
- **예시**:
  ```csharp
  public class InitialState
  {
      public string UrlPrefix { get; set; }
      public int GrantedDepartmentId { get; set; }
  }
  ```

### 2. MainState (또는 InternalState)

- **역할**: 초기 데이터 결과 및 사용자 입력을 함께 보관
- **구성**:
  - `InitialResult`: 서버 초기 응답 데이터 저장
  - `FormInput`: 사용자 입력 값 저장

- **예시 (partial class)**:
  ```csharp
  public partial class MainState
  {
      public DepartmentPermissionDto? Current { get; set; }
      public HashSet<DepartmentDto> Departments { get; set; } = new();
      public HashSet<UserDto> Users { get; set; } = new();
  }

  public partial class MainState
  {
      public int SelectedDepartmentId { get; set; }
      public int SelectedUserId { get; set; }
      public string Memo { get; set; } = string.Empty;
  }
  ```

---

## ViewModel 흐름 예시

```csharp
public class DepartmentPermissionCreateViewModel
{
    public InitialState InitialState { get; } = new();
    public MainState MainState { get; } = new();

    public async Task InitializeAsync()
    {
        var result = await api.InitializeAsync(InitialState);

        MainState.Current = result.DepartmentPermission;
        MainState.Departments = result.Departments;
        MainState.Users = result.Users;
    }

    public async Task SubmitAsync()
    {
        var dto = new DepartmentPermissionDto
        {
            DepartmentId = MainState.SelectedDepartmentId,
            UserId = MainState.SelectedUserId,
            Memo = MainState.Memo,
        };

        await api.SubmitAsync(dto);
    }
}
```

---

## Naming 제안

| 목적 | 제안 이름 | 대안 |
|------|-----------|------|
| 외부 입력 저장 | `InitialState`, `ExternalState` | `QueryParameterState` |
| 내부 상태 저장 | `MainState`, `InternalState` | `FormState`, `InputState` |

---

## 결론

- `InitialState`는 페이지 진입 시 필요한 외부 입력의 저장 및 API 호출의 기반
- `MainState`는 초기 결과와 사용자 입력 상태를 포함
- `partial class`를 통해 MainState 내 역할을 파일 수준에서 분리 가능
- 이러한 구조는 SRP와도 어느 정도 조화를 이루며 확장성과 가독성을 모두 고려한 방식임
