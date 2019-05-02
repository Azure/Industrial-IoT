// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB
{
    public interface IDocumentDBCollection<T> where T : class
    {
        DocumentCollection Collection { get; }
        Task CreateCollectionIfNotExistsAsync();
        Task<Document> CreateAsync(T item);
        Task DeleteAsync(Guid id);
        Task<T> GetAsync(Guid id);
        Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate);
        Task<(string, IEnumerable<T>)> GetPageAsync(
            Expression<Func<T, bool>> predicate,
            string continuationToken,
            int? maxItemCount
            );
        Task<IEnumerable<T>> GetAsync(SqlQuerySpec sqlQuerySpec);
        Task<(string, IEnumerable<T>)> GetPageAsync(
            SqlQuerySpec sqlQuerySpec,
            string continuationToken,
            int? maxItemCount
            );
        Task<Document> UpdateAsync(Guid id, T item, string eTag);
    }
}
