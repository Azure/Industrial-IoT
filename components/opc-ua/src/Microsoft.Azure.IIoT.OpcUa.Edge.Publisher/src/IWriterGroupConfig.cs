// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;

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