// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Filters;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Controllers
{
    /// <summary>
    /// Certificate CRL Distribution Point and Authority Information Access services.
    /// </summary>
    [ApiController]
    [Route("/certs"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    public sealed class CertificateController : Controller
    {
        private readonly ICertificateGroup _certificateGroups;

        /// <summary>
        /// Create the controller.
        /// </summary>
        /// <param name="certificateGroups"></param>
        public CertificateController(
            ICertificateGroup certificateGroups)
        {
            _certificateGroups = certificateGroups;
        }

        /// <summary>
        /// Get Issuer Certificate for Authority Information Access endpoint.
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="cert"></param>
        /// <returns>The Issuer Ca cert as a file</returns>
        [HttpGet("issuer/{serial}/{cert}")]
        [Produces(ContentType.Cert)]
        public async Task<ActionResult> GetIssuerCertAsync(string serial, string cert)
        {
            try
            {
                serial = serial.ToLower();
                cert = cert.ToLower();
                if (cert.EndsWith(".cer"))
                {
                    string groupId = cert.Substring(0, cert.Length - 4);
                    // find isser cert with serial no.

                    X509Certificate2Collection certVersions;
                    string nextPageLink;
                    (certVersions, nextPageLink)= await _certificateGroups.GetIssuerCACertificateVersionsAsync(groupId, false);
                    while (certVersions != null && certVersions.Count > 0)
                    {
                        foreach (var certVersion in certVersions)
                        {
                            if (serial.Equals(certVersion.SerialNumber, StringComparison.OrdinalIgnoreCase))
                            {
                                var byteArray = certVersion.RawData;
                                return new FileContentResult(byteArray, ContentType.Cert)
                                {
                                    FileDownloadName = Utils.DownloadName(certVersion, groupId) + ".cer"
                                };
                            }
                        }
                        if (nextPageLink != null)
                        {
                            (certVersions, nextPageLink) = await _certificateGroups.GetIssuerCACertificateVersionsAsync(groupId, false, nextPageLink);
                        }
                        else
                        {
                            certVersions = null;
                        }
                    }
                }
            }
            catch
            {
                await Task.Delay(1000);
            }
            return new NotFoundResult();
        }

        /// <summary>
        /// Get Issuer CRL in CRL Distribution Endpoint.
        /// </summary>
        [HttpGet("crl/{serial}/{crl}")]
        [Produces(ContentType.Crl)]
        public async Task<ActionResult> GetIssuerCrlAsync(string serial, string crl)
        {
            try
            {
                serial = serial.ToLower();
                crl = crl.ToLower();
                if (crl.EndsWith(".crl"))
                {
                    string groupId = crl.Substring(0, crl.Length - 4);
                    // find isser cert with serial no.
                    X509Certificate2Collection certVersions;
                    string nextPageLink;
                    (certVersions, nextPageLink) = await _certificateGroups.GetIssuerCACertificateVersionsAsync(groupId, false);
                    while (certVersions != null && certVersions.Count > 0)
                    {
                        foreach (var cert in certVersions)
                        {
                            if (serial.Equals(cert.SerialNumber, StringComparison.OrdinalIgnoreCase))
                            {
                                var thumbPrint = cert.Thumbprint;
                                var crlBinary = await _certificateGroups.GetIssuerCACrlChainAsync(groupId, thumbPrint);
                                var byteArray = crlBinary[0].RawData;
                                return new FileContentResult(byteArray, ContentType.Crl)
                                {
                                    FileDownloadName = Utils.DownloadName(cert, groupId) + ".crl"
                                };
                            }
                        }
                        if (nextPageLink != null)
                        {
                            (certVersions, nextPageLink) = await _certificateGroups.GetIssuerCACertificateVersionsAsync(groupId, false, nextPageLink);
                        }
                        else
                        {
                            certVersions = null;
                        }
                    }
                }
            }
            catch
            {
                await Task.Delay(1000);
            }
            return new NotFoundResult();
        }

    }

    public class ContentType
    {

        public const string Cert = "application/pkix-cert";
        public const string Crl = "application/pkix-crl";
        // see CertificateContentType.Pfx
        public const string Pfx = "application/x-pkcs12";
        // see CertificateContentType.Pem
        public const string Pem = "application/x-pem-file";
    }

    public class Utils
    {
        public static string DownloadName(X509Certificate2 cert, string name)
        {
            try
            {
                var dn = Opc.Ua.Utils.ParseDistinguishedName(cert.Subject);
                var prefix = dn.Where(x => x.StartsWith("CN=", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Substring(3);
                return prefix + " [" + cert.Thumbprint + "]";
            }
            catch
            {
                return name;
            }
        }

    }

}
