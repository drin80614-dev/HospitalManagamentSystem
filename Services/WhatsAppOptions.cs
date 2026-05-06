namespace HospitalManagamentSystem.Services;

public class WhatsAppOptions
{
    public bool Enabled { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v20.0";
    public int ReminderLeadHours { get; set; } = 24;
    public int PollMinutes { get; set; } = 5;
    public int BatchSize { get; set; } = 25;
    public string ClinicName { get; set; } = "Vlera Dent";

    public bool IsConfigured =>
        Enabled &&
        !string.IsNullOrWhiteSpace(AccessToken) &&
        !string.IsNullOrWhiteSpace(PhoneNumberId);
}
