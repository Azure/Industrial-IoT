
namespace Microsoft.Azure.IIoT.App.Services {
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;

    [Controller]
    [AllowAnonymous]
    [Route("Account")]
    public class AuthStateController : Controller {

        [HttpGet("{scheme}/LogIn")]
        public IActionResult LogIn([FromRoute] string scheme) {
            var redirectUri = base.Url.Content("~/");
            var authProps = new AuthenticationProperties {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(30),
                RedirectUri = redirectUri
            };
            return Challenge(authProps, scheme);
        }

        [HttpGet("{scheme}/LogOut")]
        public IActionResult LogOut([FromRoute] string scheme) {
           // var redirectUri = base.Url.Page("/Account/SignedOut", null, null, base.Request.Scheme);
            var redirectUri = base.Url.Content("~/");
            var authProps = new AuthenticationProperties {
                RedirectUri = redirectUri
            };
            return SignOut(authProps, scheme + "Cookie", scheme);
        }
    }
}
