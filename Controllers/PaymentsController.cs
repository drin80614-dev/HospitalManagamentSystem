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

        try
        {
            var paymentId = await _repository.CreatePaymentAsync(model.Payment, CurrentUserId);
            return FlashAndRedirect("Payment registered and invoice generated.", "Receipt", "Payments", new { paymentId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Patients = await _repository.GetPatientOptionsAsync();
            model.Services = await _repository.GetServicesAsync();
            return View(model);
        }
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
            TotalAmount = payment.TotalAmount,
            BalanceAmount = payment.BalanceAmount,
            PaymentMethod = payment.PaymentMethod,
            PaymentDate = payment.PaymentDate,
            Status = payment.Status,
            Notes = payment.Notes
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PaymentEditViewModel model)
    {
        var existingPayment = await _repository.GetPaymentByIdAsync(model.Id);
        if (existingPayment is null)
        {
            return NotFound();
        }

        model.PatientName = existingPayment.PatientName ?? string.Empty;
        model.HospitalNumber = existingPayment.HospitalNumber ?? string.Empty;
        model.ServiceName = existingPayment.ServiceName ?? string.Empty;
        model.Amount = existingPayment.Amount;
        model.TotalAmount = existingPayment.TotalAmount;
        model.PaymentMethod = existingPayment.PaymentMethod;
        model.PaymentDate = existingPayment.PaymentDate;

        if (model.Status is not ("Paid" or "Pending" or "Cancelled"))
        {
            ModelState.AddModelError(nameof(model.Status), "Choose a valid payment status.");
        }

        if (model.BalanceAmount > model.TotalAmount)
        {
            ModelState.AddModelError(nameof(model.BalanceAmount), "Remaining balance cannot be higher than the total.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _repository.UpdatePaymentBalanceAsync(model.Id, model.BalanceAmount, model.Status, model.Notes, CurrentUserId);
        return FlashAndRedirect("Payment balance updated.", "Edit", "Payments", new { id = model.Id });
    }

    public async Task<IActionResult> Receipt(Guid paymentId)
    {
        var invoice = await _repository.GetInvoiceByPaymentAsync(paymentId);
        return invoice is null ? NotFound() : View(invoice);
    }
}
