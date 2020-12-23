// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using System.Threading.Tasks;

    /// <summary>
    /// Factory extensions
    /// </summary>
    public static class ClientFactoryEx {

        /// <summary>
        /// Create client
        /// </summary>
        /// <returns></returns>
        public static Task<IClient> CreateAsync(this IClientFactory factory) {
            return factory.CreateAsync("Module");
        }
    }
}
