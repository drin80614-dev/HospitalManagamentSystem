using HospitalManagamentSystem.Data;
using Microsoft.Extensions.Options;

namespace HospitalManagamentSystem.Services;

public class AppointmentReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<AppointmentReminderWorker> _logger;

    public AppointmentReminderWorker(IServiceScopeFactory scopeFactory, IOptions<WhatsAppOptions> options, ILogger<AppointmentReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("WhatsApp appointment reminders are disabled.");
            return;
        }

        if (!_options.IsConfigured)
        {
            _logger.LogWarning("WhatsApp reminders are enabled, but AccessToken or PhoneNumberId is missing.");
            return;
        }

        var pollDelay = TimeSpan.FromMinutes(Math.Max(1, _options.PollMinutes));
        var leadHours = Math.Max(1, _options.ReminderLeadHours);
        var batchSize = Math.Clamp(_options.BatchSize, 1, 100);

        while (!stoppingToken.IsCancellationRequested)
        {
            await SendDueRemindersAsync(leadHours, batchSize, stoppingToken);
            await Task.Delay(pollDelay, stoppingToken);
        }
    }

    private async Task SendDueRemindersAsync(int leadHours, int batchSize, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<HospitalRepository>();
            var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppReminderService>();

            var appointments = await repository.GetDueWhatsAppReminderAppointmentsAsync(DateTime.Now, leadHours, batchSize);
            foreach (var appointment in appointments)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                var result = await whatsApp.SendAppointmentReminderAsync(appointment, stoppingToken);
                await repository.MarkAppointmentReminderAsync(
                    appointment.Id,
                    result.Success ? "Sent" : "Failed",
                    result.Error);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Automatic WhatsApp appointment reminder cycle failed.");
        }
    }
}
