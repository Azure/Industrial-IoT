// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System;

    /// <summary>
    /// Gremlin client interface
    /// </summary>
    public interface IGremlinClient : IDisposable {

        /// <summary>
        /// Returns a gremlin query to execute
        /// </summary>
        /// <param name="gremlin"></param>
        /// <param name="pageSize"></param>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        IResultFeed<T> Submit<T>(string gremlin,
            int? pageSize = null, string partitionKey = null);
    }
}
