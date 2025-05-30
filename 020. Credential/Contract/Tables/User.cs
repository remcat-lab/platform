using MemoryPack;
using System;

[MemoryPackable]
public partial class User
{
    // Protected fields with MemoryPackInclude (without index)
    [MemoryPackInclude]
    protected int seq;

    [MemoryPackInclude]
    protected string user_id;

    [MemoryPackInclude]
    protected string user_name;

    [MemoryPackInclude]
    protected string department_id;

    [MemoryPackInclude]
    protected string department_name;

    [MemoryPackInclude]
    protected string email;

    [MemoryPackInclude]
    protected string mobile;

    // Events
    public event Action<int> OnSeqChanged;
    public event Action<string> OnUserIdChanged;
    public event Action<string> OnUserNameChanged;
    public event Action<string> OnDepartmentIdChanged;
    public event Action<string> OnDepartmentNameChanged;
    public event Action<string> OnEmailChanged;
    public event Action<string> OnMobileChanged;

    // Properties
    public int Seq
    {
        get => seq;
        set
        {
            if (seq != value)
            {
                seq = value;
                OnSeqChanged?.Invoke(value);
            }
        }
    }

    public string UserId
    {
        get => user_id;
        set
        {
            if (user_id != value)
            {
                user_id = value;
                OnUserIdChanged?.Invoke(value);
            }
        }
    }

    public string UserName
    {
        get => user_name;
        set
        {
            if (user_name != value)
            {
                user_name = value;
                OnUserNameChanged?.Invoke(value);
            }
        }
    }

    public string DepartmentId
    {
        get => department_id;
        set
        {
            if (department_id != value)
            {
                department_id = value;
                OnDepartmentIdChanged?.Invoke(value);
            }
        }
    }

    public string DepartmentName
    {
        get => department_name;
        set
        {
            if (department_name != value)
            {
                department_name = value;
                OnDepartmentNameChanged?.Invoke(value);
            }
        }
    }

    public string Email
    {
        get => email;
        set
        {
            if (email != value)
            {
                email = value;
                OnEmailChanged?.Invoke(value);
            }
        }
    }

    public string Mobile
    {
        get => mobile;
        set
        {
            if (mobile != value)
            {
                mobile = value;
                OnMobileChanged?.Invoke(value);
            }
        }
    }
}
