// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    /// <summary>
    /// Endpoint persisted in twin and comparable
    /// </summary>
    public class OpcUaEndpointTag {

        /// <summary>
        /// Default constructor
        /// </summary>
        public OpcUaEndpointTag() {}

        /// <summary>
        /// Create tag from endpoint model
        /// </summary>
        /// <param name="endpoint"></param>
        public OpcUaEndpointTag(ServerEndpointModel endpoint) {
            Id = endpoint.Url.ToLowerInvariant();
            Url = endpoint.Url;
            User = endpoint.User ?? string.Empty;
            Token = endpoint.Token;
            Type = endpoint.Type;
            ClientCertificate = ToDictionary(endpoint.ClientCertificate);
            ServerCertificate = ToDictionary(endpoint.ServerCertificate);
            EdgeController = endpoint.EdgeController ?? string.Empty;
            IsTrusted = endpoint.IsTrusted ?? false;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public ServerEndpointModel ToServiceModel() {
            return new ServerEndpointModel {
                Url = Url,
                User = string.IsNullOrEmpty(User) ? null : User,
                Token = Token,
                Type = Type,
                ServerCertificate = FromDictionary(ServerCertificate),
                ClientCertificate = FromDictionary(ClientCertificate),
                EdgeController = string.IsNullOrEmpty(EdgeController) ? null : EdgeController,
                IsTrusted = IsTrusted ? true : (bool?)null
            };
        }

        /// <summary>
        /// Lower case endpoint url
        /// </summary>
        [JsonProperty(PropertyName = "EndpointId")]
        public string Id { get; set; }

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        [JsonProperty(PropertyName = "Url")]
        public string Url { get; set; }

        /// <summary>
        /// User name to use
        /// </summary>
        [JsonProperty(PropertyName = "User")]
        public string User { get; set; }

        /// <summary>
        /// User token to pass to server
        /// </summary>
        [JsonProperty(PropertyName = "Token")]
        public object Token { get; set; }

        /// <summary>
        /// Type of token
        /// </summary>
        [JsonProperty(PropertyName = "TokenType")]
        public TokenType Type { get; set; }

        /// <summary>
        /// Implict trust of the other side
        /// </summary>
        [JsonProperty(PropertyName = "IsTrusted")]
        public bool IsTrusted { get; set; }

        /// <summary>
        /// Returns the public certificate presented by the server
        /// </summary>
        [JsonProperty(PropertyName = "ServerCertificate")]
        public Dictionary<string, string> ServerCertificate { get; set; }

        /// <summary>
        /// Returns the public certificate to present to the server.
        /// </summary>
        [JsonProperty(PropertyName = "ClientCertificate")]
        public Dictionary<string, string> ClientCertificate { get; set; }

        /// <summary>
        /// Edge controller device to use - if not set, uses
        /// proxy to access.
        /// </summary>
        [JsonProperty(PropertyName = "EdgeController")]
        public string EdgeController { get; set; }

        /// <summary>
        /// Provide custom serialization by chunking the cert
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        private Dictionary<string, string> ToDictionary(X509Certificate2 certificate) {
            if (certificate == null) {
                return null;
            }
            var str = certificate == null ? string.Empty :
                Convert.ToBase64String(certificate.GetRawCertData());
            var result = new Dictionary<string, string>();
            for (var i = 0; ; i++) {
                if (str.Length < 512) {
                    result.Add($"part_{i}", str);
                    break;
                }
                var part = str.Substring(0, 512);
                result.Add($"part_{i}", part);
                str = str.Substring(512);
            }
            return result;
        }

        /// <summary>
        /// Provide custom serialization by chunking the cert
        /// </summary>
        /// <param name="chunks"></param>
        /// <returns></returns>
        private X509Certificate2 FromDictionary(Dictionary<string, string> chunks) {
            if (chunks == null) {
                return null;
            }
            var str = new StringBuilder();
            for (var i = 0; ; i++) {
                if (!chunks.TryGetValue($"part_{i}", out var chunk)) {
                    break;
                }
                str.Append(chunk);
            }
            if (str.Length == 0) {
                return null;
            }
            return new X509Certificate2(Convert.FromBase64String(str.ToString()));
        }

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            var tag = obj as OpcUaEndpointTag;
            return tag != null && 
                Id == tag.Id && Url == tag.Url && User == tag.User && Type == tag.Type && 
                IsTrusted == tag.IsTrusted && EdgeController == tag.EdgeController &&
                EqualityComparer<object>.Default
                    .Equals(Token, tag.Token) &&
                EqualityComparer<Dictionary<string, string>>.Default
                    .Equals(ServerCertificate, tag.ServerCertificate) &&
                EqualityComparer<Dictionary<string, string>>.Default
                    .Equals(ClientCertificate, tag.ClientCertificate);
        }

        public static bool operator ==(OpcUaEndpointTag tag1, OpcUaEndpointTag tag2) =>
            EqualityComparer<OpcUaEndpointTag>.Default.Equals(tag1, tag2);
        public static bool operator !=(OpcUaEndpointTag tag1, OpcUaEndpointTag tag2) =>
            !(tag1 == tag2);

        /// <summary>
        /// Hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            var hashCode = -1521869643;
            hashCode = hashCode * -1521134295 + 
                Type.GetHashCode();
            hashCode = hashCode * -1521134295 + 
                IsTrusted.GetHashCode();
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<string>.Default.GetHashCode(Id);
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<string>.Default.GetHashCode(Url);
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<string>.Default.GetHashCode(User);
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<object>.Default.GetHashCode(Token);
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<Dictionary<string, string>>.Default.GetHashCode(ServerCertificate);
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<Dictionary<string, string>>.Default.GetHashCode(ClientCertificate);
            hashCode = hashCode * -1521134295 + 
                EqualityComparer<string>.Default.GetHashCode(EdgeController);
            return hashCode;
        }
    }
}
