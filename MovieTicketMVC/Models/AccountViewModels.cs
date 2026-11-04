using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MovieTicketMVC.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256, ErrorMessage = "Your email address must not exceed 256 characters.")]
        [Display(Name = "Email address")]
        public string Email { get; set; }
    }

    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }

    public class SendCodeViewModel
    {
        public SendCodeViewModel()
        {
            Providers = new List<SelectListItem>();
        }

        [Required(ErrorMessage = "Please select a verification method.")]
        [Display(Name = "Verification method")]
        public string SelectedProvider { get; set; }

        public ICollection<SelectListItem> Providers { get; set; }

        public string ReturnUrl { get; set; }

        public bool RememberMe { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required(ErrorMessage = "A verification method is required.")]
        public string Provider { get; set; }

        [Required(ErrorMessage = "Please enter your verification code.")]
        [Display(Name = "Verification code")]
        public string Code { get; set; }

        public string ReturnUrl { get; set; }

        [Display(Name = "Remember this browser?")]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ForgotViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256, ErrorMessage = "Your email address must not exceed 256 characters.")]
        [Display(Name = "Email address")]
        public string Email { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256, ErrorMessage = "Your email address must not exceed 256 characters.")]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256, ErrorMessage = "Your email address must not exceed 256 characters.")]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter your password.")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "Your password must be between 8 and 100 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password.")]
        [StringLength(
            100,
            ErrorMessage = "Password confirmation must not exceed 100 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [System.ComponentModel.DataAnnotations.Compare(
            "Password",
            ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256, ErrorMessage = "Your email address must not exceed 256 characters.")]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter your new password.")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "Your password must be between 8 and 100 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please confirm your new password.")]
        [StringLength(
            100,
            ErrorMessage = "Password confirmation must not exceed 100 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [System.ComponentModel.DataAnnotations.Compare(
            "Password",
            ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "A password reset token is required.")]
        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256, ErrorMessage = "Your email address must not exceed 256 characters.")]
        [Display(Name = "Email address")]
        public string Email { get; set; }
    }
}