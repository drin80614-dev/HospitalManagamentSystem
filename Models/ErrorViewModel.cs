namespace HospitalManagamentSystem.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public int StatusCode { get; set; } = 500;

    public string Title { get; set; } = "Sistemi nuk mundi ta hap kete faqe";

    public string Message { get; set; } = "Provoni perseri pas pak ose kthehuni te paneli kryesor. Te dhenat jane te ruajtura ne databaze.";

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
