// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//


namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models
{
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
    using Newtonsoft.Json;

    public partial class CertificateRequestRecordDetailsApiModel
    {
        /// <summary>
        /// Initializes a new instance of the ApplicationRecordRegisterApiModel
        /// class.
        /// </summary>
        public CertificateRequestRecordDetailsApiModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the ApplicationRecordRegisterApiModel
        /// class.
        /// </summary>
        public CertificateRequestRecordDetailsApiModel(CertificateRequestRecordApiModel apiModel, string operationResult)
        {
            ApiModel = apiModel;
            OperationResult = operationResult;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        [JsonProperty(PropertyName = "ApiModel")]
        public CertificateRequestRecordApiModel ApiModel { get; set; }

        [JsonProperty(PropertyName = "OperationResult")]
        public string OperationResult { get; set; }

    }
}
