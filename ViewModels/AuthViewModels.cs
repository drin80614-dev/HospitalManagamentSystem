using System.ComponentModel.DataAnnotations;
using HospitalManagamentSystem.Models;

namespace HospitalManagamentSystem.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your username or email.")]
    [Display(Name = "Username or email")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Enter your account email.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(NewPassword))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string? CurrentPassword { get; set; }

    [MinLength(8), DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword))]
    [Display(Name = "Confirm new password")]
    public string? ConfirmPassword { get; set; }
}

public class UserCreateViewModel
{
    [Required, StringLength(80)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public Guid RoleId { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = "Active";
}

public class UsersAdminViewModel
{
    public IReadOnlyList<AppUser> Users { get; set; } = [];
    public IReadOnlyList<Role> Roles { get; set; } = [];
    public UserCreateViewModel NewUser { get; set; } = new();
}
