using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MovieTicketMVC.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required(ErrorMessage = "Мора да внесете e-mail адреса.")]
        [EmailAddress(ErrorMessage = "Внесете валидна e-mail адреса.")]
        [StringLength(256, ErrorMessage = "E-mail адресата е предолга.")]
        [Display(Name = "Email")]
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

        [Required(ErrorMessage = "Изберете метод за верификација.")]
        public string SelectedProvider { get; set; }

        public ICollection<SelectListItem> Providers { get; set; }

        public string ReturnUrl { get; set; }

        public bool RememberMe { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required(ErrorMessage = "Недостасува методот за верификација.")]
        public string Provider { get; set; }

        [Required(ErrorMessage = "Мора да внесете безбедносен код.")]
        [Display(Name = "Code")]
        public string Code { get; set; }

        public string ReturnUrl { get; set; }

        [Display(Name = "Remember this browser?")]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ForgotViewModel
    {
        [Required(ErrorMessage = "Мора да внесете e-mail адреса.")]
        [EmailAddress(ErrorMessage = "Внесете валидна e-mail адреса.")]
        [StringLength(256, ErrorMessage = "E-mail адресата е предолга.")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Мора да внесете e-mail адреса.")]
        [EmailAddress(ErrorMessage = "Внесете валидна e-mail адреса.")]
        [StringLength(256, ErrorMessage = "E-mail адресата е предолга.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Мора да внесете лозинка.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Мора да внесете e-mail адреса.")]
        [EmailAddress(ErrorMessage = "Внесете валидна e-mail адреса.")]
        [StringLength(256, ErrorMessage = "E-mail адресата е предолга.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Мора да внесете лозинка.")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "Лозинката мора да содржи помеѓу 8 и 100 знаци.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Мора да ја потврдите лозинката.")]
        [StringLength(
            100,
            ErrorMessage = "Потврдата на лозинката не смее да надминува 100 знаци.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [System.ComponentModel.DataAnnotations.Compare(
            "Password",
            ErrorMessage = "Лозинките не се совпаѓаат.")]
        public string ConfirmPassword { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Мора да внесете e-mail адреса.")]
        [EmailAddress(ErrorMessage = "Внесете валидна e-mail адреса.")]
        [StringLength(256, ErrorMessage = "E-mail адресата е предолга.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Мора да внесете нова лозинка.")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "Лозинката мора да содржи помеѓу 8 и 100 знаци.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Мора да ја потврдите новата лозинка.")]
        [StringLength(
            100,
            ErrorMessage = "Потврдата на лозинката не смее да надминува 100 знаци.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [System.ComponentModel.DataAnnotations.Compare(
            "Password",
            ErrorMessage = "Лозинките не се совпаѓаат.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Недостасува кодот за ресетирање на лозинката.")]
        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Мора да внесете e-mail адреса.")]
        [EmailAddress(ErrorMessage = "Внесете валидна e-mail адреса.")]
        [StringLength(256, ErrorMessage = "E-mail адресата е предолга.")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}