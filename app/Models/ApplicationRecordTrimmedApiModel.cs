// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models
{
    public class ApplicationRecordTrimmedApiModel : ApplicationRecordApiModel
    {
        public ApplicationRecordTrimmedApiModel() : base()
        { }

        public ApplicationRecordTrimmedApiModel(ApplicationRecordApiModel apiModel) :
            base(apiModel.ApplicationId, apiModel.ID)
        {
            ApplicationUri = apiModel.ApplicationUri;
            ApplicationName = apiModel.ApplicationName;
            //ignore other data to reduce View size, add later as needed
            //ApplicationType = apiModel.ApplicationType;
            //ApplicationNames = apiModel.ApplicationNames;
            //ProductUri = apiModel.ProductUri;
            //DiscoveryUrls = apiModel.DiscoveryUrls;
            //ServerCapabilities = apiModel.ServerCapabilities;
            //GatewayServerUri = apiModel.GatewayServerUri;
            //DiscoveryProfileUri = apiModel.DiscoveryProfileUri;
            TrimLength = 40;
        }

        public int TrimLength { get; set; }
        public string ApplicationUriTrimmed { get => Trimmed(ApplicationUri); }
        public string ApplicationNameTrimmed { get => Trimmed(ApplicationName); }

        private string Trimmed(string value)
        {
            if (value?.Length > TrimLength)
                return value.Substring(0, TrimLength - 3) + "...";
            return value;
        }
    }

}
