// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using System;
    using System.Net.Http;

    /// <summary>
    /// Creates message handlers for a particular
    /// resource identified by the resource id.
    /// </summary>
    public interface IHttpHandlerFactory {

        /// <summary>
        /// Create message handler
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        TimeSpan Create(string resourceId,
            out HttpMessageHandler handler);
    }
}
