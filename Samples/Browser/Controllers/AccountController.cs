// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Browser.Controllers {
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.IoTSolutions.Shared.Diagnostics;
    using Microsoft.Azure.IoTSolutions.Browser.Filters;
    using System.Threading.Tasks;

    /// <summary>
    /// Login/out
    /// </summary>
    [Route("/")]
    [ExceptionsFilter]
    public class AccountController : Controller {

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="logger"></param>
        public AccountController(ILogger logger) {
            _logger = logger;
        }

        /// <summary>
        /// Check if user is signed in and forward to the browser view or to authentication.
        /// </summary>
        [HttpGet]
        public IActionResult SignIn() {
            // var redirectUrl = Url.Action("Index", "Endpoints");
            //       OpenIdConnectDefaults.AuthenticationScheme);
            var result = RedirectToAction("Index", "Endpoints");
            _logger.Info($"SignIn - IsAuthenticated: {User.Identity.IsAuthenticated}",
              () => { });
            return result;
        }

        [HttpGet("SignOut")]
        public IActionResult SignOut() {
            var callbackUrl = Url.Action(nameof(SignedOut), "Account", values: null, protocol: Request.Scheme);
            _logger.Info($"SignOut - User: {User.Identity.Name}, AuthenticationType: {User.Identity.AuthenticationType}",
                () => { });
            return SignOut(new AuthenticationProperties { RedirectUri = callbackUrl },
                CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet("SignedOut")]
        public IActionResult SignedOut() {
            if (User.Identity.IsAuthenticated) {
                // Redirect to endpoints page if the user is authenticated.
                return RedirectToAction("Index", "Endpoints");
            }
            return View();
        }

        [HttpGet("AccessDenied")]
        public IActionResult AccessDenied() {
            return View();
        }

        private readonly ILogger _logger;
    }
}