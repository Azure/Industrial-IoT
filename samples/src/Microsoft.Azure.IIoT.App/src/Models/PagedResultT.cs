// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Data {
    using System.Collections.Generic;

    public class PagedResult<T> : PagedResultBase where T : class {

        public List<T> Results { get; set; }

        public PagedResult() {
            Results = new List<T>();
        }
    }
}
