``` csharp

public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            await Task.Delay(2000);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() => !IsBusy;

    partial void OnIsBusyChanged(bool oldValue, bool newValue)
    {
        SaveCommand.NotifyCanExecuteChanged();
    }
}


```
