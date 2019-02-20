// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using Gremlin.Net.Process.Traversal;

    /// <summary>
    /// Gremlin traversal interface
    /// </summary>
    public interface IGremlinTraversal : IGremlinClient {

        /// <summary>
        /// Get vertex traversal start
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        ITraversal V(params (string, string)[] ids);

        /// <summary>
        /// Get edge traversal start
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        ITraversal E(params string[] ids);

        /// <summary>
        /// Returns a gremlin query to execute
        /// </summary>
        /// <param name="gremlin"></param>
        /// <param name="pageSize"></param>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        IResultFeed<T> Submit<T>(ITraversal gremlin,
            int? pageSize = null, string partitionKey = null);
    }
}
