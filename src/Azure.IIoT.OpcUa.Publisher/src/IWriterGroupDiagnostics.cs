// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    /// <summary>
    /// Writer group diagnostics control
    /// </summary>
    public interface IWriterGroupDiagnostics
    {
        /// <summary>
        /// Reset diagnostics for writer group
        /// </summary>
        void ResetWriterGroupDiagnostics();
    }
}
