// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net.Http {
#if NET46
    public interface IHttpClientFactory {
        HttpClient CreateClient(string resourceId);
    }
#endif
}