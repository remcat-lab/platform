
# WPF MVVM에서 ViewModel과 State 분리 구조 예시

## 🧭 전체 흐름: 페이지 진입 → API 호출 → UI 갱신

### 구성 역할

| 구성 요소 | 책임 |
|-----------|------|
| **State** | API 호출, 상태 저장, 이벤트 발생 |
| **ViewModel** | 바인딩용 속성 보유, State 이벤트 구독 및 갱신 |
| **View (code-behind)** | ViewModel의 메서드 호출, UI 직접 제어 |

---

## ✅ 코드 흐름 예시

### 📄 UserPageState.cs

````csharp
public class UserPageState
{
    public event EventHandler? UsersChanged;

    private List<UserDto> _users = new();
    public IReadOnlyList<UserDto> Users => _users;

    public async Task LoadUsersAsync()
    {
        var result = await ApiClient.GetUsersAsync(); // 서버 API 호출
        _users = result;
        UsersChanged?.Invoke(this, EventArgs.Empty); // 상태 변경 통지
    }
}
````

---

### 📄 UserPageViewModel.cs

````csharp
public class UserPageViewModel : ObservableObject
{
    private readonly UserPageState _state;

    [ObservableProperty]
    private ObservableCollection<UserDto> users = new();

    [ObservableProperty]
    private bool isLoading;

    public UserPageViewModel(UserPageState state)
    {
        _state = state;
        _state.UsersChanged += OnUsersChanged;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        await _state.LoadUsersAsync();
        IsLoading = false;
    }

    private void OnUsersChanged(object? sender, EventArgs e)
    {
        Users.Clear();
        foreach (var user in _state.Users)
            Users.Add(user);
    }
}
````

---

### 📄 UserPage.xaml.cs (code-behind)

````csharp
private async void UserPage_Loaded(object sender, RoutedEventArgs e)
{
    await ViewModel.LoadAsync(); // View 진입 시 데이터 로딩
}
````

---

## 📌 흐름 시각화

```
[UserPage.xaml.cs]
  └─> ViewModel.LoadAsync()
       ├─> State.LoadUsersAsync() → API 호출
       └─> State.UsersChanged 이벤트
              └─> ViewModel.Users 갱신 → UI 바인딩 자동 반영
```

---

## 🧠 구조적 장점

- State 분리: 복잡한 상태 처리/비즈니스 로직 격리
- ViewModel 경량화: 바인딩과 이벤트 연결만 담당
- 테스트 용이성: State 단위 테스트, ViewModel UI 중심 테스트
- 역할 명확: 유지보수와 협업에 유리

---

## ✅ 결론

> **State**는 "모든 상태와 비즈니스 로직, 외부 I/O"  
> **ViewModel**은 "UI가 바인딩할 데이터 표현과 이벤트 연결"  
> **View (code-behind)**는 "UI 이벤트의 진입점"
