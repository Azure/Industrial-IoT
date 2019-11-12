// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher {

    /// <summary>
    /// Encoding configuration for publish subscribe messages
    /// </summary>
    public interface IPubSubEncodingConfig : IEncodingConfig {

        /// <summary>
        /// Data set message content flags
        /// </summary>
        uint DataSetMessageContentMask { get; }

        /// <summary>
        /// Data field content flags
        /// </summary>
        uint FieldContentMask { get; }

        /// <summary>
        /// Network message content mask
        /// </summary>
        uint NetworkMessageContentMask { get; }
    }
}