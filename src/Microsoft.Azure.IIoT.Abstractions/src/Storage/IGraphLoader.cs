// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to bulk load edges and vertices into
    /// a graph.
    /// </summary>
    public interface IGraphLoader : IDisposable {

        /// <summary>
        /// Upsert vertex
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="vertex"></param>
        /// <returns></returns>
        Task AddVertexAsync<V>(V vertex);

        /// <summary>
        /// Upsert edge
        /// </summary>
        /// <typeparam name="V1"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="V2"></typeparam>
        /// <param name="outVertex"></param>
        /// <param name="edge"></param>
        /// <param name="inVertex"></param>
        /// <returns></returns>
        Task AddEdgeAsync<V1, E, V2>(V1 outVertex,
            E edge, V2 inVertex);

        /// <summary>
        /// Complete or abort processing
        /// </summary>
        /// <param name="abort"></param>
        /// <returns></returns>
        Task CompleteAsync(bool abort= false);
    }
}
