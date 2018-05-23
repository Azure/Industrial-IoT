// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Services.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Type of token to use for serverauth
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TokenType {

        /// <summary>
        /// Anonymous
        /// </summary>
        None,

        /// <summary>
        /// User name password
        /// </summary>
        UserNamePassword,

        /// <summary>
        /// Token is a x509 cert
        /// </summary>
        X509Certificate
    }
}
