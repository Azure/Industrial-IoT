// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Microsoft.Azure.IIoT.Http {
    using System;

    /// <summary>
    /// Configuration made by handler implementations
    /// </summary>
    public interface IHttpHandlerHost {

        /// <summary>
        /// Set Maximum lifetime
        /// </summary>
        void SetMaxLifetime(TimeSpan max);
    }
}
