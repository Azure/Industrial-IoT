// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.TokenStorage;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Utils;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Controllers
{
    [Authorize]
    public class ApplicationController : Controller
    {
        private IOpcVault opcVault;
        private readonly OpcVaultApiOptions opcVaultOptions;
        private readonly AzureADOptions azureADOptions;
        private readonly ITokenCacheService tokenCacheService;

        public ApplicationController(
            OpcVaultApiOptions opcVaultOptions,
            AzureADOptions azureADOptions,
            ITokenCacheService tokenCacheService)
        {
            this.opcVaultOptions = opcVaultOptions;
            this.azureADOptions = azureADOptions;
            this.tokenCacheService = tokenCacheService;
        }


        [ActionName("Index")]
        public async Task<ActionResult> IndexAsync()
        {
            AuthorizeClient();
            var applicationQuery = new QueryApplicationsApiModel();
            var applications = await opcVault.QueryApplicationsAsync(applicationQuery);
            return View(applications.Applications);
        }

        [ActionName("Register")]
        public Task<ActionResult> RegisterAsync()
        {
            var application = new ApplicationRecordApiModel();
            UpdateApiModel(application);
            return Task.FromResult<ActionResult>(View(application));
        }

        [HttpPost]
        [ActionName("Register")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RegisterAsync(
            ApplicationRecordApiModel application,
            string add,
            string update)
        {
            UpdateApiModel(application);
            if (ModelState.IsValid && String.IsNullOrEmpty(add) && String.IsNullOrEmpty(update))
            {
                AuthorizeClient();
                await opcVault.RegisterApplicationAsync(application);
                return RedirectToAction("Index");
            }

            if (!String.IsNullOrEmpty(add))
            {
                application.DiscoveryUrls.Add("");
            }

            return View(application);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync(
            ApplicationRecordApiModel updatedApplication,
            string add,
            string update)
        {
            UpdateApiModel(updatedApplication);

            if (ModelState.IsValid && String.IsNullOrEmpty(add) && String.IsNullOrEmpty(update))
            {
                AuthorizeClient();
                var application = await opcVault.GetApplicationAsync(updatedApplication.ApplicationId);
                if (application == null)
                {
                    return new NotFoundResult();
                }

                application.ApplicationName = updatedApplication.ApplicationName;
                application.ApplicationType = updatedApplication.ApplicationType;
                application.ProductUri = updatedApplication.ProductUri;
                application.DiscoveryUrls = updatedApplication.DiscoveryUrls;
                application.ServerCapabilities = updatedApplication.ServerCapabilities;
                await opcVault.UpdateApplicationAsync(application.ApplicationId, application);
                return RedirectToAction("Index");
            }

            if (!String.IsNullOrEmpty(add))
            { 
                    updatedApplication.DiscoveryUrls.Add("");
            }

            return View(updatedApplication);
        }


        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                return new BadRequestResult();
            }
            AuthorizeClient();
            var application = await opcVault.GetApplicationAsync(id);
            if (application == null)
            {
                return new NotFoundResult();
            }

            return View(application);
        }

        [ActionName("Delete")]
        public async Task<ActionResult> DeleteAsync(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                return new BadRequestResult();
            }
            AuthorizeClient();
            var application = await opcVault.GetApplicationAsync(id);
            if (application == null)
            {
                return new NotFoundResult();
            }

            return View(application);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmedAsync([Bind("Id")] string id)
        {
            AuthorizeClient();
            await opcVault.UnregisterApplicationAsync(id);
            return RedirectToAction("Index");
        }

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id)
        {
            AuthorizeClient();
            var application = await opcVault.GetApplicationAsync(id);
            if (application == null)
            {
                return new NotFoundResult();
            }
            return View(application);
        }

        private void AuthorizeClient()
        {
            if (opcVault == null)
            {
                ServiceClientCredentials serviceClientCredentials =
                    new OpcVaultLoginCredentials(opcVaultOptions, azureADOptions, tokenCacheService, User);
                opcVault = new OpcVault(new Uri(opcVaultOptions.BaseAddress), serviceClientCredentials);
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