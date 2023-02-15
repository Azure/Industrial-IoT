// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using System;

    /// <summary>
    /// Injectable http client handler
    /// </summary>
    public interface IHttpHandler {

        /// <summary>
        /// Predicate to filter handlers per resource id.
        /// </summary>
        Func<string, bool> IsFor { get; }
    }
}