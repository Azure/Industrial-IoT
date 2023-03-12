// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Models;
    using System;

    /// <summary>
    /// Configuration for WriterGroup jobs
    /// </summary>
    public class WriterGroupJobConfig : IWriterGroupConfig
    {
        /// <inheritdoc/>
        public string PublisherId { get; set; }

        /// <inheritdoc/>
        public WriterGroupModel WriterGroup { get; set; }

        ///// <inheritdoc/>
        //public DataFlowOptions Options { get; set; }
    }
}
