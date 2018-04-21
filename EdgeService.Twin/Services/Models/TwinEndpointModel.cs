// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.EdgeService.Models {
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint persisted in twin setting and comparable
    /// </summary>
    public class TwinEndpointModel {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public EndpointModel ToServiceModel() {
            return new EndpointModel {
                Url = EndpointUrl,
                User = string.IsNullOrEmpty(User) ? null : User,
                Token = Token,
                TokenType = TokenType == TokenType.None ?
                    (TokenType?)null : TokenType,
                SecurityMode = SecurityMode == SecurityMode.Best ?
                    (SecurityMode?)null : SecurityMode,
                SecurityPolicy = SecurityPolicy,
                IsTrusted = IsTrusted ? true : (bool?)null,
                TwinId = null,
                SupervisorId = null
            };
        }

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndpointUrl { get; set; }

        /// <summary>
        /// User name to use
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string User { get; set; }

        /// <summary>
        /// User token to pass to server
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object Token { get; set; }

        /// <summary>
        /// Type of token
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TokenType TokenType { get; set; } = TokenType.None;

        /// <summary>
        /// Implict trust of the other side
        /// </summary>
        [JsonProperty]
        public bool IsTrusted { get; set; }

        /// <summary>
        /// Endpoint security policy to use - null = Best.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Security mode to use for communication - null = Best
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SecurityMode SecurityMode { get; set; } = SecurityMode.Best;

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            var endpoint = obj as TwinEndpointModel;
            return endpoint != null &&
                EndpointUrl == endpoint.EndpointUrl && User == endpoint.User &&
                TokenType == endpoint.TokenType && IsTrusted == endpoint.IsTrusted &&
                SecurityPolicy == endpoint.SecurityPolicy &&
                SecurityMode == endpoint.SecurityMode &&
                EqualityComparer<object>.Default
                    .Equals(Token, endpoint.Token);
        }

        public static bool operator ==(TwinEndpointModel ep1, TwinEndpointModel ep2) =>
            EqualityComparer<TwinEndpointModel>.Default.Equals(ep1, ep2);
        public static bool operator !=(TwinEndpointModel ep1, TwinEndpointModel ep2) =>
            !(ep1 == ep2);

        /// <summary>
        /// Hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            var hashCode = -1521869643;
            hashCode = hashCode * -1521134295 +
                TokenType.GetHashCode();
            hashCode = hashCode * -1521134295 +
                SecurityMode.GetHashCode();
            hashCode = hashCode * -1521134295 +
                IsTrusted.GetHashCode();
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(SecurityPolicy);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(EndpointUrl);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(User);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<object>.Default.GetHashCode(Token);
            return hashCode;
        }
    }
}
