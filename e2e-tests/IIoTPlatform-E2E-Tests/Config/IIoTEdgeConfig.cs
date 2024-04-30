// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests.Config
{
    using System.Collections.Generic;

    /// <summary>
    /// IoT Edge configuration
    /// </summary>
    public interface IIoTEdgeConfig
    {
        /// <summary>
        /// IoT Edge version
        /// </summary>
        string EdgeVersion { get; }
    }
}
