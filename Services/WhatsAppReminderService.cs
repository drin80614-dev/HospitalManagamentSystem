using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HospitalManagamentSystem.Models;
using Microsoft.Extensions.Options;

namespace HospitalManagamentSystem.Services;

public class WhatsAppReminderService : IWhatsAppReminderService
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<WhatsAppReminderService> _logger;

    public WhatsAppReminderService(HttpClient httpClient, IOptions<WhatsAppOptions> options, ILogger<WhatsAppReminderService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error)> SendAppointmentReminderAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return (false, "WhatsApp is not configured.");
        }

        var phone = NormalizePhone(appointment.PatientPhone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            return (false, "Patient phone number is missing.");
        }

        var endpoint = $"https://graph.facebook.com/{_options.ApiVersion}/{_options.PhoneNumberId}/messages";
        var message = BuildMessage(appointment);
        var payload = new
        {
            messaging_product = "whatsapp",
            to = phone,
            type = "text",
            text = new
            {
                preview_url = false,
                body = message
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            _logger.LogWarning("WhatsApp reminder failed for appointment {AppointmentId}: {StatusCode} {Body}", appointment.Id, response.StatusCode, body);
            return (false, $"WhatsApp API error: {(int)response.StatusCode}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "WhatsApp reminder failed for appointment {AppointmentId}", appointment.Id);
            return (false, ex.Message);
        }
    }

    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
        {
            return null;
        }

        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }
        else if (digits.StartsWith("0", StringComparison.Ordinal))
        {
            digits = "383" + digits[1..];
        }

        return digits.Length >= 8 ? digits : null;
    }

    public string BuildMessage(Appointment appointment)
    {
        var service = appointment.ServiceNames ?? appointment.ServiceName ?? "kontrollen/trajtimin tuaj";
        return $"Pershendetje {appointment.PatientName}, ju rikujtojme terminin tuaj ne {_options.ClinicName} me {appointment.AppointmentDate:dd.MM.yyyy} ora {appointment.AppointmentTime:hh\\:mm}. Sherbimi: {service}. Ju lutemi paraqituni me kohe. Faleminderit!";
    }
}
