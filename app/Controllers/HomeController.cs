// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult OnPostWithdraw()
        {
            HttpContext.Features.Get<ITrackingConsentFeature>().WithdrawConsent();
            return RedirectToPage("Index");
        }

        /// <summary>
        /// This view is invoked by the OpenIDConnect middleware after setting the authentication ticket
        /// as an intermediate step to make sure that the session cookie is sent
        /// to the returnUrl request in Safari iOS 12/Mojave.
        /// It is marked as AllowAnonymous because this request will look as
        /// unauthenticated on Safari.
        /// </summary>
        [AllowAnonymous]
        public IActionResult Continue(string returnUrl = "/")
        {
            ViewBag.returnUrl = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = HttpContext.Response.StatusCode });
        }
    }
}
