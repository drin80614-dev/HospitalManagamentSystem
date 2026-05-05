using System.ComponentModel.DataAnnotations;
using HospitalManagamentSystem.Models;

namespace HospitalManagamentSystem.ViewModels;

public class AppointmentListViewModel
{
    public IReadOnlyList<Appointment> Appointments { get; set; } = [];
    public IReadOnlyList<Doctor> Doctors { get; set; } = [];
    public Guid? DoctorId { get; set; }
    public DateTime? Date { get; set; }
    public string? Status { get; set; }
}

public class AppointmentCreateViewModel
{
    public Appointment Appointment { get; set; } = new();

    [MinLength(1, ErrorMessage = "Select at least one service.")]
    public List<Guid> ServiceIds { get; set; } = [];

    public IReadOnlyList<Patient> Patients { get; set; } = [];
    public IReadOnlyList<Doctor> Doctors { get; set; } = [];
    public IReadOnlyList<ServiceItem> Services { get; set; } = [];
}

public class RoomAssignmentViewModel
{
    public PatientRoomAssignment Assignment { get; set; } = new();
    public IReadOnlyList<Patient> Patients { get; set; } = [];
    public IReadOnlyList<Room> Rooms { get; set; } = [];
}

public class RoomTransferViewModel
{
    public Guid PatientId { get; set; }
    public Guid NewRoomId { get; set; }
    public DateTime TransferDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
    public IReadOnlyList<Patient> Patients { get; set; } = [];
    public IReadOnlyList<Room> Rooms { get; set; } = [];
}

public class PatientDischargeViewModel
{
    public Guid PatientId { get; set; }
    public DateTime DischargeDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
    public IReadOnlyList<Patient> Patients { get; set; } = [];
}

public class PaymentCreateViewModel
{
    public Payment Payment { get; set; } = new();
    public IReadOnlyList<Patient> Patients { get; set; } = [];
    public IReadOnlyList<ServiceItem> Services { get; set; } = [];
}

public class PaymentEditViewModel
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string HospitalNumber { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = "Paid";
    public string? Notes { get; set; }
}

public class ClinicalFormViewModel<TModel>
{
    public TModel Record { get; set; } = default!;
    public IReadOnlyList<Patient> Patients { get; set; } = [];
    public IReadOnlyList<Doctor> Doctors { get; set; } = [];
    public IReadOnlyList<Disease> Diseases { get; set; } = [];
}

public class LabTestListViewModel
{
    public IReadOnlyList<LabTest> LabTests { get; set; } = [];
    public string? Status { get; set; }
}

public class LabResultViewModel
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string TestType { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    public string? Result { get; set; }
    public DateTime? ResultDate { get; set; } = DateTime.Today;
}
