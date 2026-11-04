using System;
using System.Diagnostics;
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
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(
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

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(
            LoginViewModel model,
            string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (model == null)
            {
                return View("Error");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email =
                model.Email.Trim();

            var result =
                await SignInManager.PasswordSignInAsync(
                    email,
                    model.Password,
                    model.RememberMe,
                    shouldLockout: true);

            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);

                case SignInStatus.LockedOut:
                    return View("Lockout");

                case SignInStatus.RequiresVerification:
                    return RedirectToAction(
                        "SendCode",
                        new
                        {
                            ReturnUrl = returnUrl,
                            RememberMe = model.RememberMe
                        });

                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError(
                        "",
                        "Invalid email address or password.");

                    return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(
            string provider,
            string returnUrl,
            bool rememberMe)
        {
            if (string.IsNullOrWhiteSpace(provider) ||
                !await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }

            var userId =
                await SignInManager.GetVerifiedUserIdAsync();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return View("Error");
            }

            var validProviders =
                await UserManager
                    .GetValidTwoFactorProvidersAsync(userId);

            if (!validProviders.Contains(provider))
            {
                return View("Error");
            }

            return View(
                new VerifyCodeViewModel
                {
                    Provider = provider,
                    ReturnUrl = returnUrl,
                    RememberMe = rememberMe
                });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(
            VerifyCodeViewModel model)
        {
            if (model == null)
            {
                return View("Error");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId =
                await SignInManager.GetVerifiedUserIdAsync();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return View("Error");
            }

            var validProviders =
                await UserManager
                    .GetValidTwoFactorProvidersAsync(userId);

            if (string.IsNullOrWhiteSpace(model.Provider) ||
                !validProviders.Contains(model.Provider))
            {
                return View("Error");
            }

            var result =
                await SignInManager.TwoFactorSignInAsync(
                    model.Provider,
                    model.Code,
                    isPersistent: model.RememberMe,
                    rememberBrowser: model.RememberBrowser);

            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(
                        model.ReturnUrl);

                case SignInStatus.LockedOut:
                    return View("Lockout");

                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError(
                        "",
                        "Invalid verification code.");

                    return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(
            RegisterViewModel model)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (model == null)
            {
                return View("Error");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email =
                model.Email.Trim();

            var user =
                new ApplicationUser
                {
                    UserName = email,
                    Email = email
                };

            var result =
                await UserManager.CreateAsync(
                    user,
                    model.Password);

            if (!result.Succeeded)
            {
                var existingUser =
                    await UserManager.FindByEmailAsync(email);

                if (existingUser == null)
                {
                    existingUser =
                        await UserManager.FindByNameAsync(email);
                }

                if (existingUser != null)
                {
                    ModelState.AddModelError(
                        "",
                        "An account with this email address already exists. " +
                        "Sign in or reset your password.");

                    ViewBag.EmailAlreadyRegistered = true;
                }
                else
                {
                    AddErrors(result);
                }

                return View(model);
            }

            await TrySendEmailConfirmationAsync(user);

            await SignInManager.SignInAsync(
                user,
                isPersistent: false,
                rememberBrowser: false);

            return RedirectToAction(
                "Index",
                "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(
            string userId,
            string code)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(code))
            {
                return View("Error");
            }

            var user = await UserManager.FindByIdAsync(userId);

            if (user == null)
            {
                return View("Error");
            }

            var result = await UserManager.ConfirmEmailAsync(
                userId,
                code);

            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (model == null)
            {
                return View("Error");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email =
                model.Email.Trim();

            var user =
                await UserManager.FindByEmailAsync(
                    email);

            if (user == null)
            {
                return RedirectToAction(
                    "ForgotPasswordConfirmation");
            }

            try
            {
                var code =
                    await UserManager
                        .GeneratePasswordResetTokenAsync(
                            user.Id);

                var callbackUrl = CreateAbsoluteCallbackUrl(
                    "ResetPassword",
                    new { code = code });

                var encodedCallbackUrl =
                    HttpUtility.HtmlAttributeEncode(
                        callbackUrl);

                await UserManager.SendEmailAsync(
                    user.Id,
                    "Reset your password | MovieTicket",
                    "To reset your MovieTicket password, " +
                    "select <a href=\"" + encodedCallbackUrl +
                    "\">Reset password</a>.");
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "Password reset e-mail could not be sent: {0}",
                    ex);
            }

            return RedirectToAction(
                "ForgotPasswordConfirmation");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ResetPassword(
            string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return View("Error");
            }

            return View(
                new ResetPasswordViewModel
                {
                    Code = code
                });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (model == null)
            {
                return View("Error");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user =
                await UserManager.FindByEmailAsync(
                    model.Email.Trim());

            if (user == null)
            {
                return RedirectToAction(
                    "ResetPasswordConfirmation");
            }

            var result =
                await UserManager.ResetPasswordAsync(
                    user.Id,
                    model.Code,
                    model.Password);

            if (result.Succeeded)
            {
                return RedirectToAction(
                    "ResetPasswordConfirmation");
            }

            AddErrors(result);

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(
            string provider,
            string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(provider) ||
                !AuthenticationManager.GetExternalAuthenticationTypes()
                    .Any(authenticationType =>
                        string.Equals(
                            authenticationType.AuthenticationType,
                            provider,
                            StringComparison.Ordinal)))
            {
                return RedirectToAction("Login");
            }

            return new ChallengeResult(
                provider,
                Url.Action(
                    "ExternalLoginCallback",
                    "Account",
                    new
                    {
                        ReturnUrl = returnUrl
                    }));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(
            string returnUrl,
            bool rememberMe)
        {
            var model =
                await CreateSendCodeViewModelAsync(
                    returnUrl,
                    rememberMe);

            if (model == null ||
                model.Providers == null ||
                !model.Providers.Any())
            {
                return View("Error");
            }

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(
            SendCodeViewModel model)
        {
            if (model == null)
            {
                return View("Error");
            }

            var userId =
                await SignInManager
                    .GetVerifiedUserIdAsync();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return View("Error");
            }

            var validProviders =
                await UserManager
                    .GetValidTwoFactorProvidersAsync(
                        userId);

            if (!ModelState.IsValid ||
                string.IsNullOrWhiteSpace(
                    model.SelectedProvider) ||
                !validProviders.Contains(
                    model.SelectedProvider))
            {
                model.Providers =
                    validProviders
                        .Select(provider =>
                            new SelectListItem
                            {
                                Text = provider,
                                Value = provider
                            })
                        .ToList();

                if (string.IsNullOrWhiteSpace(
                        model.SelectedProvider) ||
                    !validProviders.Contains(
                        model.SelectedProvider))
                {
                    ModelState.AddModelError(
                        "SelectedProvider",
                        "Invalid verification method.");
                }

                return View(model);
            }

            bool sent;

            try
            {
                sent =
                    await SignInManager
                        .SendTwoFactorCodeAsync(
                            model.SelectedProvider);
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "Two-factor authentication code could not be sent: {0}",
                    ex);

                ModelState.AddModelError(
                    "",
                    "We could not send your verification code. " +
                    "Please try again later.");

                model.Providers =
                    validProviders
                        .Select(provider =>
                            new SelectListItem
                            {
                                Text = provider,
                                Value = provider
                            })
                        .ToList();

                return View(model);
            }

            if (!sent)
            {
                ModelState.AddModelError(
                    "",
                    "We could not send your verification code. " +
                    "Please try again later.");

                model.Providers =
                    validProviders
                        .Select(provider =>
                            new SelectListItem
                            {
                                Text = provider,
                                Value = provider
                            })
                        .ToList();

                return View(model);
            }

            return RedirectToAction(
                "VerifyCode",
                new
                {
                    Provider =
                        model.SelectedProvider,

                    ReturnUrl =
                        model.ReturnUrl,

                    RememberMe =
                        model.RememberMe
                });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(
            string returnUrl)
        {
            var loginInfo =
                await AuthenticationManager
                    .GetExternalLoginInfoAsync();

            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            var result =
                await SignInManager.ExternalSignInAsync(
                    loginInfo,
                    isPersistent: false);

            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);

                case SignInStatus.LockedOut:
                    return View("Lockout");

                case SignInStatus.RequiresVerification:
                    return RedirectToAction(
                        "SendCode",
                        new
                        {
                            ReturnUrl = returnUrl,
                            RememberMe = false
                        });

                case SignInStatus.Failure:
                default:
                    ViewBag.ReturnUrl =
                        returnUrl;

                    ViewBag.LoginProvider =
                        loginInfo.Login.LoginProvider;

                    return View(
                        "ExternalLoginConfirmation",
                        new ExternalLoginConfirmationViewModel
                        {
                            Email = loginInfo.Email
                        });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult>
            ExternalLoginConfirmation(
                ExternalLoginConfirmationViewModel model,
                string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction(
                    "Index",
                    "Manage");
            }

            var info =
                await AuthenticationManager
                    .GetExternalLoginInfoAsync();

            if (info == null)
            {
                return View(
                    "ExternalLoginFailure");
            }

            ViewBag.ReturnUrl =
                returnUrl;

            ViewBag.LoginProvider =
                info.Login.LoginProvider;

            if (model == null)
            {
                return View("Error");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email =
                model.Email.Trim();

            var user =
                new ApplicationUser
                {
                    UserName = email,
                    Email = email
                };

            var result =
                await UserManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                AddErrors(result);

                return View(model);
            }

            var loginResult =
                await UserManager.AddLoginAsync(
                    user.Id,
                    info.Login);

            if (!loginResult.Succeeded)
            {
                var deleteResult =
                    await UserManager.DeleteAsync(user);

                if (!deleteResult.Succeeded)
                {
                    Trace.TraceError(
                        "Failed to remove user {0} after external login association failed: {1}",
                        user.Id,
                        string.Join(
                            "; ",
                            deleteResult.Errors));
                }

                AddErrors(loginResult);

                return View(model);
            }

            await TrySendEmailConfirmationAsync(user);

            await SignInManager.SignInAsync(
                user,
                isPersistent: false,
                rememberBrowser: false);

            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(
                DefaultAuthenticationTypes.ApplicationCookie,
                DefaultAuthenticationTypes.ExternalCookie,
                DefaultAuthenticationTypes.TwoFactorCookie);

            return RedirectToAction(
                "Index",
                "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
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

        private async Task<SendCodeViewModel>
            CreateSendCodeViewModelAsync(
                string returnUrl,
                bool rememberMe)
        {
            var userId =
                await SignInManager
                    .GetVerifiedUserIdAsync();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var providers =
                await UserManager
                    .GetValidTwoFactorProvidersAsync(
                        userId);

            return new SendCodeViewModel
            {
                Providers =
                    providers
                        .Select(provider =>
                            new SelectListItem
                            {
                                Text = provider,
                                Value = provider
                            })
                        .ToList(),

                ReturnUrl =
                    returnUrl,

                RememberMe =
                    rememberMe
            };
        }

        private async Task TrySendEmailConfirmationAsync(
            ApplicationUser user)
        {
            if (user == null || user.EmailConfirmed)
            {
                return;
            }

            try
            {
                var code = await UserManager
                    .GenerateEmailConfirmationTokenAsync(user.Id);

                var callbackUrl = CreateAbsoluteCallbackUrl(
                    "ConfirmEmail",
                    new { userId = user.Id, code = code });

                var encodedUrl = HttpUtility.HtmlAttributeEncode(
                    callbackUrl);

                await UserManager.SendEmailAsync(
                    user.Id,
                    "Confirm your email address | MovieTicket",
                    "To confirm your MovieTicket email address, " +
                    "select <a href=\"" + encodedUrl +
                    "\">Confirm email address</a>.");
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "Email confirmation could not be sent for user {0}: {1}",
                    user.Id,
                    ex);
            }
        }

        private string CreateAbsoluteCallbackUrl(
            string action,
            object routeValues)
        {
            var relativeUrl = Url.Action(
                action,
                "Account",
                routeValues);

            if (string.IsNullOrWhiteSpace(relativeUrl))
            {
                throw new InvalidOperationException(
                    "An account callback URL could not be generated.");
            }

            var publicBaseUrl = Environment.GetEnvironmentVariable(
                "MOVIETICKET_PUBLIC_BASE_URL");

            if (string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                var requestHost = Request.Url == null
                    ? null
                    : Request.Url.Host;

                var isLocalHost =
                    string.Equals(requestHost, "localhost",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(requestHost, "127.0.0.1",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(requestHost, "::1",
                        StringComparison.OrdinalIgnoreCase);

                if (!Request.IsLocal || !isLocalHost)
                {
                    throw new InvalidOperationException(
                        "MOVIETICKET_PUBLIC_BASE_URL must be configured " +
                        "for public deployments.");
                }

                var fallbackUrl = Url.Action(
                    action,
                    "Account",
                    routeValues,
                    "https");

                if (string.IsNullOrWhiteSpace(fallbackUrl))
                {
                    throw new InvalidOperationException(
                        "An account callback URL could not be generated.");
                }

                return fallbackUrl;
            }

            Uri baseUri;
            var baseUrl = publicBaseUrl.Trim().TrimEnd('/') + "/";

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out baseUri) ||
                baseUri.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(baseUri.UserInfo) ||
                baseUri.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(baseUri.Query) ||
                !string.IsNullOrEmpty(baseUri.Fragment))
            {
                throw new InvalidOperationException(
                    "MOVIETICKET_PUBLIC_BASE_URL must be an HTTPS origin without a path, query, or fragment.");
            }

            return new Uri(
                baseUri,
                relativeUrl.TrimStart('/')).AbsoluteUri;
        }

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext
                    .GetOwinContext()
                    .Authentication;
            }
        }

        private void AddErrors(
            IdentityResult result)
        {
            if (result == null ||
                result.Errors == null)
            {
                return;
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error);
            }
        }

        private ActionResult RedirectToLocal(
            string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(
                "Index",
                "Home");
        }

        private const string XsrfKey =
            "XsrfId";

        internal class ChallengeResult
            : HttpUnauthorizedResult
        {
            public ChallengeResult(
                string provider,
                string redirectUri)
                : this(
                    provider,
                    redirectUri,
                    null)
            {
            }

            public ChallengeResult(
                string provider,
                string redirectUri,
                string userId)
            {
                LoginProvider =
                    provider;

                RedirectUri =
                    redirectUri;

                UserId =
                    userId;
            }

            public string LoginProvider
            {
                get;
                set;
            }

            public string RedirectUri
            {
                get;
                set;
            }

            public string UserId
            {
                get;
                set;
            }

            public override void ExecuteResult(
                ControllerContext context)
            {
                var properties =
                    new AuthenticationProperties
                    {
                        RedirectUri =
                            RedirectUri
                    };

                if (!string.IsNullOrWhiteSpace(
                    UserId))
                {
                    properties.Dictionary[
                        XsrfKey] =
                        UserId;
                }

                context.HttpContext
                    .GetOwinContext()
                    .Authentication
                    .Challenge(
                        properties,
                        LoginProvider);
            }
        }
    }
}