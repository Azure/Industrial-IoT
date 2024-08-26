// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Contains entry in the published nodes configuration representing
    /// the asset as well as an optional request header.
    /// </summary>
    [DataContract]
    public sealed record class PublishedNodeDeleteAssetRequestModel
    {
        /// <summary>
        /// Optional request header to use for all operations
        /// against the server.
        /// </summary>
        [DataMember(Name = "header", Order = 1,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; init; }

        /// <summary>
        /// The asset entry in the configuration that is either
        /// to be deleted. It must contain the as well as the writer
        /// id which represents the asset id.
        /// </summary>
        [DataMember(Name = "entry", Order = 2,
            EmitDefaultValue = false)]
        public required PublishedNodesEntryModel Entry { get; init; }

        /// <summary>
        /// The asset on the server is deleted no matter whether
        /// the removal in the publisher configuration was successful
        /// or not.
        /// </summary>
        [DataMember(Name = "force", Order = 3,
            EmitDefaultValue = false)]
        public bool Force { get; init; }
    }
}
