// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Content filter
    /// </summary>
    public class ContentFilterModel : JObject {

        /// <inheritdoc/>
        public ContentFilterModel() {
        }
        /// <inheritdoc/>
        public ContentFilterModel(JObject other) : base(other) {
        }
    }
}