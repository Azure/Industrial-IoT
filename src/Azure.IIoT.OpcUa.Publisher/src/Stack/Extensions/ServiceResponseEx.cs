// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;

    internal static class ServiceResponseEx
    {
        /// <summary>
        /// Validate response
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="statusCode"></param>
        /// <param name="diagnostics"></param>
        /// <param name="requested"></param>
        public static ServiceResponse<TRequest, TResult> Validate<TRequest, TResult>(
            this IServiceResponse response, IEnumerable<TResult>? results,
            Func<TResult, StatusCode> statusCode, DiagnosticInfoCollection? diagnostics,
            IEnumerable<TRequest>? requested)
        {
            return new ServiceResponse<TRequest, TResult>(response, results,
                statusCode, diagnostics, requested);
        }

        /// <summary>
        /// Validate response
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="statusCode"></param>
        /// <param name="diagnostics"></param>
        public static ServiceResponse<object?, TResult> Validate<TResult>(
            this IServiceResponse response, IEnumerable<TResult>? results,
            Func<TResult, StatusCode> statusCode, DiagnosticInfoCollection? diagnostics)
        {
            return new ServiceResponse<object?, TResult>(response, results,
                statusCode, diagnostics, null);
        }

        /// <summary>
        /// Create a lookup table
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        public static IDictionary<TRequest, (TResult, ServiceResultModel?)> AsLookupTable<TRequest, TResult>(
            this ServiceResponse<TRequest, TResult> response) where TRequest : struct
        {
            var lookup = new Dictionary<TRequest, (TResult, ServiceResultModel?)>();
            foreach (var operation in response)
            {
                lookup.AddOrUpdate(operation.Request, (operation.Result, operation.ErrorInfo));
            }
            return lookup;
        }
    }
}
