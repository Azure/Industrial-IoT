// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Azure {
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Log entry writer based on cosmos db collection
    /// </summary>
    public class CosmosDbAuditLogWriter : CosmosDbCollection<AuditLogEntryModel>,
        IAuditLogWriter {

        /// <summary>
        /// Create provider
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public CosmosDbAuditLogWriter(ICosmosDbConfig config, ILogger logger) :
            base (config, logger) {
        }

        /// <inheritdoc/>
        public Task WriteAsync(AuditLogEntryModel entry) => CreateAsync(entry);
    }
}
