using HospitalManagamentSystem.Models;

namespace HospitalManagamentSystem.ViewModels;

public class DashboardViewModel
{
    public string Role { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public IReadOnlyList<DashboardMetric> Metrics { get; set; } = [];
    public IReadOnlyList<Patient> RecentPatients { get; set; } = [];
    public IReadOnlyList<Appointment> TodayAppointments { get; set; } = [];
    public IReadOnlyList<Doctor> AvailableDoctors { get; set; } = [];
    public IReadOnlyList<Room> AvailableRooms { get; set; } = [];
    public IReadOnlyList<Diagnosis> RecentDiagnoses { get; set; } = [];
    public IReadOnlyList<Payment> PendingPayments { get; set; } = [];
    public IReadOnlyList<AuditLog> RecentActivity { get; set; } = [];
}

public class DashboardMetric
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = "0";
    public string Icon { get; set; } = "bi-activity";
    public string Accent { get; set; } = "primary";
    public string Caption { get; set; } = string.Empty;
}
