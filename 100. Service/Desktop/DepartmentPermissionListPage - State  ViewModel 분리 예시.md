# DepartmentPermissionListPage - State / ViewModel 분리 예시

---

## 1. State (비즈니스 로직 + 상태 관리)

- API 호출 담당  
- 데이터 보관  
- 상태 변경 이벤트 발생  

```csharp
public class DepartmentPermissionListState
{
    public event EventHandler? PermissionsChanged;

    private List<DepartmentPermission> _permissions = new();
    public IReadOnlyList<DepartmentPermission> Permissions => _permissions;

    private string _searchKeyword = string.Empty;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (_searchKeyword != value)
            {
                _searchKeyword = value;
                // 필요 시 별도 이벤트 발생 가능
            }
        }
    }

    // API 호출 및 데이터 갱신 메서드
    public async Task LoadPermissionsAsync()
    {
        // API에 SearchKeyword 파라미터 포함해서 호출
        var result = await ApiClient.InitializeListPageAsync(new { DepartmentId = _searchKeyword });

        _permissions = result ?? new List<DepartmentPermission>();
        PermissionsChanged?.Invoke(this, EventArgs.Empty);
    }
}
```

---

## 2. ViewModel (UI 바인딩 담당)

- State를 생성 및 구독  
- 바인딩용 ObservableCollection 보유  
- State 이벤트에 반응해 UI 속성 갱신  
- UI 입력 이벤트 처리 시 State에 값 반영 및 API 재호출 요청

```csharp
public class DepartmentPermissionListViewModel : ObservableObject
{
    private readonly DepartmentPermissionListState _state;

    [ObservableProperty]
    private ObservableCollection<DepartmentPermission> permissions = new();

    [ObservableProperty]
    private string searchKeyword = string.Empty;

    public DepartmentPermissionListViewModel()
    {
        _state = new DepartmentPermissionListState();
        _state.PermissionsChanged += OnPermissionsChanged;
    }

    private void OnPermissionsChanged(object? sender, EventArgs e)
    {
        // State의 최신 Permissions로 갱신
        Permissions.Clear();
        foreach (var p in _state.Permissions)
            Permissions.Add(p);
    }

    // UI에서 검색 키워드가 변경되면 호출
    public async Task UpdateSearchKeywordAsync(string keyword)
    {
        if (searchKeyword != keyword)
        {
            searchKeyword = keyword;
            _state.SearchKeyword = Uri.UnescapeDataString(keyword);  // URL 디코딩
            await _state.LoadPermissionsAsync();
            OnPropertyChanged(nameof(SearchKeyword));
        }
    }

    // 초기 로딩 시 호출 (URL로부터 디코딩된 키워드 주입)
    public async Task InitializeAsync(string encodedKeyword)
    {
        searchKeyword = Uri.UnescapeDataString(encodedKeyword ?? string.Empty);
        _state.SearchKeyword = searchKeyword;
        await _state.LoadPermissionsAsync();
        OnPropertyChanged(nameof(SearchKeyword));
    }
}
```

---

## 요약

| 구분     | 역할 및 코드 내용                                                  |
|----------|------------------------------------------------------------------|
| **State**  | API 호출, 상태 저장, 이벤트 통지, 비즈니스 로직                       |
| **ViewModel** | UI 바인딩용 ObservableCollection, State 이벤트 구독 및 UI 갱신, UI → State 입력 반영 |

---

필요하면 View (XAML + Code-behind)에서 ViewModel `InitializeAsync` 호출하고,  
검색어 TextBox 변경 이벤트에서 `UpdateSearchKeywordAsync` 호출하는 식으로 연동하시면 됩니다.

더 구체적인 예시나 개선 방향 원하시면 알려주세요!
