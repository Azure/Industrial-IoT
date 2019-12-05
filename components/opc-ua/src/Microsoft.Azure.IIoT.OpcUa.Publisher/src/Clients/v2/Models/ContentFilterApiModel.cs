// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Content filter
    /// </summary>
    public class ContentFilterApiModel : JObject {

        /// <inheritdoc/>
        public ContentFilterApiModel() {
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public ContentFilterApiModel(ContentFilterModel model) :
            base(model) {
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ContentFilterModel ToServiceModel() {
            return new ContentFilterModel (this);
        }
    }
}