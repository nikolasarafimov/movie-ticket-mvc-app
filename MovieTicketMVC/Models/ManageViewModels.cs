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
        [Required(ErrorMessage = "Мора да внесете нова лозинка.")]
        [StringLength(
            100,
            ErrorMessage = "Лозинката мора да содржи најмалку {2} знаци.",
            MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Нова лозинка")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Мора да ја потврдите новата лозинка.")]
        [DataType(DataType.Password)]
        [Display(Name = "Потврди нова лозинка")]
        [System.ComponentModel.DataAnnotations.Compare(
            "NewPassword",
            ErrorMessage = "Новата лозинка и потврдата не се совпаѓаат.")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Мора да ја внесете тековната лозинка.")]
        [DataType(DataType.Password)]
        [Display(Name = "Тековна лозинка")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Мора да внесете нова лозинка.")]
        [StringLength(
            100,
            ErrorMessage = "Лозинката мора да содржи најмалку {2} знаци.",
            MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Нова лозинка")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Мора да ја потврдите новата лозинка.")]
        [DataType(DataType.Password)]
        [Display(Name = "Потврди нова лозинка")]
        [System.ComponentModel.DataAnnotations.Compare(
            "NewPassword",
            ErrorMessage = "Новата лозинка и потврдата не се совпаѓаат.")]
        public string ConfirmPassword { get; set; }
    }

    public class AddPhoneNumberViewModel
    {
        [Required(ErrorMessage = "Мора да внесете телефонски број.")]
        [Phone(ErrorMessage = "Внесете валиден телефонски број.")]
        [Display(Name = "Телефонски број")]
        public string Number { get; set; }
    }

    public class VerifyPhoneNumberViewModel
    {
        [Required(ErrorMessage = "Мора да внесете код за потврда.")]
        [Display(Name = "Код за потврда")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Мора да внесете телефонски број.")]
        [Phone(ErrorMessage = "Внесете валиден телефонски број.")]
        [Display(Name = "Телефонски број")]
        public string PhoneNumber { get; set; }
    }

    public class ConfigureTwoFactorViewModel
    {
        public ConfigureTwoFactorViewModel()
        {
            Providers =
                new List<System.Web.Mvc.SelectListItem>();
        }

        [Required(ErrorMessage = "Мора да одберете провајдер.")]
        public string SelectedProvider { get; set; }

        public ICollection<System.Web.Mvc.SelectListItem> Providers
        {
            get;
            set;
        }
    }
}