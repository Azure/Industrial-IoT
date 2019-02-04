// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.TokenStorage;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Utils;
using Microsoft.Rest;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Controllers
{
    [Authorize]
    public class DownloadController : Controller
    {
        protected IOpcVault opcVault;
        private readonly OpcVaultApiOptions _opcVaultOptions;
        private readonly AzureADOptions _azureADOptions;
        private readonly ITokenCacheService _tokenCacheService;

        public DownloadController(
            OpcVaultApiOptions opcVaultOptions,
            AzureADOptions azureADOptions,
            ITokenCacheService tokenCacheService)
        {
            _opcVaultOptions = opcVaultOptions;
            _azureADOptions = azureADOptions;
            _tokenCacheService = tokenCacheService;
        }

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id, string message)
        {
            AuthorizeClient();
            var request = await opcVault.GetCertificateRequestAsync(id);
            ViewData["Message"] = message;

            var application = await opcVault.GetApplicationAsync(request.ApplicationId);
            if (application == null)
            {
                return new NotFoundResult();
            }

            ViewData["Application"] = application;

            return View(request);
        }

        [ActionName("Approve")]
        public async Task<ActionResult> ApproveAsync(string id)
        {
            AuthorizeClient();
            try
            {
                await opcVault.ApproveCertificateRequestAsync(id, false);
                return RedirectToAction("Details", new { id, message = "CertificateRequest approved!" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Details", new { id, message = ex.Message });
            }
        }

        [ActionName("Reject")]
        public async Task<ActionResult> RejectAsync(string id)
        {
            AuthorizeClient();
            try
            {
                await opcVault.ApproveCertificateRequestAsync(id, true);
                return RedirectToAction("Details", new { id, message = "CertificateRequest rejected!" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Details", new { id, message = ex.Message });
            }
        }

        [ActionName("Accept")]
        public async Task<ActionResult> AcceptAsync(string id)
        {
            AuthorizeClient();
            await opcVault.AcceptCertificateRequestAsync(id);
            return RedirectToAction("Details", new { id });
        }

        [ActionName("DownloadCertificate")]
        public async Task<ActionResult> DownloadCertificateAsync(string requestId, string applicationId)
        {
            AuthorizeClient();
            var result = await opcVault.FetchCertificateRequestResultAsync(requestId, applicationId);
            if ((result.State == Api.Vault.Models.CertificateRequestState.Approved ||
                result.State == Api.Vault.Models.CertificateRequestState.Accepted) &&
                result.SignedCertificate != null)
            {
                var byteArray = Convert.FromBase64String(result.SignedCertificate);
                return new FileContentResult(byteArray, ContentType.Cert)
                {
                    FileDownloadName = Utils.Utils.CertFileName(result.SignedCertificate) + ".der"
                };
            }
            return new NotFoundResult();
        }

        [ActionName("DownloadCertificateBase64")]
        public async Task<ActionResult> DownloadCertificateBase64Async(string requestId, string applicationId)
        {
            AuthorizeClient();
            var result = await opcVault.FetchCertificateRequestResultAsync(requestId, applicationId);
            if ((result.State == Api.Vault.Models.CertificateRequestState.Approved ||
                result.State == Api.Vault.Models.CertificateRequestState.Accepted) &&
                result.SignedCertificate != null)
            {
                return RedirectToAction("DownloadCertBase64", new { cert = result.SignedCertificate });
            }
            return new NotFoundResult();
        }

        [ActionName("DownloadIssuer")]
        public async Task<ActionResult> DownloadIssuerAsync(string requestId)
        {
            AuthorizeClient();
            var request = await opcVault.GetCertificateRequestAsync(requestId);
            if (request != null)
            {
                var issuer = await opcVault.GetCertificateGroupIssuerCAChainAsync(request.CertificateGroupId);
                var byteArray = Convert.FromBase64String(issuer.Chain[0].Certificate);
                return new FileContentResult(byteArray, ContentType.Cert)
                {
                    FileDownloadName = Utils.Utils.CertFileName(issuer.Chain[0].Certificate) + ".der"
                };
            }
            return new NotFoundResult();
        }

        [ActionName("DownloadIssuerCrl")]
        public async Task<ActionResult> DownloadIssuerCrlAsync(string requestId)
        {
            AuthorizeClient();
            var request = await opcVault.GetCertificateRequestAsync(requestId);
            if (request != null)
            {
                var issuer = await opcVault.GetCertificateGroupIssuerCAChainAsync(request.CertificateGroupId);
                var crl = await opcVault.GetCertificateGroupIssuerCACrlChainAsync(request.CertificateGroupId);
                var byteArray = Convert.FromBase64String(crl.Chain[0].Crl);
                return new FileContentResult(byteArray, ContentType.Crl)
                {
                    FileDownloadName = Utils.Utils.CertFileName(issuer.Chain[0].Certificate) + ".crl"
                };
            }
            return new NotFoundResult();
        }

        [ActionName("DownloadIssuerBase64")]
        public async Task<ActionResult> DownloadIssuerBase64Async(string groupId, string requestId)
        {
            AuthorizeClient();
            if (groupId == null)
            {
                var request = await opcVault.GetCertificateRequestAsync(requestId);
                if (request != null)
                {
                    groupId = request.CertificateGroupId;
                }
            }

            if (groupId != null)
            {
                var issuer = await opcVault.GetCertificateGroupIssuerCAChainAsync(groupId);
                return RedirectToAction("DownloadCertBase64", new { cert = issuer.Chain[0].Certificate });
            }

            return new NotFoundResult();
        }

        [ActionName("DownloadIssuerCrlBase64")]
        public async Task<ActionResult> DownloadIssuerCrlBase64Async(string groupId, string requestId)
        {
            AuthorizeClient();
            if (groupId == null)
            {
                var request = await opcVault.GetCertificateRequestAsync(requestId);
                if (request != null)
                {
                    groupId = request.CertificateGroupId;
                }
            }

            if (groupId != null)
            {
                var crl = await opcVault.GetCertificateGroupIssuerCACrlChainAsync(groupId);
                return RedirectToAction("DownloadCrlBase64", new { crl = crl.Chain[0].Crl });
            }

            return new NotFoundResult();
        }

        [ActionName("DownloadPrivateKey")]
        public async Task<ActionResult> DownloadPrivateKeyAsync(string requestId, string applicationId)
        {
            AuthorizeClient();
            var result = await opcVault.FetchCertificateRequestResultAsync(requestId, applicationId);
            if (result.State == Api.Vault.Models.CertificateRequestState.Approved &&
                result.PrivateKey != null)
            {
                if (String.Compare(result.PrivateKeyFormat, "PFX", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var byteArray = Convert.FromBase64String(result.PrivateKey);
                    return new FileContentResult(byteArray, ContentType.Pfx)
                    {
                        FileDownloadName = Utils.Utils.CertFileName(result.SignedCertificate) + ".pfx"
                    };
                }
                else if (String.Compare(result.PrivateKeyFormat, "PEM", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var byteArray = Convert.FromBase64String(result.PrivateKey);
                    return new FileContentResult(byteArray, ContentType.Pem)
                    {
                        FileDownloadName = Utils.Utils.CertFileName(result.SignedCertificate) + ".pem"
                    };
                }
            }
            return new NotFoundResult();
        }

        [ActionName("DownloadKeyBase64")]
        public async Task<ActionResult> DownloadPrivateKeyBase64Async(string requestId, string applicationId)
        {
            AuthorizeClient();
            var result = await opcVault.FetchCertificateRequestResultAsync(requestId, applicationId);
            if (result.State == Api.Vault.Models.CertificateRequestState.Approved &&
                result.PrivateKey != null)
            {
                var model = new KeyDetailsApiModel();
                if (String.Compare(result.PrivateKeyFormat, "PFX", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    model.EncodedBase64 = result.PrivateKey;
                    return View(model);
                }
                else if (String.Compare(result.PrivateKeyFormat, "PEM", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // if (false)
                    // {
                    //     //to display PEM as text
                    //     var byteArray = Convert.FromBase64String(result.PrivateKey);
                    //     model.EncodedBase64 = System.Text.Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                    // }
                    // else
                    {
                        model.EncodedBase64 = result.PrivateKey;
                    }
                    return View(model);
                }
            }
            return new NotFoundResult();
        }

        [ActionName("DownloadCertBase64")]
        public ActionResult DownloadCertBase64(string cert)
        {
            var byteArray = Convert.FromBase64String(cert);
            X509Certificate2 certificate = new X509Certificate2(byteArray);
            var model = new CertificateDetailsApiModel()
            {
                Subject = certificate.Subject,
                Issuer = certificate.Issuer,
                Thumbprint = certificate.Thumbprint,
                SerialNumber = certificate.SerialNumber,
                NotBefore = certificate.NotBefore,
                NotAfter = certificate.NotAfter,
                EncodedBase64 = cert
            };
            return View(model);
        }

        [ActionName("DownloadCrlBase64")]
        public ActionResult DownloadCrlBase64(string crl)
        {
            var byteArray = Convert.FromBase64String(crl);
            var crlObject = new Opc.Ua.X509CRL(byteArray);
            var model = new CrlDetailsApiModel()
            {
                UpdateTime = crlObject.UpdateTime,
                NextUpdateTime = crlObject.NextUpdateTime,
                Issuer = crlObject.Issuer,
                EncodedBase64 = crl
            };
            return View(model);
        }

        protected void AuthorizeClient()
        {
            if (opcVault == null)
            {
                ServiceClientCredentials serviceClientCredentials =
                    new OpcVaultLoginCredentials(_opcVaultOptions, _azureADOptions, _tokenCacheService, User);
                opcVault = new OpcVault(new Uri(_opcVaultOptions.BaseAddress), serviceClientCredentials);
            }
        }

    }
}


