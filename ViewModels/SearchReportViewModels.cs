using HospitalManagamentSystem.Models;

namespace HospitalManagamentSystem.ViewModels;

public class GlobalSearchViewModel
{
    public string Query { get; set; } = string.Empty;
    public IReadOnlyList<Patient> Patients { get; set; } = [];
    public IReadOnlyList<Doctor> Doctors { get; set; } = [];
    public IReadOnlyList<Room> Rooms { get; set; } = [];
    public IReadOnlyList<Appointment> Appointments { get; set; } = [];
}

public class ReportsViewModel
{
    public DateTime From { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime To { get; set; } = DateTime.Today;
    public IReadOnlyList<DailyPatientReportRow> DailyPatients { get; set; } = [];
    public IReadOnlyList<PaymentReportRow> Payments { get; set; } = [];
    public IReadOnlyList<DoctorPerformanceRow> DoctorPerformance { get; set; } = [];
    public IReadOnlyList<RoomOccupancyRow> RoomOccupancy { get; set; } = [];
    public IReadOnlyList<DiagnosisReportRow> Diagnoses { get; set; } = [];
    public IReadOnlyList<AppointmentReportRow> Appointments { get; set; } = [];
    public IReadOnlyList<MonthlyClinicReportRow> MonthlyClinic { get; set; } = [];
    public IReadOnlyList<ServicePerformanceReportRow> ServicePerformance { get; set; } = [];

    public int TotalPatients => DailyPatients.Sum(row => row.RegisteredPatients);
    public int TotalAppointments => Appointments.Sum(row => row.AppointmentCount);
    public decimal TotalBilled => Payments.Sum(row => row.TotalAmount);
    public decimal TotalPaid => Payments.Sum(row => row.PaidAmount);
    public decimal TotalRemaining => Payments.Sum(row => row.BalanceAmount);
}

public class InventoryViewModel
{
    public IReadOnlyList<MedicationInventoryItem> Items { get; set; } = [];
    public string? Search { get; set; }
    public bool LowStockOnly { get; set; }
}

public class NotificationsViewModel
{
    public IReadOnlyList<Notification> Notifications { get; set; } = [];
    public int UnreadCount { get; set; }
}

public class DailyPatientReportRow
{
    public DateTime ReportDate { get; set; }
    public int RegisteredPatients { get; set; }
    public int AdmittedPatients { get; set; }
    public int DischargedPatients { get; set; }
}

public class PaymentReportRow
{
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public int PaymentCount { get; set; }
}

public class DoctorPerformanceRow
{
    public string DoctorName { get; set; } = string.Empty;
    public int VisitsCompleted { get; set; }
    public int DiagnosesCreated { get; set; }
    public int PrescriptionsCreated { get; set; }
}

public class RoomOccupancyRow
{
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DiagnosisReportRow
{
    public string DiseaseName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int DiagnosisCount { get; set; }
}

public class AppointmentReportRow
{
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AppointmentCount { get; set; }
}

public class MonthlyClinicReportRow
{
    public DateTime Month { get; set; }
    public int NewPatients { get; set; }
    public int AppointmentCount { get; set; }
    public int CompletedAppointments { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingBalance { get; set; }
}

public class ServicePerformanceReportRow
{
    public string ServiceName { get; set; } = string.Empty;
    public int PaymentCount { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingBalance { get; set; }
}
