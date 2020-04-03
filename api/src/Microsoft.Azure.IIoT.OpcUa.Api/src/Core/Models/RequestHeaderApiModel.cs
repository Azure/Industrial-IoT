// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Request header model
    /// </summary>
    [DataContract]
    public class RequestHeaderApiModel {

        /// <summary>
        /// Optional User elevation
        /// </summary>
        [DataMember(Name = "elevation", Order = 0,
            EmitDefaultValue = false)]
        public CredentialApiModel Elevation { get; set; }

        /// <summary>
        /// Optional list of locales in preference order.
        /// </summary>
        [DataMember(Name = "locales", Order = 1,
            EmitDefaultValue = false)]
        public List<string> Locales { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        [DataMember(Name = "diagnostics", Order = 2,
            EmitDefaultValue = false)]
        public DiagnosticsApiModel Diagnostics { get; set; }
    }

}
