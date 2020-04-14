// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Source vertex describing the source.
    /// </summary>
    [Label(AddressSpaceElementNames.Source)]
    [DataContract]
    public class SourceVertexModel : IVertex {

        /// <summary>
        /// Returns the vertex identifier
        /// </summary>
        [DataMember(Name = "id")]
        public string Id => Uri + "/" + Version;

        /// <summary>
        /// Source uri
        /// </summary>
        [DataMember(Name = "_uri")]
        public Uri Uri { get; set; }

        /// <summary>
        /// Source semantic versioning
        /// </summary>
        [DataMember(Name = "_version")]
        public string Version { get; set; }

        /// <summary>
        /// Custom tags
        /// </summary>
        [DataMember(Name = "tag")]
        public IDictionary<string, string> Tag { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is SourceVertexModel model)) {
                return false;
            }
            if (Id == model.Id) {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return Id.GetHashSafe();
        }
    }
}
