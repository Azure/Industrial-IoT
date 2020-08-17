// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Data {
    using System;

    public abstract class PagedResultBase {
        public int PageSize { get; set; }
        public int RowCount { get; set; }
        public string Error { get; set; }
        public string ContinuationToken { get; set; }
    }
}