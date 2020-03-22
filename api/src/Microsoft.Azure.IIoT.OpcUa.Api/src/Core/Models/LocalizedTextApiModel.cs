// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Localized text.
    /// </summary>
    [DataContract]
    public class LocalizedTextApiModel {

        /// <summary>
        /// Locale or null for default locale
        /// </summary>
        [DataMember(Name = "locale",
            EmitDefaultValue = false)]
        public string Locale { get; set; }

        /// <summary>
        /// Text
        /// </summary>
        [DataMember(Name = "text")]
        public string Text { get; set; }
    }
}
