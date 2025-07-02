public class DailyTaskService : BackgroundService
{
    private readonly ILogger<DailyTaskService> _logger;
    private Timer? _timer;

    public DailyTaskService(ILogger<DailyTaskService> logger)
    {
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ScheduleNextRun(); // 시작 시 스케줄링
        return Task.CompletedTask;
    }

    private void ScheduleNextRun()
    {
        var now = DateTime.Now;
        var todayAt2AM = now.Date.AddHours(2);

        DateTime next;
        if (now < todayAt2AM)
            next = todayAt2AM; // 오늘 새벽 2시가 아직 안 지났다면 오늘 2시로
        else
            next = now.Date.AddDays(1).AddHours(2); // 이미 지났으면 내일 2시로

        var delay = next - now;

        _timer = new Timer(async _ =>
        {
            await DoWork();
            ScheduleNextRun(); // 작업 후 다시 스케줄링
        }, null, delay, Timeout.InfiniteTimeSpan);
    }

    private async Task DoWork()
    {
        _logger.LogInformation("🕑 2AM Job 실행됨: {time}", DateTime.Now);
        // API 호출
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync("http://localhost:5000/myapi/trigger");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("API 호출 실패: {status}", response.StatusCode);
        }
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
