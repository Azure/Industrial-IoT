// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents documents in the database
    /// </summary>
    public interface IDocuments {

        /// <summary>
        /// Add new item
        /// </summary>
        /// <param name="newItem"></param>
        /// <param name="ct"></param>
        /// <param name="id"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Task<IDocumentInfo<T>> AddAsync<T>(T newItem,
            CancellationToken ct = default,
            string id = null, OperationOptions options = null);

        /// <summary>
        /// Finds an item.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Task<IDocumentInfo<T>> FindAsync<T>(string id,
            CancellationToken ct = default,
            OperationOptions options = null);

        /// <summary>
        /// Replace item
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="value"></param>
        /// <param name="ct"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Task<IDocumentInfo<T>> ReplaceAsync<T>(IDocumentInfo<T> existing,
            T value, CancellationToken ct = default,
            OperationOptions options = null);

        /// <summary>
        /// Adds or updates an item.
        /// </summary>
        /// <exception cref="ResourceOutOfDateException"/>
        /// <param name="newItem"></param>
        /// <param name="ct"></param>
        /// <param name="id"></param>
        /// <param name="options"></param>
        /// <param name="etag"></param>
        /// <returns></returns>
        Task<IDocumentInfo<T>> UpsertAsync<T>(T newItem,
            CancellationToken ct = default,
            string id = null, OperationOptions options = null,
            string etag = null);

        /// <summary>
        /// Query items using expressions
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        IResultFeed<R> Query<T, R>(
            Func<IQueryable<IDocumentInfo<T>>, IQueryable<R>> query,
            int? pageSize = null, OperationOptions options = null);

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <exception cref="ResourceOutOfDateException"/>
        /// <param name="item"></param>
        /// <param name="ct"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Task DeleteAsync<T>(IDocumentInfo<T> item,
            CancellationToken ct = default,
            OperationOptions options = null);

        /// <summary>
        /// Delete an item by id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <param name="options"></param>
        /// <param name="etag"></param>
        /// <returns></returns>
        Task DeleteAsync(string id,
            CancellationToken ct = default,
            OperationOptions options = null, string etag = null);

        /// <summary>
        /// Query using sql. If not supported will throw.
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        /// <returns></returns>
        ISqlClient OpenSqlClient();
    }
}
