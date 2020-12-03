// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestExtensions {
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IIoTHubDeployment {

        /// <summary>
        /// Create a new layered deployment or update an existing one
        /// </summary>
        Task<bool> CreateOrUpdateLayeredDeploymentAsync();

        /// <summary>
        /// Create a deployment modules object
        /// </summary>
        IDictionary<string, IDictionary<string, object>> CreateDeploymentModules();
    }
}
