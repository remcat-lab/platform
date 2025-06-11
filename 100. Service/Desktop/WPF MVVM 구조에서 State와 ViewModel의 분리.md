# WPF MVVM 구조에서 State와 ViewModel의 분리

## 개요

이 문서는 WPF MVVM 패턴에서 `State`, `ViewModel`, `Code-behind`의 역할 분리 및 흐름을 설명하고, 실제 코드 예시를 통해 구조적 이해를 돕습니다.

---

## 역할 정의

| 구성요소        | 역할 및 책임 |
|----------------|--------------|
| **View (XAML)** | UI 정의 및 사용자 입력 처리 (코드 비하인드 포함) |
| **ViewModel**   | View와의 바인딩 담당, 상태(State) 변경 감지 및 전달 |
| **State**       | 비즈니스 로직, API 호출, 데이터 보관 및 가공 |
| **Model / DTO** | API 통신 전용 데이터 구조 (Params, Result 등) |

---

## 핵심 설계 원칙

- ViewModel은 UI에 필요한 ObservableProperty만 보유
- State는 로직과 데이터 처리 담당
- ViewModel은 State의 이벤트를 구독하여 자신의 속성을 변경
- View (Code-behind)는 ViewModel만 참조하고, 필요한 경우 ViewModel 메서드를 호출하여 State를 간접 제어

---

## 흐름 예시: 리스트 초기화

1. 페이지 URL을 통해 전달된 QueryString을 디코딩
2. ViewModel이 State에 초기화 요청
3. State는 API 호출 및 결과 가공 → 이벤트 발생
4. ViewModel은 해당 이벤트 구독하여 ObservableProperty 갱신
5. View는 ViewModel과 바인딩된 데이터를 통해 화면 갱신

---

## 코드 예시

### 📦 API DTO

```csharp
// API 응답 DTO
public class DefaultApiResult
{
    public HashSet<Department> Departments { get; set; }
    public HashSet<DepartmentPermission> DepartmentPermissions { get; set; }
    public HashSet<User> Users { get; set; }
    public HashSet<UserPermission> UserPermissions { get; set; }
}

public class DepartmentPermissionApiResult : DefaultApiResult
{
    public string Summary { get; set; } // 확장 가능
}
```

### 📦 State

```csharp
public class DepartmentPermissionListState
{
    public event Action? DataUpdated;

    private List<DepartmentPermission> _items = new();
    public IReadOnlyList<DepartmentPermission> Items => _items;

    public async Task InitializeAsync(DepartmentPermissionListParams param)
    {
        var result = await Api.SendAsync<DepartmentPermissionListParams, DepartmentPermissionApiResult>(param);

        _items = result.DepartmentPermissions.ToList();

        DataUpdated?.Invoke();
    }

    public async Task DeleteAsync(DepartmentPermission item)
    {
        await Api.SendAsync(new DeleteDepartmentPermissionParams { Id = item.Id });
        _items.Remove(item);
        DataUpdated?.Invoke();
    }
}
```

### 📦 ViewModel

```csharp
public class DepartmentPermissionListViewModel : ObservableObject
{
    private readonly DepartmentPermissionListState _state;

    [ObservableProperty]
    private ObservableCollection<DepartmentPermission> _items = new();

    public DepartmentPermissionListViewModel(DepartmentPermissionListState state)
    {
        _state = state;
        _state.DataUpdated += OnStateDataUpdated;
    }

    private void OnStateDataUpdated()
    {
        Items = new ObservableCollection<DepartmentPermission>(_state.Items);
    }

    public async Task InitializeAsync(string url)
    {
        var query = ParseQuery(url);
        var param = new DepartmentPermissionListParams
        {
            DepartmentId = int.Parse(query["departmentId"]),
            SearchKeyword = query["keyword"],
            Codes = JsonSerializer.Deserialize<List<string>>(query["codes"])
        };

        await _state.InitializeAsync(param);
    }

    private Dictionary<string, string> ParseQuery(string url)
    {
        // URL 쿼리 파싱 로직
    }

    public async Task DeleteAsync(DepartmentPermission selectedItem)
    {
        await _state.DeleteAsync(selectedItem);
    }
}
```

### 📦 Code Behind

```csharp
private async void Page_Loaded(object sender, RoutedEventArgs e)
{
    var viewModel = (DepartmentPermissionListViewModel)DataContext;
    await viewModel.InitializeAsync(NavigationService.CurrentUrl);
}

private async void DeleteButton_Click(object sender, RoutedEventArgs e)
{
    var selectedItem = dataGrid.SelectedItem as DepartmentPermission;
    if (selectedItem != null)
    {
        await viewModel.DeleteAsync(selectedItem);
    }
}
```

---

## 분리 기준 정리

- **State는 View에 종속**: State는 View가 필요로 하는 도메인 중심으로 구성되며, API의 Result 전체를 보관하지 않고 필요한 데이터만 추려서 보관합니다.
- **ViewModel은 중개자 역할**: ViewModel은 View와 State 사이에서 데이터 동기화 및 이벤트 구독 역할만 담당합니다.
- **코드 비하인드는 UI 이벤트 전용**: 로직은 ViewModel/State로 위임하고, 화면 전환, 포커스, 다이얼로그 등 UI 제어만 담당합니다.

---

## 장점

- State를 통한 로직 집중 → ViewModel 경량화
- ViewModel → UI 바인딩 책임만 유지 → 테스트 용이
- View는 최소한의 코드만 → 구조 명확

---

## 확장 팁

- State를 기능 단위로 쪼갤 수 있음 (예: `DepartmentState`, `UserState` 등)
- ViewModel은 Composition 패턴으로 여러 State를 조합 가능
- 이벤트 대신 Observable 패턴 사용도 가능 (Reactive 확장)
