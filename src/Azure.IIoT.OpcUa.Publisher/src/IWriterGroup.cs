// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;

    /// <summary>
    /// Writer group configuration
    /// </summary>
    public interface IWriterGroup
    {
        /// <summary>
        /// Identifier of the writer group
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Get the writer group configuration
        /// </summary>
        WriterGroupModel Configuration { get; }
    }
}
