// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
    using Newtonsoft.Json;
    using Opc.Ua;

    [CreateSigningRequestUploadApiModel]
    public partial class CreateSigningRequestUploadApiModel
    {
        /// <summary>
        /// Initializes a new instance of the StartSigningRequestUploadModel
        /// class.
        /// </summary>
        public CreateSigningRequestUploadApiModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the StartSigningRequestUploadModel
        /// class.
        /// </summary>
        public CreateSigningRequestUploadApiModel(CreateSigningRequestApiModel apiModel)
        {
            ApiModel = apiModel;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        [JsonProperty(PropertyName = "ApiModel")]
        public CreateSigningRequestApiModel ApiModel { get; set; }

        [JsonProperty(PropertyName = "ApplicationUri")]
        public string ApplicationUri { get; set; }

        [JsonProperty(PropertyName = "ApplicationName")]
        public string ApplicationName { get; set; }

        [JsonProperty(PropertyName = "CertificateRequestFile")]
        public IFormFile CertificateRequestFile { get; set; }

    }

    /// <summary>
    /// helper for model validation in signing request form
    /// </summary>
    public class CreateSigningRequestUploadApiModelAttribute : ValidationAttribute, IClientModelValidator
    {
        public CreateSigningRequestUploadApiModelAttribute()
        {
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            throw new NotImplementedException();
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            CreateSigningRequestUploadApiModel request = (CreateSigningRequestUploadApiModel)validationContext.ObjectInstance;
            var errorList = new List<string>();

            if (request.CertificateRequestFile == null &&
                String.IsNullOrWhiteSpace(request.ApiModel.CertificateRequest))
            {
                errorList.Add(nameof(request.CertificateRequestFile));
                errorList.Add("ApiModel.CertificateRequest");
                return new ValidationResult("At least one CSR field is required.", errorList);
            }

            if (request.CertificateRequestFile != null &&
                !String.IsNullOrWhiteSpace(request.ApiModel.CertificateRequest))
            {
                errorList.Add(nameof(request.CertificateRequestFile));
                errorList.Add("ApiModel.CertificateRequest");
                return new ValidationResult("Only one CSR field is required.", errorList);
            }

            byte[] certificateRequest = null;

            if (request.ApiModel.CertificateRequest != null)
            {
                errorList.Add("ApiModel.CertificateRequest");
                try
                {
                    certificateRequest = Convert.FromBase64String(request.ApiModel.CertificateRequest);
                }
                catch
                {
                    return new ValidationResult("Cannot decode base64 encoded CSR.", errorList);
                }
            }

            if (request.CertificateRequestFile != null)
            {
                errorList.Add(nameof(request.CertificateRequestFile));
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        request.CertificateRequestFile.CopyToAsync(memoryStream).Wait();
                        certificateRequest = memoryStream.ToArray();
                    }
                }
                catch
                {
                    errorList.Add(nameof(request.CertificateRequestFile));
                    return new ValidationResult("Invalid CSR file.", errorList);
                }
            }

            if (certificateRequest != null)
            {
                try
                {
                    var pkcs10CertificationRequest = new Org.BouncyCastle.Pkcs.Pkcs10CertificationRequest(certificateRequest);
                    if (!pkcs10CertificationRequest.Verify())
                    {
                        return new ValidationResult("CSR signature invalid.", errorList);
                    }

                    var info = pkcs10CertificationRequest.GetCertificationRequestInfo();
                    var altNameExtension = GetAltNameExtensionFromCSRInfo(info);
                    if (altNameExtension != null &&
                        altNameExtension.Uris.Count > 0)
                    {
                        if (!altNameExtension.Uris.Contains(request.ApplicationUri))
                        {
                            return new ValidationResult(altNameExtension.Uris[0] + " doesn't match the ApplicationUri.", errorList);
                        }
                    }
                    else
                    {
                        return new ValidationResult("The CSR does not contain a valid Application Uri.", errorList);
                    }
                }
                catch
                {
                    return new ValidationResult("CSR decoding failed. Invalid data?", errorList);
                }
            }

            return ValidationResult.Success;
        }

        private X509SubjectAltNameExtension GetAltNameExtensionFromCSRInfo(Org.BouncyCastle.Asn1.Pkcs.CertificationRequestInfo info)
        {
            for (int i = 0; i < info.Attributes.Count; i++)
            {
                var sequence = Org.BouncyCastle.Asn1.Asn1Sequence.GetInstance(info.Attributes[i].ToAsn1Object());
                var oid = Org.BouncyCastle.Asn1.DerObjectIdentifier.GetInstance(sequence[0].ToAsn1Object());
                if (oid.Equals(Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.Pkcs9AtExtensionRequest))
                {
                    var extensionInstance = Org.BouncyCastle.Asn1.Asn1Set.GetInstance(sequence[1]);
                    var extensionSequence = Org.BouncyCastle.Asn1.Asn1Sequence.GetInstance(extensionInstance[0]);
                    var extensions = Org.BouncyCastle.Asn1.X509.X509Extensions.GetInstance(extensionSequence);
                    Org.BouncyCastle.Asn1.X509.X509Extension extension = extensions.GetExtension(Org.BouncyCastle.Asn1.X509.X509Extensions.SubjectAlternativeName);
                    var asnEncodedAltNameExtension = new System.Security.Cryptography.AsnEncodedData(Org.BouncyCastle.Asn1.X509.X509Extensions.SubjectAlternativeName.ToString(), extension.Value.GetOctets());
                    var altNameExtension = new X509SubjectAltNameExtension(asnEncodedAltNameExtension, extension.IsCritical);
                    return altNameExtension;
                }
            }
            return null;
        }

    }
}
