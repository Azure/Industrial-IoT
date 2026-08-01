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
            Func<TResult, StatusCode> statusCode, List<DiagnosticInfo>? diagnostics,
            IEnumerable<TRequest>? requested)
        {
            return new ServiceResponse<TRequest, TResult>(response, results,
                statusCode, diagnostics, requested);
        }

        /// <summary>
        /// Validate response (2.0 ArrayOf overload). The 2.0 stack returns
        /// results and diagnostics as <see cref="ArrayOf{T}"/> which is not an
        /// <see cref="IEnumerable{T}"/>; materialize it here.
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="statusCode"></param>
        /// <param name="diagnostics"></param>
        /// <param name="requested"></param>
        public static ServiceResponse<TRequest, TResult> Validate<TRequest, TResult>(
            this IServiceResponse response, ArrayOf<TResult> results,
            Func<TResult, StatusCode> statusCode, ArrayOf<DiagnosticInfo> diagnostics,
            IEnumerable<TRequest>? requested)
        {
            return new ServiceResponse<TRequest, TResult>(response, results.ToArray(),
                statusCode, ToDiagnostics(diagnostics), requested);
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
            Func<TResult, StatusCode> statusCode, List<DiagnosticInfo>? diagnostics)
        {
            return new ServiceResponse<object?, TResult>(response, results,
                statusCode, diagnostics, null);
        }

        /// <summary>
        /// Validate response (2.0 ArrayOf overload).
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="statusCode"></param>
        /// <param name="diagnostics"></param>
        public static ServiceResponse<object?, TResult> Validate<TResult>(
            this IServiceResponse response, ArrayOf<TResult> results,
            Func<TResult, StatusCode> statusCode, ArrayOf<DiagnosticInfo> diagnostics)
        {
            return new ServiceResponse<object?, TResult>(response, results.ToArray(),
                statusCode, ToDiagnostics(diagnostics), null);
        }

        /// <summary>
        /// Materialize an ArrayOf diagnostics into the classic collection type.
        /// </summary>
        /// <param name="diagnostics"></param>
        private static List<DiagnosticInfo>? ToDiagnostics(ArrayOf<DiagnosticInfo> diagnostics)
        {
            if (diagnostics.IsNull)
            {
                return null;
            }
            var array = diagnostics.ToArray();
            return array == null ? null : new List<DiagnosticInfo>(array);
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
