// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Provies persistency for key values
    /// </summary>
    public interface IPersistenceProvider {

        /// <summary>
        /// Writes key value pairs to a persistent
        /// storage.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        Task WriteAsync(IDictionary<string, dynamic> values);

        /// <summary>
        /// Read a value from persistant storage
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<dynamic> ReadAsync(string key);
    }
}
