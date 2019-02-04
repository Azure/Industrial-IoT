// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.TokenStorage;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Utils;
using Microsoft.Rest;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Controllers
{
    [Authorize]
    public class ApplicationController : Controller
    {
        private IOpcVault _opcVault;
        private readonly OpcVaultApiOptions _opcVaultOptions;
        private readonly AzureADOptions _azureADOptions;
        private readonly ITokenCacheService _tokenCacheService;

        public ApplicationController(
            OpcVaultApiOptions opcVaultOptions,
            AzureADOptions azureADOptions,
            ITokenCacheService tokenCacheService)
        {
            _opcVaultOptions = opcVaultOptions;
            _azureADOptions = azureADOptions;
            _tokenCacheService = tokenCacheService;
        }


        [ActionName("Index")]
        public async Task<ActionResult> IndexAsync()
        {
            AuthorizeClient();
            var applicationQuery = new QueryApplicationsApiModel();
            var applicationsTrimmed = new List<ApplicationRecordTrimmedApiModel>();
            string nextPageLink = null;
            do
            {
                var applications = await _opcVault.QueryApplicationsAsync(applicationQuery, nextPageLink);
                foreach (var app in applications.Applications)
                {
                    applicationsTrimmed.Add(new ApplicationRecordTrimmedApiModel(app));
                }
                nextPageLink = applications.NextPageLink;
            } while (nextPageLink != null);
            return View(applicationsTrimmed);
        }

        [ActionName("Unregister")]
        public async Task<ActionResult> DeleteAsync(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                return new BadRequestResult();
            }
            AuthorizeClient();
            var application = await _opcVault.GetApplicationAsync(id);
            if (application == null)
            {
                return new NotFoundResult();
            }

            return View(application);
        }

        [HttpPost]
        [ActionName("Unregister")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UnregisterConfirmedAsync([Bind("Id")] string id)
        {
            AuthorizeClient();
            await _opcVault.UnregisterApplicationAsync(id);
            return RedirectToAction("Index");
        }

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id)
        {
            AuthorizeClient();
            var application = await _opcVault.GetApplicationAsync(id);
            if (application == null)
            {
                return new NotFoundResult();
            }
            return View(application);
        }

        [ActionName("Register")]
        public async Task<IActionResult> RegisterAsync(string id)
        {
            var apiModel = new ApplicationRecordApiModel();
            AuthorizeClient();
            if (id != null)
            {
                try
                {
                    apiModel = await _opcVault.GetApplicationAsync(id);
                    ViewData["SuccessMessage"] =
                        "Application with id " + id + " successfully loaded.";
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] =
                        "An application with id " + id + " could not be found in the database.\r\n" +
                        "Message:" + ex.Message;
                }
            }
            UpdateApiModel(apiModel);
            return View(new ApplicationRecordRegisterApiModel(apiModel));
        }

        [HttpPost]
        [ActionName("Register")]
        [ValidateAntiForgeryToken]
        [ApplicationRecordRegisterApiModel]
        public async Task<ActionResult> RegisterAsync(
            ApplicationRecordRegisterApiModel apiModel,
            string find,
            string reg,
            string add,
            string del,
            string req)
        {
            string command = null;
            if (!String.IsNullOrEmpty(find)) { command = "find"; }
            if (!String.IsNullOrEmpty(add)) { command = "add"; }
            if (!String.IsNullOrEmpty(del)) { command = "delete"; }
            if (!String.IsNullOrEmpty(reg)) { command = "register"; }
            if (!String.IsNullOrEmpty(req)) { command = "request"; }

            UpdateApiModel(apiModel);

            if (ModelState.IsValid &&
                command == "request" &&
                apiModel.ApplicationId != null)
            {
                return RedirectToAction("Request", "CertificateRequest", new { id = apiModel.ApplicationId });
            }

            if (ModelState.IsValid &&
                command == "register")
            {
                AuthorizeClient();
                try
                {
                    if (apiModel.ApplicationType == ApplicationType.Client)
                    {
                        apiModel.ServerCapabilities = null;
                        apiModel.DiscoveryUrls = null;
                    }
                    var application = await _opcVault.RegisterApplicationAsync(apiModel);
                    apiModel = new ApplicationRecordRegisterApiModel(application);
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] =
                        "The application registration failed.\r\n" +
                        "Message: " + ex.Message;
                    return View(apiModel);
                }
                return RedirectToAction("Request", "CertificateRequest", new { id = apiModel.ApplicationId });
            }

            if (command == "find")
            {
                AuthorizeClient();
                try
                {
                    var applications = await _opcVault.ListApplicationsAsync(apiModel.ApplicationUri);
                    if (applications == null ||
                        applications.Applications == null ||
                        applications.Applications.Count == 0)
                    {
                        ViewData["ErrorMessage"] =
                            "Couldn't find the application with ApplicationUri " + apiModel.ApplicationUri;
                    }
                    else
                    {
                        var lastApp = applications.Applications.LastOrDefault();
                        return RedirectToAction("Register", new { id = lastApp.ApplicationId });
                    }
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] =
                        "Failed to find the application with ApplicationUri" + apiModel.ApplicationUri + "\r\n" +
                        "Message:" + ex.Message;
                    return View(apiModel);
                }
            }

            if (!String.IsNullOrWhiteSpace(apiModel.DiscoveryUrls.Last()) &&
                command == "add")
            {
                apiModel.DiscoveryUrls.Add("");
            }

            return View(apiModel);
        }


        private void AuthorizeClient()
        {
            if (_opcVault == null)
            {
                ServiceClientCredentials serviceClientCredentials =
                    new OpcVaultLoginCredentials(_opcVaultOptions, _azureADOptions, _tokenCacheService, User);
                _opcVault = new OpcVault(new Uri(_opcVaultOptions.BaseAddress), serviceClientCredentials);
            }
        }

        private void UpdateApiModel(ApplicationRecordApiModel application)
        {
            if (application.ApplicationNames != null)
            {
                application.ApplicationNames = application.ApplicationNames.Where(x => !string.IsNullOrEmpty(x.Text)).ToList();
            }
            else
            {
                application.ApplicationNames = new List<ApplicationNameApiModel>();
            }
            if (application.ApplicationNames.Count == 0)
            {
                application.ApplicationNames.Add(new ApplicationNameApiModel(null, application.ApplicationName));
            }
            else
            {
                application.ApplicationNames[0] = new ApplicationNameApiModel(null, application.ApplicationName);
            }
            if (application.DiscoveryUrls != null)
            {
                application.DiscoveryUrls = application.DiscoveryUrls.Where(x => !string.IsNullOrEmpty(x)).ToList();
            }
            else
            {
                application.DiscoveryUrls = new List<string>();
                if (application.DiscoveryUrls.Count == 0)
                {
                    application.DiscoveryUrls.Add("");
                }
            }
        }

    }
}
