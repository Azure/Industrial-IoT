// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AspNetCore.Identity {
    using Microsoft.Azure.IIoT.Exceptions;

    /// <summary>
    /// Extensions
    /// </summary>
    public static class IdentityResultEx {

        /// <summary>
        /// Validate and throw if needed
        /// </summary>
        /// <param name="result"></param>
        public static void Validate(this IdentityResult result) {
            if (result.Succeeded) {
                return;
            }
            throw new IdentityException(result.Errors);
        }
    }

}
