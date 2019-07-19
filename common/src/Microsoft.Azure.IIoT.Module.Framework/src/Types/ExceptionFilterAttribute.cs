// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework {
    using System;

    /// <summary>
    /// Filter exceptions on controller and returns a status code for it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = false, Inherited = true)]
    public abstract class ExceptionFilterAttribute : Attribute {

        /// <summary>
        /// Default constructor
        /// </summary>
        protected ExceptionFilterAttribute() { }

        /// <summary>
        /// Filter exception and return a status code
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="status"></param>
        /// <returns>response string or null</returns>
        public abstract Exception Filter(Exception exception, out int status);
    }
}
