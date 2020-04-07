// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    using Microsoft.AspNetCore.Identity;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Identity exception
    /// </summary>
    public class IdentityException : Exception {

        /// <summary>
        /// Errors
        /// </summary>
        public IEnumerable<IdentityError> Errors { get; }

        /// <summary>
        /// Create empty
        /// </summary>
        public IdentityException() {
            Errors = new List<IdentityError>();
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="errors"></param>
        public IdentityException(IEnumerable<IdentityError> errors) {
            Errors = errors;
        }
    }
}
