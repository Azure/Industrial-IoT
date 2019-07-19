// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Application document in cosmos db database
    /// </summary>
    [Serializable]
    public class ApplicationDocument {

        /// <summary>
        /// Document id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string ApplicationId { get; set; }

        /// <summary>
        /// Etag
        /// </summary>
        [JsonProperty(PropertyName = "_etag")]
        public string ETag { get; set; }

        /// <summary>
        /// Document Type
        /// </summary>
        public string ClassType { get; set; } = ClassTypeName;

        /// <summary>
        /// Numeric id
        /// </summary>
        public uint ID { get; set; }

        /// <summary>
        /// Application state
        /// </summary>
        public ApplicationState ApplicationState { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Non-localized Application name
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Application type
        /// </summary>
        public ApplicationType ApplicationType { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        public string ProductUri { get; set; }

        /// <summary>
        /// Capabilities
        /// </summary>
        public string ServerCapabilities { get; set; }

        /// <summary>
        /// Alternative Names
        /// </summary>
        public LocalizedText[] ApplicationNames { get; set; }

        /// <summary>
        /// Discovery url
        /// </summary>
        public string[] DiscoveryUrls { get; set; }

        /// <summary>
        /// Gateway server
        /// </summary>
        public string GatewayServerUri { get; set; }

        /// <summary>
        /// Discovery Profile
        /// </summary>
        public string DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Site Id
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Create time
        /// </summary>
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// Authority
        /// </summary>
        public string CreateAuthorityId { get; set; }

        /// <summary>
        /// Approval time
        /// </summary>
        public DateTime? ApproveTime { get; set; }

        /// <summary>
        /// Authority
        /// </summary>
        public string ApproveAuthorityId { get; set; }

        /// <summary>
        /// Update time
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// Authority
        /// </summary>
        public string UpdateAuthorityId { get; set; }

        /// <summary>
        /// Disable time
        /// </summary>
        public DateTime? NotSeenSince { get; set; }

        /// <summary>
        /// Authority
        /// </summary>
        public string DisableAuthorityId { get; set; }

        /// <inheritdoc/>
        public static readonly string ClassTypeName = "Application";

        /// <summary>
        /// Application name
        /// </summary>
        public class LocalizedText {

            /// <summary>
            /// Text
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Locale
            /// </summary>
            public string Locale { get; set; }
        }
    }
}
