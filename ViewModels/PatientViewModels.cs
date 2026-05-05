using HospitalManagamentSystem.Models;

namespace HospitalManagamentSystem.ViewModels;

public class PatientListViewModel
{
    public IReadOnlyList<Patient> Patients { get; set; } = [];
    public IReadOnlyList<Doctor> Doctors { get; set; } = [];
    public IReadOnlyList<Room> Rooms { get; set; } = [];
    public string? Search { get; set; }
    public string? Status { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? RoomId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize)));
}

public class PatientDetailsViewModel
{
    public Patient Patient { get; set; } = new();
    public IReadOnlyList<Visit> Visits { get; set; } = [];
    public IReadOnlyList<Diagnosis> Diagnoses { get; set; } = [];
    public IReadOnlyList<Prescription> Prescriptions { get; set; } = [];
    public IReadOnlyList<Payment> Payments { get; set; } = [];
    public IReadOnlyList<PatientRoomAssignment> RoomAssignments { get; set; } = [];
    public IReadOnlyList<LabTest> LabTests { get; set; } = [];
}
