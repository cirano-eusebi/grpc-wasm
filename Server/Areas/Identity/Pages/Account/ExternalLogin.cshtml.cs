﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using grpc_wasm.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace grpc_wasm.Server.Areas.Identity.Pages.Account
{
	[AllowAnonymous]
	public class ExternalLoginModel : PageModel
	{
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IUserStore<ApplicationUser> _userStore;
		private readonly IUserEmailStore<ApplicationUser> _emailStore;
		private readonly ILogger<ExternalLoginModel> _logger;

		public ExternalLoginModel(
			SignInManager<ApplicationUser> signInManager,
			UserManager<ApplicationUser> userManager,
			IUserStore<ApplicationUser> userStore,
			ILogger<ExternalLoginModel> logger)
		{
			_signInManager = signInManager;
			_userManager = userManager;
			_userStore = userStore;
			_emailStore = GetEmailStore();
			_logger = logger;
		}


		/// <summary>
		///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		public string ProviderDisplayName { get; set; }

		/// <summary>
		///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		public string ReturnUrl { get; set; }

		/// <summary>
		///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		[TempData]
		public string ErrorMessage { get; set; }

		public IActionResult OnGet() => RedirectToPage("./Login");

		public IActionResult OnPost(string provider, string returnUrl = null)
		{
			// Request a redirect to the external login provider.
			var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
			var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
			return new ChallengeResult(provider, properties);
		}

		public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
		{
			returnUrl = returnUrl ?? Url.Content("~/");
			if (remoteError != null)
			{
				ErrorMessage = $"Error from external provider: {remoteError}";
				return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
			}
			var info = await _signInManager.GetExternalLoginInfoAsync();
			if (info == null)
			{
				ErrorMessage = "Error loading external login information.";
				return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
			}

			// Sign in the user with this external login provider if the user already has a login.
			var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
			if (result.Succeeded)
			{
				_logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
				return LocalRedirect(returnUrl);
			}
			if (result.IsLockedOut)
			{
				return RedirectToPage("./Lockout");
			}
			else
			{
				// Create the account
				if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
				{
					var user = new ApplicationUser();
					var email = info.Principal.FindFirst(ClaimTypes.Email).Value;
					await _userStore.SetUserNameAsync(user, email, CancellationToken.None);
					await _emailStore.SetEmailAsync(user, email, CancellationToken.None);

					if ((await _userManager.CreateAsync(user)).Succeeded && await AddUserLogin(user, email, info, returnUrl))
					{
						return LocalRedirect(returnUrl);
					}
				}
				// If the user does not have an email associated with the account, then ask the user to login again.
				// We must be missing some claim.
				return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
			}
		}
		private async Task<bool> AddUserLogin(ApplicationUser user, string email, UserLoginInfo info, string returnUrl)
		{
			var result = await _userManager.AddLoginAsync(user, info);
			if (result.Succeeded)
			{
				_logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

				// Add Claims here.

				await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
				return true;
			}
			return false;
		}

		private IUserEmailStore<ApplicationUser> GetEmailStore()
		{
			if (!_userManager.SupportsUserEmail)
			{
				throw new NotSupportedException("The default UI requires a user store with email support.");
			}
			return (IUserEmailStore<ApplicationUser>)_userStore;
		}
	}
}
