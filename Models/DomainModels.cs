using System.ComponentModel.DataAnnotations;

namespace HospitalManagamentSystem.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Receptionist = "Receptionist";
}

public abstract class EntityBase
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class Role : EntityBase
{
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }
}

public class AppUser : EntityBase
{
    public Guid RoleId { get; set; }

    [Required, StringLength(80)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(30)]
    public string Status { get; set; } = "Active";

    public DateTime? LastLoginAt { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid? DoctorId { get; set; }
    public Guid? ReceptionistId { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class Department : EntityBase
{
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    [StringLength(80)]
    public string? Location { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = "Active";
}

public class Doctor : EntityBase
{
    public Guid? UserId { get; set; }
    public Guid? DepartmentId { get; set; }

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Specialization { get; set; } = string.Empty;

    [Phone, StringLength(40)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(160)]
    public string? Email { get; set; }

    [Required, StringLength(80)]
    public string LicenseNumber { get; set; } = string.Empty;

    [StringLength(220)]
    public string? WorkingSchedule { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = "Active";

    public string? DepartmentName { get; set; }
    public string FullName => $"Dr. {FirstName} {LastName}".Trim();
}

public class Receptionist : EntityBase
{
    public Guid? UserId { get; set; }

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Phone, StringLength(40)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(160)]
    public string? Email { get; set; }

    [StringLength(120)]
    public string? Shift { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = "Active";

    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class Patient : EntityBase
{
    [StringLength(30)]
    public string HospitalNumber { get; set; } = string.Empty;

    [Required]
    public Guid? AssignedDoctorId { get; set; }
    public Guid? CurrentRoomId { get; set; }

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-30);

    [StringLength(20)]
    public string Gender { get; set; } = string.Empty;

    [StringLength(40)]
    public string PersonalNumber { get; set; } = string.Empty;

    [Required, Phone, StringLength(40)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(160)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(120)]
    public string? EmergencyContactName { get; set; }

    [Phone, StringLength(40)]
    public string? EmergencyContactPhone { get; set; }

    [StringLength(10)]
    public string? BloodType { get; set; }

    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }

    [DataType(DataType.Date)]
    public DateTime RegistrationDate { get; set; } = DateTime.Today;

    [StringLength(30)]
    public string Status { get; set; } = "Active";

    public string? AssignedDoctorName { get; set; }
    public string? CurrentRoomNumber { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public int Age => DateTime.Today.Year - DateOfBirth.Year - (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);
}

public class Room : EntityBase
{
    public Guid? DepartmentId { get; set; }

    [Required, StringLength(30)]
    public string RoomNumber { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string Floor { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string RoomType { get; set; } = "General";

    [Range(1, 50)]
    public int Capacity { get; set; } = 1;

    [Range(0, 50)]
    public int CurrentOccupancy { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = "Available";

    [Range(0, 100000)]
    public decimal PricePerDay { get; set; }

    public string? DepartmentName { get; set; }
    public int FreeBeds => Math.Max(0, Capacity - CurrentOccupancy);
}

public class PatientRoomAssignment : EntityBase
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid RoomId { get; set; }

    [DataType(DataType.Date)]
    public DateTime AdmissionDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime? ExpectedDischargeDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ActualDischargeDate { get; set; }

    public string? Notes { get; set; }
    public string? PatientName { get; set; }
    public string? RoomNumber { get; set; }
    public string? RoomType { get; set; }
}

public class Appointment : EntityBase
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    [Display(Name = "Service")]
    public Guid? ServiceId { get; set; }

    [DataType(DataType.Date)]
    public DateTime AppointmentDate { get; set; } = DateTime.Today;

    [DataType(DataType.Time)]
    public TimeSpan AppointmentTime { get; set; } = TimeSpan.FromHours(9);

    [Required, StringLength(220)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(30)]
    public string Status { get; set; } = "Scheduled";

    public string? Notes { get; set; }
    public string AppointmentNumber { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
    public string? DoctorSpecialization { get; set; }
    public string? ServiceName { get; set; }
    public decimal? ServicePrice { get; set; }
    public string? ServiceNames { get; set; }
    public decimal? ServicesTotal { get; set; }
}

public class Visit : EntityBase
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    public Guid? AppointmentId { get; set; }

    public DateTime VisitDate { get; set; } = DateTime.Now;

    [Required]
    public string Symptoms { get; set; } = string.Empty;

    [Required]
    public string Diagnosis { get; set; } = string.Empty;

    [StringLength(160)]
    public string? Disease { get; set; }

    [Required]
    public string TreatmentPlan { get; set; } = string.Empty;

    public string? Notes { get; set; }

    [DataType(DataType.Date)]
    public DateTime? FollowUpDate { get; set; }

    [StringLength(30)]
    public string VisitStatus { get; set; } = "Open";

    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
}

public class Disease : EntityBase
{
    [Required, StringLength(160)]
    public string DiseaseName { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Category { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? CommonSymptoms { get; set; }
}

public class Diagnosis : EntityBase
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    public Guid? DiseaseId { get; set; }

    [Required, StringLength(160)]
    public string DiseaseName { get; set; } = string.Empty;

    [StringLength(40)]
    public string? IcdCode { get; set; }

    [StringLength(30)]
    public string Severity { get; set; } = "Low";

    public string? Description { get; set; }

    [DataType(DataType.Date)]
    public DateTime DiagnosisDate { get; set; } = DateTime.Today;

    public string? TreatmentRecommendation { get; set; }
    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
}

public class Prescription : EntityBase
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    [Required, StringLength(160)]
    public string MedicationName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Dosage { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Frequency { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Duration { get; set; } = string.Empty;

    public string? Instructions { get; set; }

    [DataType(DataType.Date)]
    public DateTime PrescriptionDate { get; set; } = DateTime.Today;

    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
    public string? DoctorLicenseNumber { get; set; }
}

public class LabTest : EntityBase
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    [Required, StringLength(160)]
    public string TestName { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string TestType { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime RequestedDate { get; set; } = DateTime.Today;

    [StringLength(30)]
    public string Status { get; set; } = "Requested";

    public string? Result { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ResultDate { get; set; }

    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
}

public class MedicationInventoryItem : EntityBase
{
    [Required, StringLength(160)]
    public string MedicationName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Category { get; set; } = "General";

    [Required, StringLength(80)]
    public string Unit { get; set; } = "pcs";

    [Range(0, 1000000)]
    public int QuantityInStock { get; set; }

    [Range(0, 1000000)]
    public int ReorderLevel { get; set; } = 10;

    [DataType(DataType.Date)]
    public DateTime? ExpiryDate { get; set; }

    [StringLength(120)]
    public string? Supplier { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = "Available";

    public bool IsLowStock => QuantityInStock <= ReorderLevel;
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.Today;
}

public class Notification : EntityBase
{
    public Guid? UserId { get; set; }

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [StringLength(40)]
    public string NotificationType { get; set; } = "Info";

    [StringLength(120)]
    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class ServiceItem : EntityBase
{
    public Guid? DepartmentId { get; set; }

    [Required, StringLength(160)]
    public string ServiceName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, 1000000)]
    public decimal Price { get; set; }

    public string? DepartmentName { get; set; }
}

public class Payment : EntityBase
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid ServiceId { get; set; }

    [Range(0, 1000000)]
    public decimal Amount { get; set; }

    [Required, StringLength(40)]
    public string PaymentMethod { get; set; } = "Cash";

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    [StringLength(30)]
    public string Status { get; set; } = "Paid";

    public string? Notes { get; set; }
    public string? PatientName { get; set; }
    public string? HospitalNumber { get; set; }
    public string? ServiceName { get; set; }
}

public class Invoice : EntityBase
{
    [Required]
    public Guid PaymentId { get; set; }

    [Required, StringLength(40)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = "Issued";

    public string? PatientName { get; set; }
    public string? HospitalNumber { get; set; }
    public string? ServiceName { get; set; }
    public string? PaymentMethod { get; set; }
}

public class AuditLog : EntityBase
{
    public Guid? UserId { get; set; }

    [Required, StringLength(120)]
    public string Action { get; set; } = string.Empty;

    [StringLength(80)]
    public string? EntityName { get; set; }

    public Guid? EntityId { get; set; }
    public string? Details { get; set; }
    public string? UserName { get; set; }
}
