// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Management {
    using System.Threading.Tasks;

    public interface IConfigProvider {

        /// <summary>
        /// Returns the context information of our 
        /// infrastructure.
        /// </summary>
        /// <returns></returns>
        Task<IManagementConfig> GetContextAsync();
    }
}