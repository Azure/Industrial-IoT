// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Credential model
    /// </summary>
    public class CredentialApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public CredentialApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public CredentialApiModel(CredentialModel model) {
            Value = model.Value;
            Type = model.Type;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public CredentialModel ToServiceModel() {
            return new CredentialModel {
                Value = Value,
                Type = Type
            };
        }

        /// <summary>
        /// Type of credential
        /// </summary>
        public CredentialType? Type { get; set; }

        /// <summary>
        /// Value to pass to server
        /// </summary>
        public JToken Value { get; set; }
    }
}
