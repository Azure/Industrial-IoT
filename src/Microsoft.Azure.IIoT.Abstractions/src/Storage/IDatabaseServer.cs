// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System.Threading.Tasks;

    /// <summary>
    /// Document database service
    /// </summary>
    public interface IDatabaseServer {

        /// <summary>
        /// Opens a named or default database
        /// </summary>
        /// <param name="id"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Task<IDatabase> OpenAsync(string id = null,
            DatabaseOptions options = null);
    }
}
