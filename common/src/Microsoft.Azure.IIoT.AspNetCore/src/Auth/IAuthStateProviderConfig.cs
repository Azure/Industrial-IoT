// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth {
    using System;

    /// <summary>
    /// Configures the auth state provider for
    /// a blazor application.
    /// </summary>
    public interface IAuthStateProviderConfig {

        /// <summary>
        /// Target <see cref="Http.Resource"/> which
        /// determines the configuration to use.
        /// </summary>
        string Resource { get; }

        /// <summary>
        /// Revalidation
        /// </summary>
        TimeSpan RevalidateInterval { get; }
    }
}
