// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
using Opc.Ua.Gds.Client;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models
{
    /// <summary>
    /// helper for model validation in registration form
    /// </summary>
    public class ApplicationRecordRegisterApiModelAttribute : ValidationAttribute, IClientModelValidator
    {
        ServerCapabilities _serverCaps = new ServerCapabilities();
        const int ApplicationTypeClient = 1;

        public ApplicationRecordRegisterApiModelAttribute()
        {
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            throw new NotImplementedException();
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ApplicationRecordRegisterApiModel application = (ApplicationRecordRegisterApiModel)validationContext.ObjectInstance;
            var errorList = new List<string>();

            if (String.IsNullOrWhiteSpace(application.ApplicationUri)) { errorList.Add(nameof(application.ApplicationUri)); }
            if (String.IsNullOrWhiteSpace(application.ProductUri)) { errorList.Add(nameof(application.ProductUri)); }
            if (String.IsNullOrWhiteSpace(application.ApplicationName)) { errorList.Add(nameof(application.ApplicationName)); }
            if (application.ApplicationType != ApplicationType.Client)
            {
                if (String.IsNullOrWhiteSpace(application.ServerCapabilities)) { errorList.Add(nameof(application.ServerCapabilities)); }
                if (application.DiscoveryUrls != null)
                {
                    for (int i = 0; i < application.DiscoveryUrls.Count; i++)
                    {
                        if (String.IsNullOrWhiteSpace(application.DiscoveryUrls[i])) { errorList.Add($"DiscoveryUrls[{i}]"); }
                    }
                }
                else
                {
                    errorList.Add($"DiscoveryUrls[0]");
                }
            }
            if (errorList.Count > 0) { return new ValidationResult("Required Field.", errorList); }

            /* entries will be ignored on register
            if (application.ApplicationType == ApplicationTypeClient)
            {
                if (!String.IsNullOrWhiteSpace(application.ServerCapabilities)) { errorList.Add(nameof(application.ServerCapabilities)); }
                for (int i = 0; i < application.DiscoveryUrls.Count; i++)
                {
                    if (!String.IsNullOrWhiteSpace(application.DiscoveryUrls[i])) { errorList.Add($"DiscoveryUrls[{i}]"); }
                }
                if (errorList.Count > 0) { return new ValidationResult("Invalid entry for client.", errorList); }
            }
            */

            if (!Uri.IsWellFormedUriString(application.ApplicationUri, UriKind.Absolute)) { errorList.Add("ApplicationUri"); }
            if (!Uri.IsWellFormedUriString(application.ProductUri, UriKind.Absolute)) { errorList.Add("ProductUri"); }
            if (application.ApplicationType != ApplicationType.Client)
            {
                for (int i = 0; i < application.DiscoveryUrls.Count; i++)
                {
                    if (!Uri.IsWellFormedUriString(application.DiscoveryUrls[i], UriKind.Absolute)) { errorList.Add($"DiscoveryUrls[{i}]"); continue; }
                    Uri uri = new Uri(application.DiscoveryUrls[i], UriKind.Absolute);
                    if (String.IsNullOrEmpty(uri.Host)) { errorList.Add($"DiscoveryUrls[{i}]"); continue; }
                    if (uri.HostNameType == UriHostNameType.Unknown) { errorList.Add($"DiscoveryUrls[{i}]"); continue; }
                }
            }
            if (errorList.Count > 0) { return new ValidationResult("Not a well formed Uri.", errorList); }

            if (application.ApplicationType != ApplicationType.Client &&
                !String.IsNullOrEmpty(application.ServerCapabilities))
            {
                string[] serverCapModelArray = application.ServerCapabilities.Split(',');
                foreach (var cap in serverCapModelArray)
                {
                    ServerCapability serverCap = _serverCaps.Find(cap);
                    if (serverCap == null)
                    {
                        errorList.Add(nameof(application.ServerCapabilities));
                        return new ValidationResult(cap + " is not a valid ServerCapability.", errorList);
                    }
                }
            }

            return ValidationResult.Success;
        }
    }


    [ApplicationRecordRegisterApiModel]
    public class ApplicationRecordRegisterApiModel : ApplicationRecordApiModel
    {
        public ApplicationRecordRegisterApiModel() : base()
        { }

        public ApplicationRecordRegisterApiModel(ApplicationRecordApiModel apiModel) :
            base(ApplicationState.New, apiModel.ApplicationType, apiModel.ApplicationId, apiModel.Id)
        {
            ApplicationUri = apiModel.ApplicationUri;
            ApplicationName = apiModel.ApplicationName;
            ApplicationNames = apiModel.ApplicationNames;
            ProductUri = apiModel.ProductUri;
            DiscoveryUrls = apiModel.DiscoveryUrls;
            ServerCapabilities = apiModel.ServerCapabilities;
            GatewayServerUri = apiModel.GatewayServerUri;
            DiscoveryProfileUri = apiModel.DiscoveryProfileUri;
        }
    }

}
