// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.State {

    using System.Threading.Tasks;

    /// <summary>
    /// Interface for runtime state reporting.
    /// </summary>
    public interface IRuntimeStateReporter {

        /// <summary>
        /// Send restart announcement.
        /// </summary>
        Task SendRestartAnnouncement();
    }
}