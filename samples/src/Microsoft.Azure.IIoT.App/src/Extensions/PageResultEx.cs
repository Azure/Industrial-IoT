// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Data {
    using System;
    using System.Linq;

    /// <summary>
    /// Page extensions
    /// </summary>
    public static class PagedResultEx {
        public static PagedResult<T> GetPaged<T>(this PagedResult<T> query, int page, int pageSize, string error) where T : class {
            var result = new PagedResult<T> {
                CurrentPage = page,
                PageSize = pageSize,
                RowCount = query.Results.Count,
                Error = error
            };

            var pageCount = (double)result.RowCount / pageSize;
            result.PageCount = (int)Math.Ceiling(pageCount);

            var skip = (page - 1) * pageSize;
            result.Results = query.Results.Skip(skip).Take(pageSize).ToList();

            query.CurrentPage = page;

            return result;
        }
    }
}