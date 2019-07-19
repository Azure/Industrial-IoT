// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Query engine abstraction for memory database
    /// </summary>
    public interface IQueryEngine {

        /// <summary>
        /// Execute sql expression over the passed in values 
        /// to return result
        /// </summary>
        /// <param name="values"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        IEnumerable<IDocumentInfo<JObject>> ExecuteSql(
            IEnumerable<IDocumentInfo<JObject>> values, string query);

        /// <summary>
        /// Execute gremlin expression over the passed in values 
        /// to return result
        /// </summary>
        /// <param name="values"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        IEnumerable<IDocumentInfo<JObject>> ExecuteGremlin(
            IEnumerable<IDocumentInfo<JObject>> values, string query);
    }
}
