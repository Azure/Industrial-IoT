// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime {

    /// <summary>
    /// Encoding configuration for pub/sub messages
    /// </summary>
    public class PubSubEncodingConfig : IPubSubEncodingConfig {

        /// <inheritdoc/>
        public string ContentType { get; set; }

        /// <inheritdoc/>
        public uint NetworkMessageContentMask { get; set; }

        /// <inheritdoc/>
        public uint DataSetMessageContentMask { get; set; }

        /// <inheritdoc/>
        public uint FieldContentMask { get; set; }
    }
}