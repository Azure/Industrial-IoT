// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher {
    using Azure.IIoT.OpcUa.Shared.Models;

    /// <summary>
    /// Writer group configuration provider
    /// </summary>
    public interface IWriterGroupConfig {
        /// <summary>
        /// Publisher id
        /// </summary>
        string PublisherId { get; }

        /// <summary>
        /// The writer group to execute
        /// </summary>
        WriterGroupModel WriterGroup { get; }
    }
}