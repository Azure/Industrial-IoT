// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using System;

    /// <summary>
    /// Run context
    /// </summary>
    public static class HostContext {

        /// <summary>
        /// Running in container
        /// </summary>
        public static bool IsContainer
            => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")?
                .EqualsIgnoreCase("true") ?? false;
    }
}
