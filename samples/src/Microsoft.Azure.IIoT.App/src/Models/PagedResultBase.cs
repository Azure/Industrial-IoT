// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Data {

    public abstract class PagedResultBase {
        public string Error { get; set; }
        public string ContinuationToken { get; set; }
    }
}