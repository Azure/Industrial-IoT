// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.TokenStorage;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Controllers
{
    [Authorize]
    public class CertificateRequestController : DownloadController
    {

        public CertificateRequestController(
            OpcVaultApiOptions opcVaultOptions,
            AzureADOptions azureADOptions,
            ITokenCacheService tokenCacheService) :
            base(opcVaultOptions, azureADOptions, tokenCacheService)
        {
        }

        [ActionName("Index")]
        public async Task<ActionResult> IndexAsync()
        {
            var appDictionary = new Dictionary<string, ApplicationRecordApiModel>();
            AuthorizeClient();
            string nextPageLink = null;
            var indexRequests = new List<CertificateRequestIndexApiModel>();
            var requests = await opcVault.QueryRequestsAsync();
            while (requests != null)
            {
                foreach (var request in requests.Requests)
                {
                    var indexRequest = new CertificateRequestIndexApiModel(request);
                    ApplicationRecordApiModel application;
                    if (!appDictionary.TryGetValue(request.ApplicationId, out application))
                    {
                        application = await opcVault.GetApplicationAsync(request.ApplicationId);
                    }

                    if (application != null)
                    {
                        appDictionary[request.ApplicationId] = application;
                        indexRequest.ApplicationName = application.ApplicationName;
                        indexRequest.ApplicationUri = application.ApplicationUri;
                    }
                    indexRequests.Add(indexRequest);
                }
                if (requests.NextPageLink == null)
                {
                    break;
                }
                nextPageLink = requests.NextPageLink;
                requests = await opcVault.QueryRequestsNextAsync(nextPageLink);
            }

            return View(indexRequests);
        }
    }
}
