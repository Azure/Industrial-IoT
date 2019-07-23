// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Default {
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Create sql query engine adapter
    /// </summary>
    public class QueryEngineAdapter : IQueryEngine {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="provider"></param>
        public QueryEngineAdapter(Func<IEnumerable<IDocumentInfo<JObject>>, string,
            IEnumerable<IDocumentInfo<JObject>>> provider) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <inheritdoc/>
        public IEnumerable<IDocumentInfo<JObject>> ExecuteSql(
            IEnumerable<IDocumentInfo<JObject>> values, string query) {
            return _provider(values, query);
        }

        /// <inheritdoc/>
        public IEnumerable<IDocumentInfo<JObject>> ExecuteGremlin(
            IEnumerable<IDocumentInfo<JObject>> values, string query) {
            return _provider(values, query);
        }

        private readonly Func<IEnumerable<IDocumentInfo<JObject>>, string,
            IEnumerable<IDocumentInfo<JObject>>> _provider;
    }
}
