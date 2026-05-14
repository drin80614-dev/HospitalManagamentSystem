using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HospitalManagamentSystem.Models;
using HospitalManagamentSystem.ViewModels;
using Microsoft.Extensions.Options;

namespace HospitalManagamentSystem.Data;

public class HospitalRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;

    public HospitalRepository(HttpClient httpClient, IOptions<CloudflareD1Options> options)
    {
        _httpClient = httpClient;
        var configuredBaseUrl = Environment.GetEnvironmentVariable("D1_API_BASE_URL")
            ?? options.Value.ApiBaseUrl;
        var apiToken = Environment.GetEnvironmentVariable("D1_API_TOKEN")
            ?? options.Value.ApiToken;

        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            _httpClient.BaseAddress = new Uri(configuredBaseUrl.TrimEnd('/') + "/");
        }

        if (!string.IsNullOrWhiteSpace(apiToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        }
    }

    private bool HasApi => _httpClient.BaseAddress is not null;

    public Task EnsureFeatureSchemaAsync() => Task.CompletedTask;

    public async Task<AppUser?> AuthenticateAsync(string login, string password)
    {
        var response = await SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", new { login, password }, throwOnFailure: false);
        return response?.User;
    }

    public Task<AppUser?> GetUserByLoginAsync(string login) => Task.FromResult<AppUser?>(null);

    public Task<long> GetLiveSyncVersionAsync()
        => Task.FromResult(1L);

    public Task<AppUser?> GetUserByIdAsync(Guid id) => Task.FromResult<AppUser?>(null);

    public Task<string?> CreatePasswordResetTokenAsync(string email) => Task.FromResult<string?>(null);

    public Task<bool> ResetPasswordAsync(string token, string passwordHash) => Task.FromResult(false);

    public Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPasswordHash, Func<string, string, bool> verifyPassword)
        => Task.FromResult(false);

    public Task TouchLastLoginAsync(Guid userId) => Task.CompletedTask;

    public async Task<IReadOnlyList<Role>> GetRolesAsync()
        => await GetAsync<IReadOnlyList<Role>>("api/roles") ?? [];

    public Task<IReadOnlyList<AppUser>> GetUsersAsync()
        => Task.FromResult<IReadOnlyList<AppUser>>([]);

    public Task<Guid> CreateUserAsync(UserCreateViewModel model, string passwordHash, Guid actorUserId)
        => throwUnavailableGuidAsync("User creation is handled by Cloudflare D1 seed/API configuration.");

    public async Task<DashboardViewModel> GetDashboardAsync(string role, string userName, Guid? doctorId = null)
    {
        var patients = await GetPatientOptionsAsync(role == AppRoles.Doctor ? doctorId : null);
        var doctors = await GetDoctorsAsync(true);
        var rooms = await GetRoomsAsync();
        var appointments = (await GetAppointmentsAsync(role == AppRoles.Doctor ? doctorId : null, DateTime.Today, null)).Appointments;
        var payments = await GetPaymentsAsync();
        var pendingPayments = payments.Where(payment => payment.BalanceAmount > 0 || payment.Status == "Pending").Take(8).ToList();

        return new DashboardViewModel
        {
            Role = role,
            UserName = userName,
            Metrics =
            [
                new DashboardMetric { Label = "Pacientet", Value = patients.Count.ToString(CultureInfo.InvariantCulture), Icon = "bi-people", Accent = "primary", Caption = "Total ne D1" },
                new DashboardMetric { Label = "Terminet sot", Value = appointments.Count.ToString(CultureInfo.InvariantCulture), Icon = "bi-calendar2-check", Accent = "info", Caption = "Orari i dites" },
                new DashboardMetric { Label = "Stomatologe", Value = doctors.Count.ToString(CultureInfo.InvariantCulture), Icon = "bi-person-badge", Accent = "success", Caption = "Aktive" },
                new DashboardMetric { Label = "Pagesa ne pritje", Value = pendingPayments.Count.ToString(CultureInfo.InvariantCulture), Icon = "bi-receipt", Accent = "warning", Caption = "Balanca te hapura" }
            ],
            RecentPatients = patients.Take(8).ToList(),
            TodayAppointments = appointments,
            AvailableDoctors = doctors,
            AvailableRooms = rooms.Where(room => room.Status == "Available").ToList(),
            PendingPayments = pendingPayments
        };
    }

    public async Task<(IReadOnlyList<Patient> Patients, int TotalCount)> GetPatientsAsync(
        string? search,
        string? status,
        Guid? doctorId,
        Guid? roomId,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);

        var patients = await GetPatientOptionsAsync();
        if (!string.IsNullOrWhiteSpace(search))
        {
            patients = await SearchPatientsAsync(search);
        }

        var filtered = patients
            .Where(patient => string.IsNullOrWhiteSpace(status) || patient.Status == status)
            .Where(patient => !doctorId.HasValue || patient.AssignedDoctorId == doctorId)
            .Where(patient => !roomId.HasValue || patient.CurrentRoomId == roomId)
            .ToList();

        var total = filtered.Count;
        return (filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList(), total);
    }

    public async Task<IReadOnlyList<Patient>> GetPatientOptionsAsync(Guid? doctorId = null)
    {
        var patients = await SearchPatientsAsync(string.Empty);
        return doctorId.HasValue
            ? patients.Where(patient => patient.AssignedDoctorId == doctorId).ToList()
            : patients;
    }

    public async Task<Patient?> GetPatientAsync(Guid id)
        => await GetAsync<Patient>($"api/patients/{id}");

    public async Task<PatientDetailsViewModel?> GetPatientDetailsAsync(Guid id)
    {
        var history = await GetAsync<D1PatientHistory>($"api/patients/{id}/history");
        if (history?.Patient is null)
        {
            var patient = await GetPatientAsync(id);
            if (patient is null)
            {
                return null;
            }

            return new PatientDetailsViewModel { Patient = patient };
        }

        return new PatientDetailsViewModel
        {
            Patient = history.Patient,
            Visits = history.Visits ?? [],
            Diagnoses = history.Diagnoses ?? [],
            Prescriptions = history.Prescriptions ?? [],
            Payments = history.Payments ?? []
        };
    }

    public async Task<Guid> CreatePatientAsync(Patient patient, Guid actorUserId)
    {
        var created = await SendAsync<Patient>(HttpMethod.Post, "api/patients", new
        {
            patient.FirstName,
            patient.LastName,
            patient.Phone,
            patient.AssignedDoctorId,
            patient.DateOfBirth,
            patient.Gender,
            patient.PersonalNumber,
            patient.Email,
            patient.Address,
            patient.BloodType,
            patient.Allergies,
            patient.ChronicDiseases,
            patient.RegistrationDate,
            patient.Status
        });

        return created?.Id ?? throw new InvalidOperationException("Patient could not be created in Cloudflare D1.");
    }

    public Task UpdatePatientAsync(Patient patient, Guid actorUserId)
        => Task.CompletedTask;

    public Task DeletePatientAsync(Guid id, Guid actorUserId)
        => Task.CompletedTask;

    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(bool activeOnly = false)
    {
        var departments = await GetAsync<IReadOnlyList<Department>>("api/departments") ?? [];
        return activeOnly
            ? departments.Where(department => department.Status == "Active").ToList()
            : departments;
    }

    public async Task<Department?> GetDepartmentAsync(Guid id)
        => (await GetDepartmentsAsync()).FirstOrDefault(department => department.Id == id);

    public Task<Guid> CreateDepartmentAsync(Department department)
        => throwUnavailableGuidAsync("Department creation is handled in Cloudflare D1.");

    public Task UpdateDepartmentAsync(Department department)
        => Task.CompletedTask;

    public async Task<IReadOnlyList<Doctor>> GetDoctorsAsync(bool activeOnly = false)
        => await GetAsync<IReadOnlyList<Doctor>>($"api/doctors?activeOnly={activeOnly.ToString().ToLowerInvariant()}") ?? [];

    public async Task<Doctor?> GetDoctorAsync(Guid id)
        => (await GetDoctorsAsync()).FirstOrDefault(doctor => doctor.Id == id);

    public Task<Guid> CreateDoctorAsync(Doctor doctor, Guid actorUserId)
        => throwUnavailableGuidAsync("Doctor creation is handled in Cloudflare D1.");

    public Task UpdateDoctorAsync(Doctor doctor, Guid actorUserId)
        => Task.CompletedTask;

    public Task DeleteDoctorAsync(Guid id, Guid actorUserId)
        => Task.CompletedTask;

    public Task<IReadOnlyList<Receptionist>> GetReceptionistsAsync()
        => Task.FromResult<IReadOnlyList<Receptionist>>([]);

    public Task<Receptionist?> GetReceptionistAsync(Guid id)
        => Task.FromResult<Receptionist?>(null);

    public Task<Guid> CreateReceptionistAsync(Receptionist receptionist, Guid actorUserId)
        => throwUnavailableGuidAsync("Receptionist creation is handled in Cloudflare D1.");

    public Task UpdateReceptionistAsync(Receptionist receptionist, Guid actorUserId)
        => Task.CompletedTask;

    public async Task<IReadOnlyList<Room>> GetRoomsAsync(bool onlyAvailable = false)
    {
        var rooms = await GetAsync<IReadOnlyList<Room>>("api/rooms") ?? [];
        return onlyAvailable
            ? rooms.Where(room => room.Status == "Available").ToList()
            : rooms;
    }

    public async Task<Room?> GetRoomAsync(Guid id)
        => (await GetRoomsAsync()).FirstOrDefault(room => room.Id == id);

    public Task<Guid> CreateRoomAsync(Room room)
        => throwUnavailableGuidAsync("Room creation is handled in Cloudflare D1.");

    public Task UpdateRoomAsync(Room room)
        => Task.CompletedTask;

    public async Task<Guid> AssignRoomAsync(PatientRoomAssignment assignment, Guid actorUserId)
    {
        var result = await SendAsync<IdResponse>(HttpMethod.Post, "api/hospitalizations", new
        {
            assignment.PatientId,
            assignment.RoomId,
            assignment.AdmissionDate,
            assignment.ExpectedDischargeDate,
            assignment.Notes
        });

        return result?.Id ?? throw new InvalidOperationException("Hospitalization could not be created in Cloudflare D1.");
    }

    public Task TransferPatientRoomAsync(RoomTransferViewModel model, Guid actorUserId)
        => Task.CompletedTask;

    public Task DischargePatientAsync(PatientDischargeViewModel model, Guid actorUserId)
        => Task.CompletedTask;

    public async Task<AppointmentListViewModel> GetAppointmentsAsync(Guid? doctorId, DateTime? date, string? status)
    {
        var query = QueryString(new Dictionary<string, string?>
        {
            ["doctorId"] = doctorId?.ToString(),
            ["date"] = date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["status"] = status
        });
        var appointments = await GetAsync<IReadOnlyList<Appointment>>($"api/appointments{query}") ?? [];

        return new AppointmentListViewModel
        {
            Appointments = appointments,
            Doctors = await GetDoctorsAsync(true),
            DoctorId = doctorId,
            Date = date,
            Status = status
        };
    }

    public async Task<Appointment?> GetAppointmentAsync(Guid id)
        => await GetAsync<Appointment>($"api/appointments/{id}");

    public async Task<IReadOnlyList<Guid>> GetAppointmentServiceIdsAsync(Guid appointmentId)
    {
        var appointment = await GetAppointmentAsync(appointmentId);
        return appointment?.ServiceId is { } serviceId && serviceId != Guid.Empty ? [serviceId] : [];
    }

    public async Task<Guid> CreateAppointmentAsync(Appointment appointment, IReadOnlyCollection<Guid> serviceIds, Guid actorUserId)
    {
        var created = await SendAsync<Appointment>(HttpMethod.Post, "api/appointments", AppointmentPayload(appointment, serviceIds));
        return created?.Id ?? throw new InvalidOperationException("Appointment could not be created in Cloudflare D1.");
    }

    public async Task UpdateAppointmentAsync(Appointment appointment, IReadOnlyCollection<Guid> serviceIds, Guid actorUserId)
        => await SendAsync<Appointment>(HttpMethod.Patch, $"api/appointments/{appointment.Id}", AppointmentPayload(appointment, serviceIds));

    public async Task<IReadOnlyList<Appointment>> GetAppointmentCalendarAsync(Guid? doctorId, DateTime start, DateTime end)
    {
        var appointments = (await GetAppointmentsAsync(doctorId, null, null)).Appointments;
        return appointments
            .Where(appointment => appointment.AppointmentDate.Date >= start.Date && appointment.AppointmentDate.Date <= end.Date)
            .ToList();
    }

    public async Task UpdateAppointmentStatusAsync(Guid id, string status, Guid actorUserId)
        => await SendAsync<Appointment>(HttpMethod.Patch, $"api/appointments/{id}", new { status });

    public async Task<Guid> CreateVisitAsync(Visit visit, Guid actorUserId)
    {
        var result = await SendAsync<IdResponse>(HttpMethod.Post, "api/visits", new
        {
            visit.PatientId,
            visit.DoctorId,
            visit.AppointmentId,
            visit.VisitDate,
            visit.Symptoms,
            visit.Diagnosis,
            visit.Disease,
            visit.TreatmentPlan,
            visit.Notes,
            visit.FollowUpDate,
            status = visit.VisitStatus
        });

        return result?.Id ?? throw new InvalidOperationException("Visit could not be saved in Cloudflare D1.");
    }

    public Task<IReadOnlyList<Disease>> GetDiseasesAsync()
        => Task.FromResult<IReadOnlyList<Disease>>([]);

    public Task<Guid> CreateDiseaseAsync(Disease disease)
        => throwUnavailableGuidAsync("Diseases are not maintained as a separate D1 endpoint.");

    public async Task<Guid> CreateDiagnosisAsync(Diagnosis diagnosis, Guid actorUserId)
    {
        var result = await SendAsync<IdResponse>(HttpMethod.Post, "api/visits", new
        {
            diagnosis.PatientId,
            diagnosis.DoctorId,
            diagnosis.AppointmentId,
            VisitDate = diagnosis.DiagnosisDate,
            Symptoms = diagnosis.Description ?? diagnosis.DiseaseName,
            Diagnosis = diagnosis.DiseaseName,
            Disease = diagnosis.DiseaseName,
            TreatmentPlan = diagnosis.TreatmentRecommendation ?? string.Empty,
            Notes = diagnosis.Description,
            Status = "Completed"
        });

        return result?.Id ?? throw new InvalidOperationException("Diagnosis could not be saved in Cloudflare D1.");
    }

    public async Task<Guid> CreatePrescriptionAsync(Prescription prescription, Guid actorUserId)
    {
        var result = await SendAsync<IdResponse>(HttpMethod.Post, "api/prescriptions", new
        {
            prescription.PatientId,
            prescription.DoctorId,
            prescription.MedicationName,
            prescription.Dosage,
            prescription.Frequency,
            prescription.Duration,
            prescription.Instructions,
            prescription.PrescriptionDate
        });

        return result?.Id ?? throw new InvalidOperationException("Prescription could not be saved in Cloudflare D1.");
    }

    public async Task<Prescription?> GetPrescriptionAsync(Guid id)
        => await GetAsync<Prescription>($"api/prescriptions/{id}");

    public Task<Guid> CreateLabTestAsync(LabTest labTest, Guid actorUserId)
        => throwUnavailableGuidAsync("Lab tests are not enabled in the Cloudflare D1 API.");

    public Task<IReadOnlyList<LabTest>> GetLabTestsAsync(Guid? doctorId = null, string? status = null)
        => Task.FromResult<IReadOnlyList<LabTest>>([]);

    public Task<LabTest?> GetLabTestAsync(Guid id)
        => Task.FromResult<LabTest?>(null);

    public Task UpdateLabResultAsync(Guid id, string status, string? result, DateTime? resultDate, Guid actorUserId)
        => Task.CompletedTask;

    public async Task<IReadOnlyList<ServiceItem>> GetServicesAsync()
        => await GetAsync<IReadOnlyList<ServiceItem>>("api/services") ?? [];

    public async Task<ServiceItem?> GetServiceAsync(Guid id)
        => (await GetServicesAsync()).FirstOrDefault(service => service.Id == id);

    public Task<Guid> CreateServiceAsync(ServiceItem service)
        => throwUnavailableGuidAsync("Service creation is handled in Cloudflare D1 seed/API configuration.");

    public Task UpdateServiceAsync(ServiceItem service)
        => Task.CompletedTask;

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(Guid? patientId = null)
    {
        var query = QueryString(new Dictionary<string, string?> { ["patientId"] = patientId?.ToString() });
        return await GetAsync<IReadOnlyList<Payment>>($"api/payments{query}") ?? [];
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid id)
        => await GetAsync<Payment>($"api/payments/{id}");

    public async Task<Guid> CreatePaymentAsync(Payment payment, Guid actorUserId, IReadOnlyCollection<Guid>? serviceIds = null)
    {
        var result = await SendAsync<Payment>(HttpMethod.Post, "api/payments", new
        {
            payment.PatientId,
            payment.AppointmentId,
            serviceIds = serviceIds?.Where(id => id != Guid.Empty).ToArray() ?? [payment.ServiceId],
            totalAmount = payment.TotalAmount,
            paidAmount = payment.Amount,
            payment.PaymentMethod,
            payment.PaymentDate,
            payment.Status,
            payment.Notes
        });

        return result?.Id ?? throw new InvalidOperationException("Payment could not be saved in Cloudflare D1.");
    }

    public async Task UpdatePaymentBalanceAsync(Guid id, decimal balanceAmount, string status, string? notes, Guid actorUserId)
        => await SendAsync<Payment>(HttpMethod.Patch, $"api/payments/{id}/balance", new { balanceAmount, status, notes });

    public async Task<Invoice?> GetInvoiceByPaymentAsync(Guid paymentId)
        => await GetAsync<Invoice>($"api/invoices/by-payment/{paymentId}");

    public Task<InventoryViewModel> GetInventoryAsync(string? search, bool lowStockOnly)
        => Task.FromResult(new InventoryViewModel { Search = search, LowStockOnly = lowStockOnly });

    public Task<MedicationInventoryItem?> GetInventoryItemAsync(Guid id)
        => Task.FromResult<MedicationInventoryItem?>(null);

    public Task<Guid> CreateInventoryItemAsync(MedicationInventoryItem item, Guid actorUserId)
        => throwUnavailableGuidAsync("Inventory is outside the current D1 schema.");

    public Task UpdateInventoryItemAsync(MedicationInventoryItem item, Guid actorUserId)
        => Task.CompletedTask;

    public Task<NotificationsViewModel> GetNotificationsAsync(Guid userId)
        => Task.FromResult(new NotificationsViewModel());

    public Task MarkNotificationReadAsync(Guid id, Guid userId)
        => Task.CompletedTask;

    public Task AddNotificationAsync(Guid? userId, string title, string message, string type, string? linkUrl = null)
        => Task.CompletedTask;

    public async Task<GlobalSearchViewModel> SearchAsync(string query)
    {
        return new GlobalSearchViewModel
        {
            Query = query,
            Patients = await SearchPatientsAsync(query),
            Doctors = (await GetDoctorsAsync(true))
                .Where(doctor => doctor.FullName.Contains(query ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .ToList(),
            Rooms = (await GetRoomsAsync())
                .Where(room => room.RoomNumber.Contains(query ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .ToList(),
            Appointments = (await GetAppointmentsAsync(null, null, null)).Appointments
                .Where(appointment => (appointment.PatientName ?? string.Empty).Contains(query ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                    || appointment.AppointmentNumber.Contains(query ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .ToList()
        };
    }

    public async Task<ReportsViewModel> GetReportsAsync(DateTime from, DateTime to)
    {
        var payments = await GetPaymentsAsync();
        var appointments = (await GetAppointmentsAsync(null, null, null)).Appointments;
        var patients = await GetPatientOptionsAsync();

        return new ReportsViewModel
        {
            From = from,
            To = to,
            DailyPatients = patients
                .Where(patient => patient.RegistrationDate.Date >= from.Date && patient.RegistrationDate.Date <= to.Date)
                .GroupBy(patient => patient.RegistrationDate.Date)
                .Select(group => new DailyPatientReportRow { ReportDate = group.Key, RegisteredPatients = group.Count() })
                .ToList(),
            Payments = payments
                .Where(payment => payment.PaymentDate.Date >= from.Date && payment.PaymentDate.Date <= to.Date)
                .GroupBy(payment => new { Date = payment.PaymentDate.Date, payment.PaymentMethod, payment.Status })
                .Select(group => new PaymentReportRow
                {
                    PaymentDate = group.Key.Date,
                    PaymentMethod = group.Key.PaymentMethod,
                    Status = group.Key.Status,
                    TotalAmount = group.Sum(payment => payment.TotalAmount),
                    PaidAmount = group.Sum(payment => payment.Amount),
                    BalanceAmount = group.Sum(payment => payment.BalanceAmount),
                    PaymentCount = group.Count()
                })
                .ToList(),
            Appointments = appointments
                .Where(appointment => appointment.AppointmentDate.Date >= from.Date && appointment.AppointmentDate.Date <= to.Date)
                .GroupBy(appointment => new { Date = appointment.AppointmentDate.Date, appointment.Status })
                .Select(group => new AppointmentReportRow { AppointmentDate = group.Key.Date, Status = group.Key.Status, AppointmentCount = group.Count() })
                .ToList()
        };
    }

    private async Task<IReadOnlyList<Patient>> SearchPatientsAsync(string query)
        => await GetAsync<IReadOnlyList<Patient>>($"api/patients/search?q={Uri.EscapeDataString(query ?? string.Empty)}") ?? [];

    private static object AppointmentPayload(Appointment appointment, IReadOnlyCollection<Guid> serviceIds)
        => new
        {
            appointment.PatientId,
            appointment.DoctorId,
            serviceIds = serviceIds.Where(id => id != Guid.Empty).ToArray(),
            appointmentDate = appointment.AppointmentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            appointmentTime = appointment.AppointmentTime.ToString(),
            appointment.Reason,
            appointment.Status,
            appointment.Notes
        };

    private async Task<T?> GetAsync<T>(string path)
    {
        if (!HasApi)
        {
            return default;
        }

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var response = await _httpClient.GetAsync(path);
                if (!response.IsSuccessStatusCode)
                {
                    if (attempt < 3 && IsTransientStatusCode(response.StatusCode))
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(180 * attempt));
                        continue;
                    }

                    return default;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
            }
            catch when (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(180 * attempt));
            }
        }

        return default;
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object payload, bool throwOnFailure = true)
    {
        if (!HasApi)
        {
            if (throwOnFailure)
            {
                throw new InvalidOperationException("Cloudflare D1 API is not configured. Set D1_API_BASE_URL.");
            }

            return default;
        }

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(method, path)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
                };
                using var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    if (attempt < 3 && IsTransientStatusCode(response.StatusCode))
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(220 * attempt));
                        continue;
                    }

                    if (!throwOnFailure)
                    {
                        return default;
                    }

                    throw new InvalidOperationException(ExtractError(body) ?? $"Cloudflare D1 API returned {(int)response.StatusCode}.");
                }

                return JsonSerializer.Deserialize<T>(body, JsonOptions);
            }
            catch when (!throwOnFailure)
            {
                return default;
            }
            catch when (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(220 * attempt));
            }
        }

        return default;
    }

    private static bool IsTransientStatusCode(System.Net.HttpStatusCode statusCode)
        => (int)statusCode is 408 or 425 or 429 or 500 or 502 or 503 or 504;

    private static string? ExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static string QueryString(IDictionary<string, string?> values)
    {
        var parts = values
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}")
            .ToList();

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    private static Task<Guid> throwUnavailableGuidAsync(string message)
        => Task.FromException<Guid>(new InvalidOperationException(message));

    private sealed class LoginResponse
    {
        public AppUser? User { get; set; }
    }

    private sealed class IdResponse
    {
        public Guid Id { get; set; }
    }

    private sealed class D1PatientHistory
    {
        public Patient? Patient { get; set; }
        public IReadOnlyList<Visit>? Visits { get; set; }
        public IReadOnlyList<Diagnosis>? Diagnoses { get; set; }
        public IReadOnlyList<Prescription>? Prescriptions { get; set; }
        public IReadOnlyList<Payment>? Payments { get; set; }
    }
}
