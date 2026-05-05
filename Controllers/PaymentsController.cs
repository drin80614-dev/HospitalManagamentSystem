using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using HospitalManagamentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Receptionist}")]
public class PaymentsController : AppController
{
    private readonly HospitalRepository _repository;

    public PaymentsController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index(Guid? patientId) => View(await _repository.GetPaymentsAsync(patientId));

    public async Task<IActionResult> Create(Guid? patientId)
    {
        return View(new PaymentCreateViewModel
        {
            Payment = new Payment { PatientId = patientId ?? Guid.Empty, PaymentDate = DateTime.Now, Status = "Paid", PaymentMethod = "Cash" },
            Patients = await _repository.GetPatientOptionsAsync(),
            Services = await _repository.GetServicesAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Patients = await _repository.GetPatientOptionsAsync();
            model.Services = await _repository.GetServicesAsync();
            return View(model);
        }

        var paymentId = await _repository.CreatePaymentAsync(model.Payment, CurrentUserId);
        return FlashAndRedirect("Payment registered and invoice generated.", "Receipt", "Payments", new { paymentId });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var payment = await _repository.GetPaymentByIdAsync(id);
        if (payment is null)
        {
            return NotFound();
        }

        return View(new PaymentEditViewModel
        {
            Id = payment.Id,
            PatientName = payment.PatientName ?? string.Empty,
            HospitalNumber = payment.HospitalNumber ?? string.Empty,
            ServiceName = payment.ServiceName ?? string.Empty,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            PaymentDate = payment.PaymentDate,
            Status = payment.Status,
            Notes = payment.Notes
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PaymentEditViewModel model)
    {
        if (model.Status is not ("Paid" or "Pending" or "Cancelled"))
        {
            ModelState.AddModelError(nameof(model.Status), "Choose a valid payment status.");
        }

        if (!ModelState.IsValid)
        {
            var payment = await _repository.GetPaymentByIdAsync(model.Id);
            if (payment is null)
            {
                return NotFound();
            }

            model.PatientName = payment.PatientName ?? string.Empty;
            model.HospitalNumber = payment.HospitalNumber ?? string.Empty;
            model.ServiceName = payment.ServiceName ?? string.Empty;
            model.Amount = payment.Amount;
            model.PaymentMethod = payment.PaymentMethod;
            model.PaymentDate = payment.PaymentDate;
            return View(model);
        }

        await _repository.UpdatePaymentStatusAsync(model.Id, model.Status, model.Notes, CurrentUserId);
        return FlashAndRedirect("Payment status updated.", "Index", "Payments");
    }

    public async Task<IActionResult> Receipt(Guid paymentId)
    {
        var invoice = await _repository.GetInvoiceByPaymentAsync(paymentId);
        return invoice is null ? NotFound() : View(invoice);
    }
}
