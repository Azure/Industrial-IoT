// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Publisher collector collects metrics from for writer groups
    /// inside the publisher during runtime.
    /// </summary>
    public interface IDiagnosticCollector
    {
        /// <summary>
        /// Remove writer group from collector
        /// </summary>
        /// <param name="writerGroupId">String with the id of the
        /// writer group or null for the default writer group</param>
        /// <returns></returns>
        bool RemoveWriterGroup(string writerGroupId);

        /// <summary>
        /// Reset collector diagnostic info
        /// </summary>
        /// <param name="writerGroupId">String with the id of the
        /// writer group or null for the default writer group</param>
        void ResetWriterGroup(string writerGroupId);

        /// <summary>
        /// Try get copy of diagnostics from collector.
        /// </summary>
        /// <param name="writerGroupId">String with the id of the
        /// writer group or null for the default writer group</param>
        /// <param name="diagnostic"></param>
        /// <returns></returns>
        bool TryGetDiagnosticsForWriterGroup(string writerGroupId,
            [NotNullWhen(true)] out WriterGroupDiagnosticModel? diagnostic);

        /// <summary>
        /// Enumerate diagnostics
        /// </summary>
        /// <returns></returns>
        IEnumerable<(string, WriterGroupDiagnosticModel)> EnumerateDiagnostics();
    }
}
