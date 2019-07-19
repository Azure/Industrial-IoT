// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a database
    /// </summary>
    public interface IDatabase {

        /// <summary>
        /// Opens or creates a (default) collection as a
        /// collection of document elements.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Task<IItemContainer> OpenContainerAsync(
            string id = null, ContainerOptions options = null);

        /// <summary>
        /// List all collections in the database
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<string>> ListContainersAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Delete (default) collection in database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task DeleteContainerAsync(string id = null);
    }
}
