// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//


namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models
{
    using Newtonsoft.Json;

    public partial class ApplicationRecordRegisterApiModel
    {
        /// <summary>
        /// Initializes a new instance of the ApplicationRecordRegisterApiModel
        /// class.
        /// </summary>
        public ApplicationRecordRegisterApiModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the ApplicationRecordRegisterApiModel
        /// class.
        /// </summary>
        public ApplicationRecordRegisterApiModel(ApplicationRecordApiModel apiModel)
        {
            ApiModel = apiModel;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        [JsonProperty(PropertyName = "ApiModel")]
        public ApplicationRecordApiModel ApiModel { get; set; }

    }
}
