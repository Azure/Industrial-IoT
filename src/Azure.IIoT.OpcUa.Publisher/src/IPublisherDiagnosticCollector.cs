// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Publisher collector collects metrics from for writer groups
    /// inside the publisher during runtime.
    /// </summary>
    public interface IPublisherDiagnosticCollector
    {
        /// <summary>
        /// Remove writer group from collector
        /// </summary>
        /// <param name="writerGroupId"></param>
        /// <returns></returns>
        bool RemoveWriterGroup(string writerGroupId);

        /// <summary>
        /// Reset collector diagnostic info
        /// </summary>
        /// <param name="writerGroupId"></param>
        void ResetWriterGroup(string writerGroupId);

        /// <summary>
        /// Try get copy of diagnostics from collector
        /// </summary>
        /// <param name="writerGroupId"></param>
        /// <param name="diagnostic"></param>
        /// <returns></returns>
        bool TryGetDiagnosticsForWriterGroup(string writerGroupId,
            out WriterGroupDiagnosticModel diagnostic);
    }
}
