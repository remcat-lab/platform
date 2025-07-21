using System;
using System.IO.Pipes;
using System.IO;
using System.Threading.Tasks;
using System.Deployment.Application;
using System.Windows;

public partial class App : Application
{
    private const string PipeName = "MyWpfAppPipe";
    private System.Threading.Mutex _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        bool created;
        _mutex = new System.Threading.Mutex(true, "MyWpfAppMutex", out created);

        if (!created)
        {
            // 다른 인스턴스가 이미 실행 중이면 종료
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // URL 인자 처리
        if (e.Args.Length > 0)
        {
            HandleUrl(e.Args[0]);
        }

        // Pipe 서버 시작
        StartPipeServer();
    }

    private void StartPipeServer()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                    await server.WaitForConnectionAsync();

                    using var reader = new StreamReader(server);
                    string url = await reader.ReadLineAsync();

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        Application.Current.Dispatcher.Invoke(() => HandleUrl(url));
                    }
                }
                catch { /* 로그 기록 */ }
            }
        });
    }

    private void HandleUrl(string url)
    {
        // 업데이트 체크 후 처리
        CheckForUpdateAndHandleUrl(url);
    }

    private void CheckForUpdateAndHandleUrl(string url)
    {
        if (ApplicationDeployment.IsNetworkDeployed)
        {
            var ad = ApplicationDeployment.CurrentDeployment;

            try
            {
                if (ad.CheckForUpdate())
                {
                    ad.Update();
                    ApplicationDeployment.CurrentDeployment.Update();
                    Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
                    System.Windows.Forms.Application.Restart();
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update check failed: {ex.Message}");
            }
        }

        // 업데이트가 없거나 실패했으면 URL 처리
        ShowMainWindowWithUrl(url);
    }

    private void ShowMainWindowWithUrl(string url)
    {
        var mainWindow = Current.MainWindow as MainWindow ?? new MainWindow();
        mainWindow.Show();
        mainWindow.NavigateTo(url);
    }
}
