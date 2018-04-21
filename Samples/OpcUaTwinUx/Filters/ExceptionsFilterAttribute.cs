// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Browser.Filters {
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Azure.IIoT.Browser.Models;
    using System;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Detect all the unhandled exceptions returned by the API controllers
    /// and decorate the response accordingly, managing the HTTP status code
    /// and preparing a JSON response with useful error details.
    /// When including the stack trace, split the text in multiple lines
    /// for an easier parsing.
    /// @see https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters
    /// </summary>
    public class ExceptionsFilterAttribute : ExceptionFilterAttribute {

        /// <inheritdoc />
        public override void OnException(ExceptionContext context) {
            if (context.Exception == null) {
                base.OnException(context);
                return;
            }
            switch(context.Exception) {
                case UnauthorizedAccessException ue:
                    context.Result = GetResponse(HttpStatusCode.Unauthorized, 
                        context.Exception);
                    break;
                case ArgumentException ae:
                    context.Result = GetResponse(HttpStatusCode.BadRequest, 
                        context.Exception);
                    break;
                case NotImplementedException ne:
                case NotSupportedException ns:
                    context.Result = GetResponse(HttpStatusCode.NotImplemented,
                        context.Exception);
                    break;
                case TimeoutException te:
                    context.Result = GetResponse(HttpStatusCode.RequestTimeout,
                        context.Exception);
                    break;
                default:
                    context.Result = GetResponse(HttpStatusCode.InternalServerError, 
                        context.Exception);
                    break;
            }
        }

        /// <inheritdoc />
        public override Task OnExceptionAsync(ExceptionContext context) {
            try {
                OnException(context);
                return Task.CompletedTask;
            }
            catch (Exception) {
                return base.OnExceptionAsync(context);
            }
        }

        /// <summary>
        /// Create result
        /// </summary>
        /// <param name="code"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        private ObjectResult GetResponse(HttpStatusCode code, Exception exception) {
            var result = new ObjectResult(exception) {
                StatusCode = (int)code
            };
            return result;
        }
    }
}
