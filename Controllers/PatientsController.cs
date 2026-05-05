using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using HospitalManagamentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Doctor},{AppRoles.Receptionist}")]
public class PatientsController : AppController
{
    private readonly HospitalRepository _repository;

    public PatientsController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index(string? search, string? status, Guid? doctorId, Guid? roomId, int page = 1)
    {
        if (CurrentRole == AppRoles.Doctor)
        {
            doctorId = CurrentDoctorId;
        }

        var (patients, total) = await _repository.GetPatientsAsync(search, status, doctorId, roomId, page, 10);
        return View(new PatientListViewModel
        {
            Patients = patients,
            TotalCount = total,
            Search = search,
            Status = status,
            DoctorId = doctorId,
            RoomId = roomId,
            Page = page,
            Doctors = await _repository.GetDoctorsAsync(true),
            Rooms = await _repository.GetRoomsAsync()
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _repository.GetPatientDetailsAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        if (CurrentRole == AppRoles.Doctor && model.Patient.AssignedDoctorId != CurrentDoctorId)
        {
            return Forbid();
        }

        return View(model);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Receptionist}")]
    public async Task<IActionResult> Create()
    {
        await LoadPatientLookups();
        return View(new Patient { RegistrationDate = DateTime.Today, Status = "Active" });
    }

    [HttpPost, Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Receptionist}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Patient patient)
    {
        if (!ModelState.IsValid)
        {
            await LoadPatientLookups();
            return View(patient);
        }

        var id = await _repository.CreatePatientAsync(patient, CurrentUserId);
        return FlashAndRedirect("Patient registered successfully.", "Details", "Patients", new { id });
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Receptionist}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var patient = await _repository.GetPatientAsync(id);
        if (patient is null)
        {
            return NotFound();
        }

        await LoadPatientLookups();
        return View(patient);
    }

    [HttpPost, Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Receptionist}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Patient patient)
    {
        if (!ModelState.IsValid)
        {
            await LoadPatientLookups();
            return View(patient);
        }

        await _repository.UpdatePatientAsync(patient, CurrentUserId);
        return FlashAndRedirect("Patient updated.", "Details", "Patients", new { id = patient.Id });
    }

    [HttpPost, Authorize(Roles = AppRoles.Admin), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _repository.DeletePatientAsync(id, CurrentUserId);
        return FlashAndRedirect("Patient deleted.", "Index", "Patients");
    }

    private async Task LoadPatientLookups()
    {
        ViewBag.Doctors = await _repository.GetDoctorsAsync(true);
    }
}
