// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Storage {
    using System.Threading.Tasks;

    /// <summary>
    /// Graph loader extensions
    /// </summary>
    public static class GraphLoaderEx {

        /// <summary>
        /// Add edge to a vertex that is defined only by its mandatory fields
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="loader"></param>
        /// <param name="outVertex"></param>
        /// <param name="edge"></param>
        /// <param name="inVId"></param>
        /// <param name="inVLabel"></param>
        /// <param name="inVPk"></param>
        /// <returns></returns>
        public static Task AddEdgeAsync<V, E>(this IGraphLoader loader,
            V outVertex, E edge, string inVId, string inVLabel, string inVPk = null) {
            return loader.AddEdgeAsync(outVertex, edge, new {
                id = inVId,
                label = inVLabel,
                __pk = inVPk
            });
        }

        /// <summary>
        /// Add edge from a vertex that is defined only by its mandatory fields
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="loader"></param>
        /// <param name="outVertex"></param>
        /// <param name="edge"></param>
        /// <param name="outVId"></param>
        /// <param name="outVLabel"></param>
        /// <param name="outVPk"></param>
        /// <returns></returns>
        public static Task AddEdgeAsync<E, V>(this IGraphLoader loader,
            string outVId, string outVLabel, E edge, V outVertex, string outVPk = null) {
            return loader.AddEdgeAsync(new {
                id = outVId,
                label = outVLabel,
                __pk = outVPk
            }, edge, outVertex);
        }
    }
}
