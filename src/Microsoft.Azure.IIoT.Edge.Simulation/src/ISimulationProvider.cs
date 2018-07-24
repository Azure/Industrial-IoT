// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Simulation {
    using System.Threading.Tasks;

    public interface ISimulationProvider {

        /// <summary>
        /// Create or retrieve named simulator
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<ISimulator> CreateOrGetAsync(string name,
            bool closeOnDispose);
    }
}
