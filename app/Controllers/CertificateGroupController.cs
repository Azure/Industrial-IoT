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
    /// <summary>
    /// The certificate group controller.
    /// </summary>
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

        /// <summary>
        /// List all certificate groups.
        /// </summary>
        [ActionName("Index")]
        public async Task<ActionResult> IndexAsync(string error)
        {
            AuthorizeClient();
            try
            {
                var requests = await _opcVault.GetCertificateGroupsConfigurationAsync();
                ViewData["ErrorMessage"] = error;
                return View(requests.Groups);
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] =
                    "Failed to load the certificate groups. " +
                    "Message:" + ex.Message;
                return View(new CertificateGroupConfigurationApiModel[0]);
            }
        }

        /// <summary>
        /// Show the details.
        /// </summary>
        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id)
        {
            AuthorizeClient();
            try
            {
                var request = await _opcVault.GetCertificateGroupConfigurationAsync(id);
                return View(request);
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] =
                    "Failed to load the certificate group " + id + ". " +
                    "Message:" + ex.Message;
                return View(new CertificateGroupConfigurationApiModel());
            }
        }

        [ActionName("Renew")]
        public async Task<ActionResult> Renew(string id)
        {
            AuthorizeClient();
            try
            {
                var request = await _opcVault.CreateCertificateGroupIssuerCACertAsync(id);
                return RedirectToAction("IssuerDetails", new { id });
            }
            catch (Exception ex)
            {
                string error =
                    "Failed to renew the Issuer CA certificate. " +
                    "Message:" + ex.Message;
                return RedirectToAction("IssuerDetails", new { id, error });
            }

        }

        /// <summary>
        /// Revoke all certificates for a group.
        /// </summary>
        [ActionName("Revoke")]
        public async Task<ActionResult> Revoke(string id)
        {
            AuthorizeClient();
            try
            {
                await _opcVault.RevokeCertificateGroupAsync(id);
                return RedirectToAction("IssuerDetails", new { id });
            }
            catch (Exception ex)
            {
                string error =
                    "Failed to revoke the certificage group " + id + "." +
                    "Message:" + ex.Message;
                return RedirectToAction("IssuerDetails", new { id, error });
            }
        }

        /// <summary>
        /// Update a certificate group.
        /// </summary>
        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync(
            CertificateGroupConfigurationApiModel newGroup)
        {
            if (ModelState.IsValid)
            {
                AuthorizeClient();
                CertificateGroupConfigurationApiModel group;
                try
                {
                    group = await _opcVault.GetCertificateGroupConfigurationAsync(newGroup.Name);
                }
                catch (Exception ex)
                {
                    var error =
                        "Failed to load the certificate group configuration " + newGroup.Name + ". " +
                        "Message:" + ex.Message;
                    return RedirectToAction("Index", new { error });
                }

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
                //group.IssuerCACRLDistributionPoint = newGroup.IssuerCACRLDistributionPoint;
                //group.IssuerCAAuthorityInformationAccess = newGroup.IssuerCAAuthorityInformationAccess;
                try
                {
                    await _opcVault.UpdateCertificateGroupConfigurationAsync(group.Name, group).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var error =
                        "Failed to update the certificate group configuration " + group.Name + ". " +
                        "Message:" + ex.Message;
                    return RedirectToAction("Index", new { error });
                }

                return RedirectToAction("Index");
            }

            return View(newGroup);
        }

        /// <summary>
        /// The edit form for a certificate group.
        /// </summary>
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

        /// <summary>
        /// The Issuer Details.
        /// </summary>
        [ActionName("IssuerDetails")]
        public async Task<ActionResult> IssuerDetailsAsync(string id, string error)
        {
            AuthorizeClient();
            try
            {
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
                ViewData["ErrorMessage"] = error;
                return View(modelCollection);
            }
            catch (Exception ex)
            {
                var exError =
                    "Failed to load the Issuer CA Chain " + id + ". " +
                    "Message:" + ex.Message;
                return RedirectToAction("Index", new { error = exError });
            }
        }


        [ActionName("DownloadIssuer")]
        public async Task<ActionResult> DownloadIssuerAsync(string id)
        {
            AuthorizeClient();
            try
            {
                var issuer = await _opcVault.GetCertificateGroupIssuerCAChainAsync(id);
                var byteArray = Convert.FromBase64String(issuer.Chain[0].Certificate);
                return new FileContentResult(byteArray, ContentType.Cert)
                {
                    FileDownloadName = Utils.Utils.CertFileName(issuer.Chain[0].Certificate) + ".der"
                };
            }
            catch (Exception ex)
            {
                var error =
                    "Failed to load the Issuer CA certificate. " +
                    "Message:" + ex.Message;
                return RedirectToAction("Index", new { error });
            }
        }

        [ActionName("DownloadIssuerCrl")]
        public async Task<ActionResult> DownloadIssuerCrlAsync(string id)
        {
            AuthorizeClient();
            try
            {
                var issuer = await _opcVault.GetCertificateGroupIssuerCAChainAsync(id);
                var crl = await _opcVault.GetCertificateGroupIssuerCACrlChainAsync(id);
                var byteArray = Convert.FromBase64String(crl.Chain[0].Crl);
                return new FileContentResult(byteArray, ContentType.Crl)
                {
                    FileDownloadName = Utils.Utils.CertFileName(issuer.Chain[0].Certificate) + ".crl"
                };
            }
            catch (Exception ex)
            {
                var error =
                    "Failed to load the Issuer CRL. " +
                    "Message:" + ex.Message;
                return RedirectToAction("Index", new { error });
            }
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
