using System.ComponentModel.DataAnnotations;
using Certiva.Localization;

namespace Certiva.Models;

public sealed class AdminLoginViewModel
{
    [Required(ErrorMessageResourceType = typeof(ValidationResources), ErrorMessageResourceName = "LoginEmailRequired")]
    [EmailAddress(ErrorMessageResourceType = typeof(ValidationResources), ErrorMessageResourceName = "ValidEmailRequired")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(ValidationResources), ErrorMessageResourceName = "LoginPasswordRequired")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
