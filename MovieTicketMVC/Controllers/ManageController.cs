using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using MovieTicketMVC.Models;

namespace MovieTicketMVC.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public ManageController()
        {
        }

        public ManageController(
            ApplicationUserManager userManager,
            ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager
                    ?? HttpContext
                        .GetOwinContext()
                        .Get<ApplicationSignInManager>();
            }

            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager
                    ?? HttpContext
                        .GetOwinContext()
                        .GetUserManager<ApplicationUserManager>();
            }

            private set
            {
                _userManager = value;
            }
        }

        // GET: /Manage/Index
        [HttpGet]
        public async Task<ActionResult> Index(
            ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                GetStatusMessage(message);

            var userId =
                User.Identity.GetUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return View("Error");
            }

            var user =
                await UserManager.FindByIdAsync(
                    userId);

            if (user == null)
            {
                return View("Error");
            }

            var model =
                new IndexViewModel
                {
                    HasPassword =
                        user.PasswordHash != null,

                    PhoneNumber =
                        await UserManager
                            .GetPhoneNumberAsync(
                                userId),

                    TwoFactor =
                        await UserManager
                            .GetTwoFactorEnabledAsync(
                                userId),

                    Logins =
                        await UserManager
                            .GetLoginsAsync(
                                userId),

                    BrowserRemembered =
                        await AuthenticationManager
                            .TwoFactorBrowserRememberedAsync(
                                userId)
                };

            return View(model);
        }

        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(
            string loginProvider,
            string providerKey)
        {
            if (string.IsNullOrWhiteSpace(loginProvider) ||
                string.IsNullOrWhiteSpace(providerKey))
            {
                return RedirectToAction(
                    "ManageLogins",
                    new
                    {
                        Message =
                            ManageMessageId.Error
                    });
            }

            var userId =
                User.Identity.GetUserId();

            var user =
                await UserManager.FindByIdAsync(
                    userId);

            if (user == null)
            {
                return View("Error");
            }

            var logins =
                await UserManager.GetLoginsAsync(
                    userId);

            var loginExists =
                logins.Any(
                    login =>
                        login.LoginProvider ==
                            loginProvider &&
                        login.ProviderKey ==
                            providerKey);

            if (!loginExists)
            {
                return RedirectToAction(
                    "ManageLogins",
                    new
                    {
                        Message =
                            ManageMessageId.Error
                    });
            }

            var hasPassword =
                user.PasswordHash != null;

            if (!hasPassword &&
                logins.Count <= 1)
            {
                return RedirectToAction(
                    "ManageLogins",
                    new
                    {
                        Message =
                            ManageMessageId.CannotRemoveOnlyLogin
                    });
            }

            var result =
                await UserManager.RemoveLoginAsync(
                    userId,
                    new UserLoginInfo(
                        loginProvider,
                        providerKey));

            if (!result.Succeeded)
            {
                return RedirectToAction(
                    "ManageLogins",
                    new
                    {
                        Message =
                            ManageMessageId.Error
                    });
            }

            await SignInManager.SignInAsync(
                user,
                isPersistent: false,
                rememberBrowser: false);

            return RedirectToAction(
                "ManageLogins",
                new
                {
                    Message =
                        ManageMessageId.RemoveLoginSuccess
                });
        }

        // GET: /Manage/AddPhoneNumber
        [HttpGet]
        public ActionResult AddPhoneNumber()
        {
            return RedirectToAction(
                "Index",
                new
                {
                    Message =
                        ManageMessageId.PhoneVerificationUnavailable
                });
        }

        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPhoneNumber(
            AddPhoneNumberViewModel model)
        {
            return RedirectToAction(
                "Index",
                new
                {
                    Message =
                        ManageMessageId.PhoneVerificationUnavailable
                });
        }

        // GET: /Manage/VerifyPhoneNumber
        [HttpGet]
        public ActionResult VerifyPhoneNumber(
            string phoneNumber)
        {
            return RedirectToAction(
                "Index",
                new
                {
                    Message =
                        ManageMessageId.PhoneVerificationUnavailable
                });
        }

        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyPhoneNumber(
            VerifyPhoneNumberViewModel model)
        {
            return RedirectToAction(
                "Index",
                new
                {
                    Message =
                        ManageMessageId.PhoneVerificationUnavailable
                });
        }

        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult>
            EnableTwoFactorAuthentication()
        {
            var userId =
                User.Identity.GetUserId();

            var providers =
                await UserManager
                    .GetValidTwoFactorProvidersAsync(
                        userId);

            if (providers == null ||
                providers.Count == 0)
            {
                return RedirectToAction(
                    "Index",
                    new
                    {
                        Message =
                            ManageMessageId
                                .TwoFactorUnavailable
                    });
            }

            var result =
                await UserManager
                    .SetTwoFactorEnabledAsync(
                        userId,
                        true);

            if (!result.Succeeded)
            {
                return RedirectToAction(
                    "Index",
                    new
                    {
                        Message =
                            ManageMessageId.Error
                    });
            }

            var user =
                await UserManager.FindByIdAsync(
                    userId);

            if (user != null)
            {
                await SignInManager.SignInAsync(
                    user,
                    isPersistent: false,
                    rememberBrowser: false);
            }

            return RedirectToAction(
                "Index",
                new
                {
                    Message =
                        ManageMessageId
                            .SetTwoFactorSuccess
                });
        }

        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult>
            DisableTwoFactorAuthentication()
        {
            var userId =
                User.Identity.GetUserId();

            var result =
                await UserManager
                    .SetTwoFactorEnabledAsync(
                        userId,
                        false);

            if (!result.Succeeded)
            {
                return RedirectToAction(
                    "Index",
                    new
                    {
                        Message =
                            ManageMessageId.Error
                    });
            }

            var user =
                await UserManager.FindByIdAsync(
                    userId);

            if (user != null)
            {
                await SignInManager.SignInAsync(
                    user,
                    isPersistent: false,
                    rememberBrowser: false);
            }

            return RedirectToAction(
                "Index",
                new
                {
                    Message =
                        ManageMessageId
                            .DisableTwoFactorSuccess
                });
        }

        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult>
            RemovePhoneNumber()
        {
            var userId =
                User.Identity.GetUserId();

            var result =
                await UserManager
                    .SetPhoneNumberAsync(
                        userId,
                        null);

            if (!result.Succeeded)
            {
                return RedirectToAction(
                    "Index",
                    new
                    {
                        Message =
                            ManageMessageId.Error
                    });
            }

            var user =
                await UserManager.FindByIdAsync(
                    userId);

            if (user != null)
            {
                await SignInManager.SignInAsync(
                    user,
                    isPersistent: false,
                    rememberBrowser: false);
            }

            return RedirectToAction(
                "Index",
                new
                {
                    Message =
                        ManageMessageId
                            .RemovePhoneSuccess
                });
        }

        // GET: /Manage/ChangePassword
        [HttpGet]
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId =
                User.Identity.GetUserId();

            var result =
                await UserManager.ChangePasswordAsync(
                    userId,
                    model.OldPassword,
                    model.NewPassword);

            if (!result.Succeeded)
            {
                AddErrors(result);

                return View(model);
            }

            var user =
                await UserManager.FindByIdAsync(
                    userId);

            if (user != null)
            {
                await SignInManager.SignInAsync(
                    user,
                    isPersistent: false,
                    rememberBrowser: false);
            }

            return RedirectToAction(
                "Index",
                new
                {
                    Message =
                        ManageMessageId
                            .ChangePasswordSuccess
                });
        }

        // GET: /Manage/SetPassword
        [HttpGet]
        public ActionResult SetPassword()
        {
            return View();
        }

        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(
            SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId =
                User.Identity.GetUserId();

            var result =
                await UserManager.AddPasswordAsync(
                    userId,
                    model.NewPassword);

            if (!result.Succeeded)
            {
                AddErrors(result);

                return View(model);
            }

            var user =
                await UserManager.FindByIdAsync(
                    userId);

            if (user != null)
            {
                await SignInManager.SignInAsync(
                    user,
                    isPersistent: false,
                    rememberBrowser: false);
            }

            return RedirectToAction(
                "Index",
                new
                {
                    Message =
                        ManageMessageId
                            .SetPasswordSuccess
                });
        }

        // GET: /Manage/ManageLogins
        [HttpGet]
        public async Task<ActionResult> ManageLogins(
            ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                GetStatusMessage(message);

            var userId =
                User.Identity.GetUserId();

            var user =
                await UserManager.FindByIdAsync(
                    userId);

            if (user == null)
            {
                return View("Error");
            }

            var userLogins =
                await UserManager.GetLoginsAsync(
                    userId);

            var externalAuthenticationTypes =
                AuthenticationManager
                    .GetExternalAuthenticationTypes()
                    .ToList();

            var otherLogins =
                externalAuthenticationTypes
                    .Where(auth =>
                        userLogins.All(login =>
                            !string.Equals(
                                auth.AuthenticationType,
                                login.LoginProvider,
                                StringComparison.Ordinal)))
                    .ToList();

            ViewBag.ShowRemoveButton =
                user.PasswordHash != null ||
                userLogins.Count > 1;

            return View(
                new ManageLoginsViewModel
                {
                    CurrentLogins =
                        userLogins,

                    OtherLogins =
                        otherLogins
                });
        }

        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(
            string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return RedirectToAction(
                    "ManageLogins",
                    new
                    {
                        Message =
                            ManageMessageId.Error
                    });
            }

            var providerExists =
                AuthenticationManager
                    .GetExternalAuthenticationTypes()
                    .Any(authentication =>
                        string.Equals(
                            authentication.AuthenticationType,
                            provider,
                            StringComparison.Ordinal));

            if (!providerExists)
            {
                return RedirectToAction(
                    "ManageLogins",
                    new
                    {
                        Message =
                            ManageMessageId.Error
                    });
            }

            return new AccountController.ChallengeResult(
                provider,
                Url.Action(
                    "LinkLoginCallback",
                    "Manage"),
                User.Identity.GetUserId());
        }

        // GET: /Manage/LinkLoginCallback
        [HttpGet]
        public async Task<ActionResult>
            LinkLoginCallback()
        {
            var userId =
                User.Identity.GetUserId();

            var loginInfo =
                await AuthenticationManager
                    .GetExternalLoginInfoAsync(
                        XsrfKey,
                        userId);

            if (loginInfo == null)
            {
                return RedirectToAction(
                    "ManageLogins",
                    new
                    {
                        Message =
                            ManageMessageId.Error
                    });
            }

            var result =
                await UserManager.AddLoginAsync(
                    userId,
                    loginInfo.Login);

            return RedirectToAction(
                "ManageLogins",
                result.Succeeded
                    ? null
                    : new
                    {
                        Message =
                            ManageMessageId.Error
                    });
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        private const string XsrfKey =
            "XsrfId";

        private IAuthenticationManager
            AuthenticationManager
        {
            get
            {
                return HttpContext
                    .GetOwinContext()
                    .Authentication;
            }
        }

        private static string GetStatusMessage(
            ManageMessageId? message)
        {
            switch (message)
            {
                case ManageMessageId
                    .ChangePasswordSuccess:
                    return "Your password has been changed.";

                case ManageMessageId
                    .SetPasswordSuccess:
                    return "Your password has been set.";

                case ManageMessageId
                    .SetTwoFactorSuccess:
                    return "Two-factor authentication has been enabled.";

                case ManageMessageId
                    .DisableTwoFactorSuccess:
                    return "Two-factor authentication has been disabled.";

                case ManageMessageId
                    .RemoveLoginSuccess:
                    return "The external login was removed.";

                case ManageMessageId
                    .RemovePhoneSuccess:
                    return "Your phone number was removed.";

                case ManageMessageId
                    .PhoneVerificationUnavailable:
                    return "Phone verification is not configured for this application.";

                case ManageMessageId
                    .TwoFactorUnavailable:
                    return "Two-factor authentication cannot be enabled because no verified provider is available.";

                case ManageMessageId
                    .CannotRemoveOnlyLogin:
                    return "You cannot remove your only sign-in method.";

                case ManageMessageId.Error:
                    return "An error has occurred.";

                default:
                    return string.Empty;
            }
        }

        private void AddErrors(
            IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error);
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            DisableTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            PhoneVerificationUnavailable,
            TwoFactorUnavailable,
            CannotRemoveOnlyLogin,
            Error
        }
    }
}