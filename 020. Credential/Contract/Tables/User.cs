using MemoryPack; 
using System; 
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

[MemoryPackable] public partial class User
{
    [MemoryPackInclude] protected int seq;
    [MemoryPackInclude] protected string user_id;
    [MemoryPackInclude] protected string user_name;
    [MemoryPackInclude] protected string department_id;
    [MemoryPackInclude] protected string department_name;
    [MemoryPackInclude] protected string email;
    [MemoryPackInclude] protected string mobile;

    public event Action<int> OnSeqChanged;
    public event Action<string> OnUserIdChanged;
    public event Action<string> OnUserNameChanged;
    public event Action<string> OnDepartmentIdChanged;
    public event Action<string> OnDepartmentNameChanged;
    public event Action<string> OnEmailChanged;
    public event Action<string> OnMobileChanged;

    public int Seq { get => seq; set { if (seq != value) { seq = value; OnSeqChanged?.Invoke(value); } } }
    public string UserId { get => user_id; set { if (user_id != value) { user_id = value; OnUserIdChanged?.Invoke(value); } } }
    public string UserName { get => user_name; set { if (user_name != value) { user_name = value; OnUserNameChanged?.Invoke(value); } } }
    public string DepartmentId { get => department_id; set { if (department_id != value) { department_id = value; OnDepartmentIdChanged?.Invoke(value); } } }
    public string DepartmentName { get => department_name; set { if (department_name != value) { department_name = value; OnDepartmentNameChanged?.Invoke(value); } } }
    public string Email { get => email; set { if (email != value) { email = value; OnEmailChanged?.Invoke(value); } } }
    public string Mobile { get => mobile; set { if (mobile != value) { mobile = value; OnMobileChanged?.Invoke(value); } } }
}

public class UserApiServiceModel : User 
{
    public const string TableName = "user";
    public const string SEQ = "seq";
    public const string USER_ID = "user_id";
    public const string USER_NAME = "user_name";
    public const string DEPARTMENT_ID = "department_id";
    public const string DEPARTMENT_NAME = "department_name";
    public const string EMAIL = "email";
    public const string MOBILE = "mobile";

    public const string InsertStatement =
        $"INSERT INTO {TableName} ({USER_ID}, {USER_NAME}, {DEPARTMENT_ID}, {DEPARTMENT_NAME}, {EMAIL}, {MOBILE}) VALUES (@{USER_ID}, @{USER_NAME}, @{DEPARTMENT_ID}, @{DEPARTMENT_NAME}, @{EMAIL}, @{MOBILE});";
}

public class UserDesktopModel : User
{
    public DisplayState Display { get; } = new DisplayState();

    public UserDesktopModel()
    {
        OnSeqChanged += val => Display.SeqText = val.ToString();
        OnUserIdChanged += val => Display.UserIdText = val?.ToString();
        OnUserNameChanged += val => Display.UserNameText = val?.ToString();
        OnDepartmentIdChanged += val => Display.DepartmentIdText = val?.ToString();
        OnDepartmentNameChanged += val => Display.DepartmentNameText = val?.ToString();
        OnEmailChanged += val => Display.EmailText = val?.ToString();
        OnMobileChanged += val => Display.MobileText = val?.ToString();
    }

    public virtual void InitializeAfterDeserialize()
    {
        OnSeqChanged?.Invoke(seq);
        OnUserIdChanged?.Invoke(user_id);
        OnUserNameChanged?.Invoke(user_name);
        OnDepartmentIdChanged?.Invoke(department_id);
        OnDepartmentNameChanged?.Invoke(department_name);
        OnEmailChanged?.Invoke(email);
        OnMobileChanged?.Invoke(mobile);
    }
    
    public partial class DisplayState : ObservableObject
    {
        [ObservableProperty] private string seqText;
        [ObservableProperty] private string userIdText;
        [ObservableProperty] private string userNameText;
        [ObservableProperty] private string departmentIdText;
        [ObservableProperty] private string departmentNameText;
        [ObservableProperty] private string emailText;
        [ObservableProperty] private string mobileText;
    }
}
