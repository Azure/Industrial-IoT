// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Type of credentials to use for authentication
    /// </summary>
    [JsonConverter(typeof(CredentialTypeEnumConverter))]
    public enum CredentialType {

        /// <summary>
        /// No credentials for anonymous access
        /// </summary>
        None,

        /// <summary>
        /// User name and password as credential
        /// </summary>
        UserName,

        /// <summary>
        /// Credential is a x509 certificate
        /// </summary>
        X509Certificate,

        /// <summary>
        /// Jwt token as credential
        /// </summary>
        JwtToken
    }

    /// <summary>
    /// Convert old *-1.0.1 enum string value to UserName credential
    /// </summary>
    public class CredentialTypeEnumConverter : StringEnumConverter {

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(CredentialType);
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.String &&
                reader.Value.ToString().StartsWith(nameof(CredentialType.UserName),
                    StringComparison.InvariantCulture)) {
                return CredentialType.UserName;
            }
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }
}
