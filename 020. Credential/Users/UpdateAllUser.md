# 🔧 기능 설계 문서: Active Directory 사용자 목록 조회 및 저장 (관리자 전용)

## 📌 개요
이 문서는 WPF 애플리케이션 내에서 Active Directory 사용자 목록을 조회하고 저장하는 기능을 개발하기 위한 기술 명세서를 제공합니다. 본 기능은 관리자 전용으로, **권한 기반 접근 제어**를 포함하며, 해당 기능이 노출되는 UI 역시 사용자 권한에 따라 동적으로 구성됩니다.

---

## 1. 📂 파일 구조

```
/Pages
  └── Admin
      └── AdUserPage.xaml
      └── AdUserPageViewModel.cs
/Core
  └── Services
      └── ActiveDirectoryService.cs
      └── PermissionService.cs
  └── Models
      └── AdUser.cs
      └── Permission.cs
/Navigation
  └── SideMenuConfiguration.cs
```

---

## 2. 🖥️ 기능 상세

### 2.1 AD 사용자 조회/저장 페이지 (AdUserPage)

- Active Directory에서 사용자 정보를 조회하여 UI에 표시
- 저장 버튼을 통해 DB에 사용자 목록을 저장
- DataGrid 형태로 사용자 리스트 출력

#### UI 구성

| 항목        | 설명                           |
|-------------|--------------------------------|
| DataGrid    | AD 사용자 목록 출력             |
| 버튼 (조회) | AD에서 사용자 목록 조회         |
| 버튼 (저장) | DB에 사용자 목록 저장           |

---

### 2.2 AD 사용자 정보 모델 (AdUser.cs)

```csharp
public class AdUser
{
    public string DisplayName { get; set; }
    public string SamAccountName { get; set; }
    public string Email { get; set; }
    public string Department { get; set; }
}
```

---

### 2.3 AD 사용자 조회 서비스 (ActiveDirectoryService.cs)

```csharp
public class ActiveDirectoryService
{
    public List<AdUser> GetAllUsers()
    {
        var context = new PrincipalContext(ContextType.Domain);
        var searcher = new PrincipalSearcher(new UserPrincipal(context));
        return searcher.FindAll()
            .OfType<UserPrincipal>()
            .Select(up => new AdUser
            {
                DisplayName = up.DisplayName,
                SamAccountName = up.SamAccountName,
                Email = up.EmailAddress,
                Department = up.GetUnderlyingObject() is DirectoryEntry entry ? entry.Properties["department"]?.Value?.ToString() : null
            })
            .ToList();
    }
}
```

---

## 3. 🔒 접근 권한 제어

### 3.1 권한 테이블 예시 (Permission)

| UserId       | PermissionCode     |
|--------------|--------------------|
| `user01`     | `Admin.AdUserPage` |

### 3.2 PermissionService.cs

```csharp
public class PermissionService
{
    private readonly Dictionary<string, List<string>> _userPermissions;

    public PermissionService()
    {
        // 예시: DB에서 로딩하여 캐싱
        _userPermissions = LoadPermissionsFromDatabase();
    }

    public bool HasAccess(string userId, string permissionCode)
    {
        return _userPermissions.TryGetValue(userId, out var perms) && perms.Contains(permissionCode);
    }

    private Dictionary<string, List<string>> LoadPermissionsFromDatabase()
    {
        // 실제 DB 호출 로직으로 대체
        return new Dictionary<string, List<string>>
        {
            { "adminUser", new List<string> { "Admin.AdUserPage" } }
        };
    }
}
```

---

## 4. 📋 SideMenu 구성 제어

### 4.1 SideMenu 항목 정의

```csharp
public class SideMenuItem
{
    public string Title { get; set; }
    public string TargetPageUrl { get; set; }
    public string RequiredPermission { get; set; }
}
```

### 4.2 권한 기반 메뉴 필터링

```csharp
public class SideMenuConfiguration
{
    public static List<SideMenuItem> GetVisibleMenuItems(string userId, PermissionService permissionService)
    {
        var allItems = new List<SideMenuItem>
        {
            new SideMenuItem { Title = "AD 사용자 관리", TargetPageUrl = "/Admin/AdUserPage", RequiredPermission = "Admin.AdUserPage" },
            // 기타 메뉴
        };

        return allItems.Where(item =>
            string.IsNullOrEmpty(item.RequiredPermission) ||
            permissionService.HasAccess(userId, item.RequiredPermission)).ToList();
    }
}
```

---

## 5. 🧪 페이지 접근 시 권한 확인

```csharp
public class AdUserPageViewModel
{
    public AdUserPageViewModel(string userId, PermissionService permissionService)
    {
        if (!permissionService.HasAccess(userId, "Admin.AdUserPage"))
            throw new UnauthorizedAccessException("접근 권한이 없습니다.");

        // 초기화 로직
    }
}
```

---

## 6. 📤 DB 저장 로직 (선택 사항)

`SaveAdUsers(List<AdUser> users)`를 통해 조회된 사용자를 데이터베이스에 저장 (DB 구조 생략)

---

## ✅ 보안 고려사항

- AD 사용자 조회는 반드시 인증된 내부 네트워크 환경에서 수행
- ViewModel 또는 Navigation 진입 시 권한 체크 필수
- UI 메뉴 비노출 + 런타임 진입 차단 이중 확인

---

## 🧾 TODO 및 향후 확장

| 항목                      | 상태   |
|---------------------------|--------|
| 권한 관리 페이지 구현     | 미완료 |
| AD 그룹별 필터 기능       | 미완료 |
| 저장 시 중복 사용자 처리 | 미완료 |
| 사용자 검색 기능 추가     | 미완료 |
