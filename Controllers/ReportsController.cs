using System.Text;
using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class ReportsController : Controller
{
    private readonly HospitalRepository _repository;

    public ReportsController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        return View(await _repository.GetReportsAsync(from ?? DateTime.Today.AddDays(-30), to ?? DateTime.Today));
    }

    public async Task<IActionResult> ExportCsv(string type, DateTime? from, DateTime? to)
    {
        var reports = await _repository.GetReportsAsync(from ?? DateTime.Today.AddDays(-30), to ?? DateTime.Today);
        var csv = new StringBuilder();

        switch ((type ?? "payments").ToLowerInvariant())
        {
            case "monthly":
                csv.AppendLine("Month,New Patients,Appointments,Completed,Total Billed,Total Paid,Remaining");
                foreach (var row in reports.MonthlyClinic)
                {
                    csv.AppendLine($"{row.Month:yyyy-MM},{row.NewPatients},{row.AppointmentCount},{row.CompletedAppointments},{row.TotalBilled},{row.TotalPaid},{row.RemainingBalance}");
                }
                break;
            case "services":
                csv.AppendLine("Service,Payments,Total Billed,Total Paid,Remaining");
                foreach (var row in reports.ServicePerformance)
                {
                    csv.AppendLine($"\"{row.ServiceName}\",{row.PaymentCount},{row.TotalBilled},{row.TotalPaid},{row.RemainingBalance}");
                }
                break;
            case "patients":
                csv.AppendLine("Date,Registered,Admitted,Discharged");
                foreach (var row in reports.DailyPatients)
                {
                    csv.AppendLine($"{row.ReportDate:yyyy-MM-dd},{row.RegisteredPatients},{row.AdmittedPatients},{row.DischargedPatients}");
                }
                break;
            case "doctors":
                csv.AppendLine("Doctor,Visits,Diagnoses,Prescriptions");
                foreach (var row in reports.DoctorPerformance)
                {
                    csv.AppendLine($"\"{row.DoctorName}\",{row.VisitsCompleted},{row.DiagnosesCreated},{row.PrescriptionsCreated}");
                }
                break;
            case "diagnoses":
                csv.AppendLine("Disease,Severity,Count");
                foreach (var row in reports.Diagnoses)
                {
                    csv.AppendLine($"\"{row.DiseaseName}\",{row.Severity},{row.DiagnosisCount}");
                }
                break;
            default:
                csv.AppendLine("Date,Method,Status,Total,Paid,Remaining,Count");
                foreach (var row in reports.Payments)
                {
                    csv.AppendLine($"{row.PaymentDate:yyyy-MM-dd},{row.PaymentMethod},{row.Status},{row.TotalAmount},{row.PaidAmount},{row.BalanceAmount},{row.PaymentCount}");
                }
                break;
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"hms-{type}-{DateTime.Today:yyyyMMdd}.csv");
    }
}
