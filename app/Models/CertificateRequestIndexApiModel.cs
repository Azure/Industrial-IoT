// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models
{
    public class CertificateRequestIndexApiModel : CertificateRequestRecordApiModel
    {
        public CertificateRequestIndexApiModel() : base()
        { }

        public CertificateRequestIndexApiModel(CertificateRequestRecordApiModel apiModel) :
            base(apiModel.State, apiModel.SigningRequest)
        {
            RequestId = apiModel.RequestId;
            ApplicationId = apiModel.ApplicationId;
            CertificateGroupId = apiModel.CertificateGroupId;
            CertificateTypeId = apiModel.CertificateTypeId;
            SubjectName = apiModel.SubjectName;
            DomainNames = apiModel.DomainNames;
            PrivateKeyFormat = apiModel.PrivateKeyFormat;
            TrimLength = 40;
        }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "ApplicationUri")]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "ApplicationName")]
        public string ApplicationName { get; set; }

        public int TrimLength { get; set; }
        public string ApplicationUriTrimmed { get => Trimmed(ApplicationUri); }
        public string ApplicationNameTrimmed { get => Trimmed(ApplicationName); }
        public string SubjectNameTrimmed { get => Trimmed(SubjectName); }

        private string Trimmed(string value)
        {
            if (value?.Length > TrimLength)
                return value.Substring(0, TrimLength - 3) + "...";
            return value;
        }


    }
}
