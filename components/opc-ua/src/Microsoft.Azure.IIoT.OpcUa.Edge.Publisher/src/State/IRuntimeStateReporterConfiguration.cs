// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.State {

    /// <summary>
    /// Configuration of RuntimeStateReporter.
    /// </summary>
    public interface IRuntimeStateReporterConfiguration {

        /// <summary>
        /// Configuration flag for enabling/disabling runtime state reporting.
        /// </summary>
        bool EnableRuntimeStateReporting { get; }
    }
}
