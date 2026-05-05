using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using HospitalManagamentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Doctor},{AppRoles.Receptionist}")]
public class AppointmentsController : AppController
{
    private readonly HospitalRepository _repository;

    public AppointmentsController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index(Guid? doctorId, DateTime? date, string? status)
    {
        if (CurrentRole == AppRoles.Doctor)
        {
            doctorId = CurrentDoctorId;
        }

        return View(await _repository.GetAppointmentsAsync(doctorId, date, status));
    }

    public async Task<IActionResult> Calendar(Guid? doctorId, DateTime? start)
    {
        if (CurrentRole == AppRoles.Doctor)
        {
            doctorId = CurrentDoctorId;
        }

        var weekStart = (start ?? DateTime.Today).Date;
        var appointments = await _repository.GetAppointmentCalendarAsync(doctorId, weekStart, weekStart.AddDays(6));
        ViewBag.Start = weekStart;
        ViewBag.DoctorId = doctorId;
        ViewBag.Doctors = await _repository.GetDoctorsAsync(true);
        return View(appointments);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Receptionist}")]
    public async Task<IActionResult> Create(Guid? patientId)
    {
        return View(new AppointmentCreateViewModel
        {
            Appointment = new Appointment { PatientId = patientId ?? Guid.Empty, AppointmentDate = DateTime.Today, Status = "Scheduled" },
            Patients = await _repository.GetPatientOptionsAsync(),
            Doctors = await _repository.GetDoctorsAsync(true),
            Services = await _repository.GetServicesAsync()
        });
    }

    [HttpPost, Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Receptionist}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppointmentCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Patients = await _repository.GetPatientOptionsAsync();
            model.Doctors = await _repository.GetDoctorsAsync(true);
            model.Services = await _repository.GetServicesAsync();
            return View(model);
        }

        try
        {
            await _repository.CreateAppointmentAsync(model.Appointment, CurrentUserId);
            return FlashAndRedirect("Appointment created.", "Index", "Appointments");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Patients = await _repository.GetPatientOptionsAsync();
            model.Doctors = await _repository.GetDoctorsAsync(true);
            model.Services = await _repository.GetServicesAsync();
            return View(model);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(Guid id, string status)
    {
        if (CurrentRole == AppRoles.Receptionist && status is "Completed")
        {
            return Forbid();
        }

        await _repository.UpdateAppointmentStatusAsync(id, status, CurrentUserId);
        return FlashAndRedirect("Appointment status updated.", "Index", "Appointments");
    }
}
