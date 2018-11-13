// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Attribute value read
    /// </summary>
    public class AttributeReadResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public AttributeReadResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public AttributeReadResponseApiModel(AttributeReadResultModel model) {
            Value = model.Value;
            ErrorInfo = model.ErrorInfo == null ? null :
                new ServiceResultApiModel(model.ErrorInfo);
        }

        /// <summary>
        /// Attribute value
        /// </summary>
        public JToken Value { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
