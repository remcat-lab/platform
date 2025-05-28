# 🔧 기능 설계 문서: Active Directory 사용자 인증 및 정보 등록 기능

## 📌 개요
이 문서는 WPF 애플리케이션 내에서 **Active Directory를 통한 사용자 인증** 및 **기존 사용자 테이블 자동 등록/갱신 기능**에 대한 기술 명세서를 제공합니다. 본 기능은 Help Page를 통해 **사용자가 ID/PW를 입력**하고, AD 인증 후 사용자 정보를 LDAP에서 조회하여 내부 DB(User Table)에 자동으로 저장하는 구조입니다.

---

## 1. 📂 파일 구조

```
/Pages
  └── Help
      └── LdapRegisterPage.xaml
      └── LdapRegisterViewModel.cs
/Core
  └── Services
      └── ActiveDirectoryService.cs
  └── Models
      └── LdapCredential.cs
      └── User.cs
```

---

## 2. 🖥️ 기능 상세

### 2.1 사용자 인증 및 등록 페이지 (LdapRegisterPage)

- 사용자로부터 ID와 비밀번호 입력
- 전송 버튼 클릭 시 Active Directory에 인증 시도
- 인증 성공 시 LDAP에서 사용자 정보 조회
- 내부 DB(User Table)에 사용자 정보 저장 또는 갱신

#### UI 구성

| 항목             | 설명                     |
|------------------|--------------------------|
| TextBox (ID)     | 사용자 ID 입력           |
| PasswordBox      | 사용자 비밀번호 입력     |
| 버튼 (전송)      | AD 인증 및 사용자 등록   |
| TextBlock        | 처리 결과 메시지 출력    |

---

## 3. 🔐 LDAP 인증 및 사용자 정보 조회

### 3.1 Credential 모델 (LdapCredential.cs)

```csharp
public class LdapCredential
{
    public string UserId { get; set; }
    public string Password { get; set; }
}
```

---

### 3.2 Active Directory 서비스 (ActiveDirectoryService.cs)

```csharp
public class ActiveDirectoryService
{
    public User AuthenticateAndFetchUser(LdapCredential credential)
    {
        using var context = new PrincipalContext(ContextType.Domain);
        bool isValid = context.ValidateCredentials(credential.UserId, credential.Password);
        if (!isValid)
            throw new UnauthorizedAccessException("AD 인증 실패");

        var userPrincipal = UserPrincipal.FindByIdentity(context, credential.UserId);
        if (userPrincipal == null)
            throw new Exception("사용자를 찾을 수 없음");

        var entry = userPrincipal.GetUnderlyingObject() as DirectoryEntry;

        return new User
        {
            UserId = userPrincipal.SamAccountName,
            UserName = userPrincipal.DisplayName,
            DepartmentId = entry?.Properties["departmentNumber"]?.Value?.ToString(),
            DepartmentName = entry?.Properties["department"]?.Value?.ToString(),
            Mobile = entry?.Properties["mobile"]?.Value?.ToString(),
            Email = userPrincipal.EmailAddress
        };
    }
}
```

---

## 4. 🗃️ 사용자 테이블 등록/갱신

### 4.1 User 모델 (User.cs)

```csharp
public class User
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string DepartmentId { get; set; }
    public string DepartmentName { get; set; }
    public string Mobile { get; set; }
    public string Email { get; set; }
}
```

### 4.2 저장 로직

```csharp
public class UserService
{
    public void SaveOrUpdateUser(User user)
    {
        // DB에서 userId로 기존 사용자 조회
        var existing = FindUserById(user.UserId);
        if (existing == null)
            InsertUser(user);
        else
            UpdateUser(user);
    }
}
```

---

## 5. ✅ 보안 및 예외 처리

- 비밀번호는 절대로 로깅되지 않으며, 메모리에서 즉시 폐기
- AD 인증 실패 시 예외 처리 및 사용자에게 알림
- 모든 예외는 사용자 친화적인 메시지로 UI에 표시

---

## 🧾 TODO 및 확장 계획

| 항목                           | 상태   |
|--------------------------------|--------|
| AD 로그인 시 로그인 기록 저장  | 미완료 |
| 사용자 권한 자동 매핑 기능     | 미완료 |
| 인증 실패 로그 기록 기능      | 미완료 |
