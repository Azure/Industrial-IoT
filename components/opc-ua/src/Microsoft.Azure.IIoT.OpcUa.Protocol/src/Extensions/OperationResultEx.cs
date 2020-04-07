// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Opc.Ua;
    using Opc.Ua.Client;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Operation result extensions
    /// </summary>
    public static class OperationResultEx {

        /// <summary>
        /// Validates responses
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="operations"></param>
        /// <param name="results"></param>
        /// <param name="diagnostics"></param>
        /// <param name="traceOnly"></param>
        public static void Validate(string operation,
            List<OperationResultModel> operations, IEnumerable<StatusCode> results,
            DiagnosticInfoCollection diagnostics, bool traceOnly) {
            Validate<object>(operation, operations, results, diagnostics, null, traceOnly);
        }

        /// <summary>
        /// Validates responses against requests
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="results"></param>
        /// <param name="diagnostics"></param>
        /// <param name="operations"></param>
        /// <param name="requested"></param>
        /// <param name="traceOnly"></param>
        public static void Validate<T>(string operation,
            List<OperationResultModel> operations, IEnumerable<StatusCode> results,
            DiagnosticInfoCollection diagnostics, IEnumerable<T> requested, bool traceOnly) {
            if (operations == null) {
                SessionClientEx.Validate(results, diagnostics, requested);
                return;
            }
            if (diagnostics == null) {
                diagnostics = new DiagnosticInfoCollection();
            }
            var resultsWithStatus = results?.ToList();
            if (resultsWithStatus == null || (resultsWithStatus.Count == 0 &&
                diagnostics.Count == 0)) {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError,
                    "The server returned no results or diagnostics information.");
            }
            // Add diagnostics
            var ids = requested?.ToArray() ?? new T[0];
            for (var index = resultsWithStatus.Count; index < diagnostics.Count; index++) {
                resultsWithStatus.Add(diagnostics[index] == null ?
                    StatusCodes.Good : StatusCodes.BadUnexpectedError);
            }
            operations.AddRange(results
                .Select((status, index) => new OperationResultModel {
                    Operation = index < ids.Length ? $"{operation}_{ids[index]}" : operation,
                    DiagnosticsInfo = index < diagnostics.Count ? diagnostics[index] : null,
                    StatusCode = status,
                    TraceOnly = traceOnly
                })
                .Where(o => o.StatusCode != StatusCodes.Good || o.DiagnosticsInfo != null));
        }
    }
}
