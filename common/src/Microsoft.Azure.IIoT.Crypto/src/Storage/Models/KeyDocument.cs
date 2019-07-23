// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Storage.Models {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Key document
    /// </summary>
    public class KeyDocument {

        /// <summary>
        /// Key id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Whether the key is disabled
        /// </summary>
        public bool IsDisabled { get; set; }

        /// <summary>
        /// Whether the key is exportable
        /// </summary>
        public bool IsExportable { get; set; }

        /// <summary>
        /// Key itself in json format
        /// </summary>
        public JToken KeyJson { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public string Type => nameof(Key);
    }
}

