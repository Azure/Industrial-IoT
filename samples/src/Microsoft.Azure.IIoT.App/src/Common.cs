// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.AspNetCore.Components;
using Microsoft.Azure.IIoT.App.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.App.Common {
    public class UICommon : ComponentBase  {

        public PagedResult<T> UpdatePage<T>(
                                        Func<PagedResult<T>,
                                        Task<PagedResult<T>>> getList, 
                                        int page, PagedResult<T> list, 
                                        ref PagedResult<T> pagedList) where T : class {
         
            PagedResult<T> newList = list;
            if (!string.IsNullOrEmpty(list.ContinuationToken) && page > pagedList.PageCount) {
                    list = Task.Run(async () => await getList(newList)).Result;
            }
            pagedList = list.GetPaged(page, PageLength, null);
            return list;
        }

        public int PageLength { get; set; } = 10;
        public int PageLengthSmall { get; set; } = 4;
        public string None { get; set; } = "(None)";
        public string Spinner { get; set; }
        public string CredentialKey { get; } = "credential";
        public Dictionary<string, string> ApplicationUri { get; set; } = new Dictionary<string, string>();
    }
}
