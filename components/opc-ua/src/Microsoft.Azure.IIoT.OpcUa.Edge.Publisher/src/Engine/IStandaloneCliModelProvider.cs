// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;

    /// <summary>
    /// Interface that provides access to the StandaloneCliModel passed via command line arguments.
    /// </summary>
    public interface IStandaloneCliModelProvider {
        /// <summary>
        /// The instance of the StandaloneCliModel that represents the passed command line arguments.
        /// </summary>
        StandaloneCliModel StandaloneCliModel { get; }
    }
}
