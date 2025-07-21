using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows;
using System.Deployment.Application;

namespace MyWpfApp
{
    public partial class App : Application
    {
        private const string PipeName = "MyWpfAppPipe";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string initialUrl = e.Args.Length > 0 ? e.Args[0] : null;

            try
            {
                // 시도: PipeServer 생성
                var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                // PipeServer 생성 성공 → 내가 첫 번째 인스턴스
                StartPipeServer(server);

                ShowMainWindowAndNavigate(initialUrl);
            }
            catch (IOException)
            {
                // PipeServer 생성 실패 → 이미 실행 중
                if (!string.IsNullOrEmpty(initialUrl))
                {
                    SendUrlToExistingInstance(initialUrl);
                }

                Shutdown(); // 현재 인스턴스 종료
            }
        }

        private void StartPipeServer(NamedPipeServerStream server)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await server.WaitForConnectionAsync();

                    using var reader = new StreamReader(server);
                    string url = await reader.ReadLineAsync();

                    Application.Current.Dispatcher.Invoke(() => HandleUrl(url));

                    server.Disconnect(); // 다음 연결을 위해
                }
            });
        }

        private void SendUrlToExistingInstance(string url)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(500); // 500ms timeout

                using var writer = new StreamWriter(client) { AutoFlush = true };
                writer.WriteLine(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"기존 인스턴스와 통신 실패: {ex.Message}", "Error");
            }
        }

        private void ShowMainWindowAndNavigate(string url)
        {
            var mainWindow = new MainWindow();
            Current.MainWindow = mainWindow;
            mainWindow.Show();

            if (!string.IsNullOrEmpty(url))
            {
                HandleUrl(url);
            }
        }

        private void HandleUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            // 업데이트 확인
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                var ad = ApplicationDeployment.CurrentDeployment;

                try
                {
                    if (ad.CheckForUpdate())
                    {
                        ad.Update();
                        Application.Restart();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"업데이트 확인 실패: {ex.Message}", "Update Error");
                }
            }

            // 업데이트가 없으면 URL 처리
            if (Current.MainWindow is MainWindow mw)
            {
                mw.NavigateTo(url);
            }
        }
    }
}
