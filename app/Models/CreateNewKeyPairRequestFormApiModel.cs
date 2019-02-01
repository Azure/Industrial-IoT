// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
using Opc.Ua.Gds.Client;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models
{
    /// <summary>
    /// helper for model validation in new keypair request form
    /// </summary>
    public class CreateNewKeyPairRequestFormApiModelAttribute : ValidationAttribute, IClientModelValidator
    {
        ServerCapabilities _serverCaps = new ServerCapabilities();
        const int ApplicationTypeClient = 1;

        public CreateNewKeyPairRequestFormApiModelAttribute()
        {
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            throw new NotImplementedException();
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            CreateNewKeyPairRequestApiModel request = (CreateNewKeyPairRequestApiModel)validationContext.ObjectInstance;
            var errorList = new List<string>();

            if (String.IsNullOrWhiteSpace(request.SubjectName)) { errorList.Add(nameof(request.SubjectName)); }
            if (request.DomainNames != null)
            {
                if (request.DomainNames.Count > 0)
                {
                    if (String.IsNullOrWhiteSpace(request.DomainNames[0]))
                    {
                        errorList.Add("DomainNames[0]");
                    }
                }
            }
            if (String.IsNullOrWhiteSpace(request.PrivateKeyFormat)) { errorList.Add(nameof(request.PrivateKeyFormat)); }
            if (errorList.Count > 0) { return new ValidationResult("Required Field.", errorList); }

            try
            {
                var dn = Opc.Ua.Utils.ParseDistinguishedName(request.SubjectName);
                var prefix = dn.Where(x => x.StartsWith("CN=", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (prefix == null)
                {
                    errorList.Add(nameof(request.SubjectName));
                    return new ValidationResult("Need at least a common name CN=", errorList);
                }
            }
            catch
            {
                errorList.Add(nameof(request.SubjectName));
            }
            if (errorList.Count > 0) { return new ValidationResult("Not a well formed Certificate Subject.", errorList); }

            return ValidationResult.Success;
        }
    }


    [CreateNewKeyPairRequestFormApiModel]
    public class CreateNewKeyPairRequestFormApiModel : CreateNewKeyPairRequestApiModel
    {
        public CreateNewKeyPairRequestFormApiModel() : base()
        { }

        public CreateNewKeyPairRequestFormApiModel(CreateNewKeyPairRequestApiModel apiModel) :
            base()
        {
            ApplicationId = apiModel.ApplicationId;
            CertificateGroupId = apiModel.CertificateGroupId;
            CertificateTypeId = apiModel.CertificateTypeId;
            SubjectName = apiModel.SubjectName;
            DomainNames = apiModel.DomainNames;
            PrivateKeyFormat = apiModel.PrivateKeyFormat;
            PrivateKeyPassword = apiModel.PrivateKeyPassword;
        }
    }
}
