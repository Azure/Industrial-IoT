// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
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
    public class CertificateGroupController : Controller
    {
        private IOpcVault _opcVault;
        private readonly OpcVaultApiOptions _opcVaultOptions;
        private readonly AzureADOptions _azureADOptions;
        private readonly ITokenCacheService _tokenCacheService;

        public CertificateGroupController(
            OpcVaultApiOptions opcVaultOptions,
            AzureADOptions azureADOptions,
            ITokenCacheService tokenCacheService)
        {
            this._opcVaultOptions = opcVaultOptions;
            this._azureADOptions = azureADOptions;
            this._tokenCacheService = tokenCacheService;
        }

        [ActionName("Index")]
        public async Task<ActionResult> IndexAsync()
        {
            AuthorizeClient();
            var requests = await _opcVault.GetCertificateGroupsConfigurationAsync();
            return View(requests.Groups);
        }

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id)
        {
            AuthorizeClient();
            var request = await _opcVault.GetCertificateGroupConfigurationAsync(id);
            return View(request);
        }

        [ActionName("Renew")]
        public async Task<ActionResult> Renew(string id)
        {
            AuthorizeClient();
            var request = await _opcVault.CreateCertificateGroupIssuerCACertAsync(id);
            return RedirectToAction("IssuerDetails", new { id });
        }

        [ActionName("Revoke")]
        public async Task<ActionResult> Revoke(string id)
        {
            AuthorizeClient();
            await _opcVault.RevokeGroupAsync(id);
            return RedirectToAction("IssuerDetails", new { id });
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync(
            CertificateGroupConfigurationApiModel newGroup)
        {
            if (ModelState.IsValid)
            {
                AuthorizeClient();
                var group = await _opcVault.GetCertificateGroupConfigurationAsync(newGroup.Name);
                if (group == null)
                {
                    return new NotFoundResult();
                }

                // at this point only allow lifetime and Subject update
                group.SubjectName = newGroup.SubjectName;
                group.DefaultCertificateLifetime = newGroup.DefaultCertificateLifetime;
                //group.DefaultCertificateKeySize = newGroup.DefaultCertificateKeySize;
                //group.DefaultCertificateHashSize = newGroup.DefaultCertificateHashSize;
                group.IssuerCACertificateLifetime = newGroup.IssuerCACertificateLifetime;
                //group.IssuerCACertificateKeySize = newGroup.IssuerCACertificateKeySize;
                //group.IssuerCACertificateHashSize = newGroup.IssuerCACertificateHashSize;
                try
                {
                    await _opcVault.UpdateCertificateGroupConfigurationAsync(group.Name, group).ConfigureAwait(false);
                }
                catch (HttpOperationException)
                {
                    return View(newGroup);
                }

                return RedirectToAction("Index");
            }

            return View(newGroup);
        }

        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                return new BadRequestResult();
            }
            AuthorizeClient();
            var group = await _opcVault.GetCertificateGroupConfigurationAsync(id);
            if (group == null)
            {
                return new NotFoundResult();
            }

            return View(group);
        }

        [ActionName("IssuerDetails")]
        public async Task<ActionResult> IssuerDetailsAsync(string id)
        {
            AuthorizeClient();
            var issuer = await _opcVault.GetCertificateGroupIssuerCAChainAsync(id);
            var certList = new List<CertificateDetailsApiModel>();
            foreach (var certificate in issuer.Chain)
            {
                var byteArray = Convert.FromBase64String(certificate.Certificate);
                X509Certificate2 cert = new X509Certificate2(byteArray);
                var model = new CertificateDetailsApiModel()
                {
                    Subject = cert.Subject,
                    Issuer = cert.Issuer,
                    Thumbprint = cert.Thumbprint,
                    SerialNumber = cert.SerialNumber,
                    NotBefore = cert.NotBefore,
                    NotAfter = cert.NotAfter
                };
                certList.Add(model);
            }
            var modelCollection = new CertificateDetailsCollectionApiModel(id)
            {
                Certificates = certList.ToArray()
            };
            return View(modelCollection);
        }


        [ActionName("DownloadIssuer")]
        public async Task<ActionResult> DownloadIssuerAsync(string id)
        {
            AuthorizeClient();
            var issuer = await _opcVault.GetCertificateGroupIssuerCAChainAsync(id);
            var byteArray = Convert.FromBase64String(issuer.Chain[0].Certificate);
            return new FileContentResult(byteArray, ContentType.Cert)
            {
                FileDownloadName = Utils.Utils.CertFileName(issuer.Chain[0].Certificate) + ".der"
            };
        }

        [ActionName("DownloadIssuerCrl")]
        public async Task<ActionResult> DownloadIssuerCrlAsync(string id)
        {
            AuthorizeClient();
            var issuer = await _opcVault.GetCertificateGroupIssuerCAChainAsync(id);
            var crl = await _opcVault.GetCertificateGroupIssuerCACrlChainAsync(id);
            var byteArray = Convert.FromBase64String(crl.Chain[0].Crl);
            return new FileContentResult(byteArray, ContentType.Crl)
            {
                FileDownloadName = Utils.Utils.CertFileName(issuer.Chain[0].Certificate) + ".crl"
            };
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

    }
}
