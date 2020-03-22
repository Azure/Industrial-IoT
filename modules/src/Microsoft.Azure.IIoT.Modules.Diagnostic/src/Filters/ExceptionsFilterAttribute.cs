// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Diagnostic.v2.Filters {
    using Microsoft.Azure.IIoT.Module.Framework;
    using System;
    using System.Net;

    /// <summary>
    /// Convert all the exceptions returned by the module controllers to a
    /// status code.
    /// </summary>
    public class ExceptionsFilterAttribute : ExceptionFilterAttribute {

        /// <inheritdoc />
        public override Exception Filter(Exception exception, out int status) {
            status = (int)HttpStatusCode.InternalServerError;
            return exception;
        }
    }
}
