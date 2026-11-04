using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;

namespace MovieTicketMVC.Models
{
    public class IndexViewModel
    {
        public IndexViewModel()
        {
            Logins = new List<UserLoginInfo>();
        }

        public bool HasPassword { get; set; }

        public IList<UserLoginInfo> Logins { get; set; }

        public string PhoneNumber { get; set; }

        public bool TwoFactor { get; set; }

        public bool BrowserRemembered { get; set; }
    }

    public class ManageLoginsViewModel
    {
        public ManageLoginsViewModel()
        {
            CurrentLogins = new List<UserLoginInfo>();
            OtherLogins = new List<AuthenticationDescription>();
        }

        public IList<UserLoginInfo> CurrentLogins { get; set; }

        public IList<AuthenticationDescription> OtherLogins { get; set; }
    }

    public class FactorViewModel
    {
        public string Purpose { get; set; }
    }

    public class SetPasswordViewModel
    {
        [Required(ErrorMessage = "Please enter your new password.")]
        [StringLength(
            100,
            ErrorMessage = "Your password must be between {2} and {1} characters long.",
            MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [System.ComponentModel.DataAnnotations.Compare(
            "NewPassword",
            ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Please enter your current password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Please enter your new password.")]
        [StringLength(
            100,
            ErrorMessage = "Your password must be between {2} and {1} characters long.",
            MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [System.ComponentModel.DataAnnotations.Compare(
            "NewPassword",
            ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class AddPhoneNumberViewModel
    {
        [Required(ErrorMessage = "Please enter your phone number.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone number")]
        public string Number { get; set; }
    }

    public class VerifyPhoneNumberViewModel
    {
        [Required(ErrorMessage = "Please enter your verification code.")]
        [Display(Name = "Verification code")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Please enter your phone number.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }
    }

    public class ConfigureTwoFactorViewModel
    {
        public ConfigureTwoFactorViewModel()
        {
            Providers = new List<System.Web.Mvc.SelectListItem>();
        }

        [Required(ErrorMessage = "Please select a two-factor authentication provider.")]
        [Display(Name = "Authentication provider")]
        public string SelectedProvider { get; set; }

        public ICollection<System.Web.Mvc.SelectListItem> Providers
        {
            get;
            set;
        }
    }
}