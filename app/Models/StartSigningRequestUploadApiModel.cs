// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models
{
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
    using Newtonsoft.Json;

    public partial class StartSigningRequestUploadApiModel
    {
        /// <summary>
        /// Initializes a new instance of the StartSigningRequestUploadModel
        /// class.
        /// </summary>
        public StartSigningRequestUploadApiModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the StartSigningRequestUploadModel
        /// class.
        /// </summary>
        public StartSigningRequestUploadApiModel(StartSigningRequestApiModel apiModel)
        {
            ApiModel = apiModel;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        [JsonProperty(PropertyName = "ApiModel")]
        public StartSigningRequestApiModel ApiModel { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "CertificateRequestFile")]
        public IFormFile CertificateRequestFile { get; set; }

    }
}
