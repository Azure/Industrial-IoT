// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Request to create an asset in the configuration api
    /// </summary>
    /// <typeparam name="T">Type of the configuration</typeparam>
    [DataContract]
    public sealed record class PublishedNodeCreateAssetRequestModel<T>
    {
        /// <summary>
        /// Optional request header to use for all operations
        /// against the server.
        /// </summary>
        [DataMember(Name = "header", Order = 1,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; init; }

        /// <summary>
        /// The asset entry in the configuration that is to be
        /// created or updated. It must contain the writer group
        /// id as well as the data set name which is the name of
        /// asset.
        /// </summary>
        [DataMember(Name = "entry", Order = 2,
            EmitDefaultValue = false)]
        public required PublishedNodesEntryModel Entry { get; init; }

        /// <summary>
        /// The asset configuration to use when creating the asset.
        /// </summary>
        [DataMember(Name = "configuration", Order = 3,
            EmitDefaultValue = false)]
        public required T Configuration { get; init; }

        /// <summary>
        /// Time to wait after the configuration is applied to perform
        /// the configuration of the asset in the configuration api.
        /// This is to let the server settle.
        /// </summary>
        [DataMember(Name = "waitTime", Order = 4,
            EmitDefaultValue = false)]
        public TimeSpan? WaitTime { get; init; }
    }
}
