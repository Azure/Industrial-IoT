// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            try
            {
                var requests = await _opcVault.QueryCertificateRequestsAsync();
                while (requests != null)
                {
                    foreach (var request in requests.Requests)
                    {
                        var indexRequest = new CertificateRequestIndexApiModel(request);
                        ApplicationRecordApiModel application;
                        if (!appDictionary.TryGetValue(request.ApplicationId, out application))
                        {
                            application = await _opcVault.GetApplicationAsync(request.ApplicationId);
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
                    requests = await _opcVault.QueryCertificateRequestsAsync(nextPageLink);
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] =
                    "Failed to load all the certificate requests. " +
                    "Message:" + ex.Message;
            }
            return View(indexRequests);
        }

        [ActionName("Request")]
        public async Task<IActionResult> RequestAsync(string id)
        {
            AuthorizeClient();
            try
            {
                var application = await _opcVault.GetApplicationAsync(id);
                UpdateApiModel(application);
                return View(application);
            }
            catch (Exception ex)
            {
                var application = new ApplicationRecordApiModel();
                ViewData["ErrorMessage"] =
                    "Failed to find the application with ApplicationId " + id + "\r\n" +
                    "Message:" + ex.Message;
                return View(application);
            }
        }

        [ActionName("StartNewKeyPair")]
        public async Task<ActionResult> StartNewKeyPairAsync(string id)
        {
            AuthorizeClient();
            var groups = await _opcVault.GetCertificateGroupsConfigurationAsync();
            if (groups == null)
            {
                return new NotFoundResult();
            }

            string defaultGroupId, defaultTypeId;
            if (groups.Groups.Count > 0)
            {
                defaultGroupId = groups.Groups[0].Name;
                defaultTypeId = groups.Groups[0].CertificateType;
            }
            else
            {
                return new NotFoundResult();
            }

            var application = await _opcVault.GetApplicationAsync(id);
            if (application == null)
            {
                return new NotFoundResult();
            }

            // preset Domain names with discovery Urls
            var domainNames = new List<string>();
            if (application.DiscoveryUrls != null)
            {
                foreach (var discoveryUrl in application.DiscoveryUrls)
                {
                    Uri url = Opc.Ua.Utils.ParseUri(discoveryUrl);
                    if (url == null)
                    {
                        continue;
                    }

                    string domainName = url.DnsSafeHost;
                    if (url.HostNameType != UriHostNameType.Dns)
                    {
                        domainName = Opc.Ua.Utils.NormalizedIPAddress(domainName);
                    }

                    if (!Opc.Ua.Utils.FindStringIgnoreCase(domainNames, domainName))
                    {
                        domainNames.Add(domainName);
                    }
                }
            }

            ViewData["Application"] = application;
            ViewData["Groups"] = groups;

            var request = new CreateNewKeyPairRequestFormApiModel()
            {
                ApplicationId = id,
                CertificateGroupId = defaultGroupId,
                CertificateTypeId = defaultTypeId,
                DomainNames = domainNames
            };
            UpdateApiModel(request);
            return View(request);
        }

        [HttpPost]
        [ActionName("StartNewKeyPair")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> StartNewKeyPairAsync(
            CreateNewKeyPairRequestFormApiModel request,
            string add,
            string del)
        {
            AuthorizeClient();
            UpdateApiModel(request);
            if (ModelState.IsValid &&
                String.IsNullOrEmpty(add) &&
                String.IsNullOrEmpty(del))
            {
                string id;
                try
                {
                    id = await _opcVault.CreateNewKeyPairRequestAsync(request);
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] =
                        "Failed to create Certificate Request.\r\n" +
                        "Message:" + ex.Message;
                    goto LoadAppAndView;
                }
                string errorMessage = null;
                try
                {
                    // TODO: call depending on auto approve setup
                    //await opcVault.ApproveCertificateRequestAsync(id, false);
                }
                catch (Exception ex)
                {
                    errorMessage =
                    "Failed to approve Certificate Request.\r\n" +
                    "Please contact Administrator for approval." +
                    ex.Message;
                }
                return RedirectToAction("Details", new { id, errorMessage });
            }

            if (!String.IsNullOrWhiteSpace(request.DomainNames.Last()) &&
                !String.IsNullOrEmpty(add))
            {
                request.DomainNames.Add("");
            }

            LoadAppAndView:
            // reload app info
            var application = await _opcVault.GetApplicationAsync(request.ApplicationId);
            if (application == null)
            {
                return new NotFoundResult();
            }

            ViewData["Application"] = application;

            return View(request);
        }

        [ActionName("StartSigning")]
        public async Task<ActionResult> StartSigningAsync(string id)
        {
            AuthorizeClient();
            var groups = await _opcVault.GetCertificateGroupsConfigurationAsync();
            if (groups == null)
            {
                return new NotFoundResult();
            }

            string defaultGroupId, defaultTypeId;
            if (groups.Groups.Count > 0)
            {
                defaultGroupId = groups.Groups[0].Name;
                defaultTypeId = groups.Groups[0].CertificateType;
            }
            else
            {
                return new NotFoundResult();
            }

            var application = await _opcVault.GetApplicationAsync(id);
            if (application == null)
            {
                return new NotFoundResult();
            }

            var request = new CreateSigningRequestUploadApiModel()
            {
                ApiModel = new CreateSigningRequestApiModel()
                {
                    ApplicationId = id,
                    CertificateGroupId = defaultGroupId,
                    CertificateTypeId = defaultTypeId
                },
                ApplicationUri = application.ApplicationUri,
                ApplicationName = application.ApplicationName
            };

            return View(request);
        }

        [HttpPost]
        [ActionName("StartSigning")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> StartSigningAsync(
            CreateSigningRequestUploadApiModel request)
        {
            if (ModelState.IsValid && (request.CertificateRequestFile != null || request.ApiModel.CertificateRequest != null))
            {
                var requestApi = request.ApiModel;
                if (request.CertificateRequestFile != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await request.CertificateRequestFile.CopyToAsync(memoryStream);
                        requestApi.CertificateRequest = Convert.ToBase64String(memoryStream.ToArray());
                    }
                }
                AuthorizeClient();
                string errorMessage = null;
                string id;
                try
                {
                    id = await _opcVault.CreateSigningRequestAsync(requestApi);
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] =
                        "Failed to create Signing Request.\r\n" +
                        "Message:" + ex.Message;
                    return View(request);
                }

                try
                {
                    // TODO: call depending on auto approve setup
                    //await opcVault.ApproveCertificateRequestAsync(id, false);
                }
                catch (Exception ex)
                {
                    errorMessage =
                    "Failed to approve signing request." +
                    "Message: " + ex.Message;
                }
                return RedirectToAction("Details", new { id, errorMessage });
            }
            return View(request);
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
            }
            if (application.DiscoveryUrls.Count == 0)
            {
                application.DiscoveryUrls.Add("");
            }
        }

        private void UpdateApiModel(CreateNewKeyPairRequestFormApiModel request)
        {
            if (request.DomainNames != null)
            {
                request.DomainNames = request.DomainNames.Where(x => !string.IsNullOrEmpty(x)).ToList();
            }
            else
            {
                request.DomainNames = new List<string>();
            }
            if (request.DomainNames.Count == 0)
            {
                request.DomainNames.Add("");
            }
        }

    }
}
