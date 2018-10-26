// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//


namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Models
{
    using Newtonsoft.Json;
    using System;

    public partial class KeyDetailsApiModel
    {
        /// <summary>
        /// Initializes a new instance of the KeyDetailsApiModel
        /// class.
        /// </summary>
        public KeyDetailsApiModel()
        {
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        [JsonProperty(PropertyName = "EncodedBase64")]
        public string EncodedBase64 { get; set; }

    }
}
