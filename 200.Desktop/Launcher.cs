// Program.cs
using System;
using System.IO.Pipes;
using System.Diagnostics;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("No URL provided.");
            return;
        }

        string url = args[0];
        string pipeName = "MyWpfAppPipe";

        bool sent = false;
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            client.Connect(500); // 500ms timeout
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine(url);
            sent = true;
        }
        catch
        {
            Console.WriteLine("No WPF process found.");
        }

        if (!sent)
        {
            // 실행 중이 아니면 WPF exe를 실행
            string exePath = @"%LocalAppData%\Apps\2.0\<your-folder>\WpfApp.exe"; // 정확한 경로로 변경
            Process.Start(exePath, url);
        }
    }
}
