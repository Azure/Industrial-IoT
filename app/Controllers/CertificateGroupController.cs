// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.TokenStorage;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Utils;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Controllers
{
    [Authorize]
    public class CertificateGroupController : Controller
    {
        private IOpcVault opcVault;
        private readonly OpcVaultApiOptions opcVaultOptions;
        private readonly AzureADOptions azureADOptions;
        private readonly ITokenCacheService tokenCacheService;

        public CertificateGroupController(
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
            var requests = await opcVault.GetCertificateGroupConfigurationCollectionAsync();
            return View(requests.Groups);
        }

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id)
        {
            AuthorizeClient();
            var request = await opcVault.GetCertificateGroupConfigurationAsync(id);
            return View(request);
        }

        [ActionName("Renew")]
        public async Task<ActionResult> Renew(string id)
        {
            AuthorizeClient();
            var request = await opcVault.CreateCACertificateAsync(id);
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
                var group = await opcVault.GetCertificateGroupConfigurationAsync(newGroup.Name);
                if (group == null)
                {
                    return new NotFoundResult();
                }

                // at this point only allow lifetime and Subject update
                group.SubjectName = newGroup.SubjectName;
                group.DefaultCertificateLifetime = newGroup.DefaultCertificateLifetime;
                //group.DefaultCertificateKeySize = newGroup.DefaultCertificateKeySize;
                //group.DefaultCertificateHashSize = newGroup.DefaultCertificateHashSize;
                group.CACertificateLifetime = newGroup.CACertificateLifetime;
                //group.CACertificateKeySize = newGroup.CACertificateKeySize;
                //group.CACertificateHashSize = newGroup.CACertificateHashSize;
                try
                {
                    await opcVault.UpdateCertificateGroupConfigurationAsync(group.Name, group).ConfigureAwait(false);
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
            var group = await opcVault.GetCertificateGroupConfigurationAsync(id);
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
            var issuer = await opcVault.GetCACertificateChainAsync(id);
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
            var modelCollection = new CertificateDetailsCollectionApiModel(id);
            modelCollection.Certificates = certList.ToArray();
            return View(modelCollection);
        }


        [ActionName("DownloadIssuer")]
        public async Task<ActionResult> DownloadIssuerAsync(string id)
        {
            AuthorizeClient();
            var issuer = await opcVault.GetCACertificateChainAsync(id);
            var byteArray = Convert.FromBase64String(issuer.Chain[0].Certificate);
            return new FileContentResult(byteArray, ContentType.Cert)
            {
                FileDownloadName = CertFileName(issuer.Chain[0].Certificate) + ".der"
            };
        }

        [ActionName("DownloadIssuerCrl")]
        public async Task<ActionResult> DownloadIssuerCrlAsync(string id)
        {
            AuthorizeClient();
            var issuer = await opcVault.GetCACertificateChainAsync(id);
            var crl = await opcVault.GetCACrlChainAsync(id);
            var byteArray = Convert.FromBase64String(crl.Chain[0].Crl);
            return new FileContentResult(byteArray, ContentType.Crl)
            {
                FileDownloadName = CertFileName(issuer.Chain[0].Certificate) + ".crl"
            };
        }

        private string CertFileName(string signedCertificate)
        {
            try
            {
                var signedCertByteArray = Convert.FromBase64String(signedCertificate);
                X509Certificate2 cert = new X509Certificate2(signedCertByteArray);
                return cert.Subject + "[" + cert.Thumbprint + "]";
            }
            catch
            {
                return "Certificate";
            }
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

    }
}
