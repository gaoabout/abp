using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Identity;
using Volo.Abp.Ui;
using Volo.Abp.Uow;

namespace Volo.Abp.Account.Web.Pages.Account
{
    public class ExternalLoginModel : AccountModelBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public ExternalLoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string LoginProvider { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public IActionResult OnGetAsync()
        {
            return RedirectToPage("./Login");
        }

        [UnitOfWork]
        public virtual IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        [UnitOfWork]
        public virtual async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null, string returnUrlHash = "")
        {
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToPage("./Login");
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                return RedirectSafely(returnUrl, returnUrlHash);
            }

            if (result.IsLockedOut)
            {
                throw new UserFriendlyException("Cannot proceed because user is locked out!");
            }

            ReturnUrl = returnUrl;
            LoginProvider = info.LoginProvider;

            //User does not have an account, create an account.
            var success = await CreateUserAsync(returnUrl, returnUrlHash);

            if (success)
            {
                return RedirectSafely(returnUrl, returnUrlHash);
            }

            return Page();
        }

        [UnitOfWork]
        public virtual async Task<bool> CreateUserAsync(string returnUrl = null, string returnUrlHash = null)
        {
            if (!ModelState.IsValid)
            {
                return false;
            }

            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                throw new ApplicationException("Error loading external login information during confirmation.");
            }

            var user = new IdentityUser(GuidGenerator.Create(), info.Principal.FindFirstValue(ClaimTypes.Email));

            var result = await _userManager.CreateAsync(user);

            //todo: needs to check identity errors?
            //CheckIdentityErrors( await _userManager.CreateAsync(user));

            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, false);
                    return true;
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return false;
        }
    }
}