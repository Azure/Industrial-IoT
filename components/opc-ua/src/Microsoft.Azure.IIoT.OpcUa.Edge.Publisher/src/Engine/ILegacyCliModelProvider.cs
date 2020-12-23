// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;

    /// <summary>
    /// Interface that provides access to the LegacyCliModel passed via command line arguments.
    /// </summary>
    public interface ILegacyCliModelProvider {
        /// <summary>
        /// The instance of the LegacyCliModel that represents the passed command line arguments.
        /// </summary>
        LegacyCliModel LegacyCliModel { get; }
    }
}