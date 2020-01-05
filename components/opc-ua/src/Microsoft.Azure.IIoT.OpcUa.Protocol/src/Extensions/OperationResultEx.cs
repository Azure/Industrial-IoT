// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using Opc.Ua.Encoders;
    using Opc.Ua.Client;
    using Newtonsoft.Json.Linq;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Operation result extensions
    /// </summary>
    public static class OperationResultEx {

        /// <summary>
        /// Convert from service result to diagnostics info
        /// </summary>
        /// <param name="result"></param>
        /// <param name="config"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static List<OperationResultModel> ToOperationResults(this ServiceResultModel result,
            DiagnosticsModel config, ServiceMessageContext context) {

            if (result?.Diagnostics == null) {
                return null;
            }
            var root = kDiagnosticsProperty;
            switch (config?.Level ?? Core.Models.DiagnosticsLevel.Status) {
                case Core.Models.DiagnosticsLevel.Diagnostics:
                case Core.Models.DiagnosticsLevel.Verbose:
                    using (var decoder = new JsonDecoderEx(result.Diagnostics.CreateReader(), context)) {
                        var results = decoder.ReadEncodeableArray<OperationResultModel>(root).ToList();
                        if (results.Count == 0) {
                            return null;
                        }
                        return results;
                    }
                case Core.Models.DiagnosticsLevel.Status:
                    // TODO
                    break;
                case Core.Models.DiagnosticsLevel.Operations:
                    // TODO
                    break;
                default:
                    break;
            }
            return null;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <param name="config"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ServiceResultModel ToServiceModel(this List<OperationResultModel> diagnostics,
            DiagnosticsModel config, ServiceMessageContext context) {
            if ((diagnostics?.Count ?? 0) == 0) {
                return null; // All well
            }
            var result = diagnostics.LastOrDefault(d => !d.TraceOnly);
            var statusCode = result?.StatusCode;
            return new ServiceResultModel {
                // The last operation result is the one that caused the service to fail.
                StatusCode = statusCode?.Code,
                ErrorMessage = result?.DiagnosticsInfo?.AdditionalInfo ?? (statusCode == null ?
                    null : StatusCode.LookupSymbolicId(statusCode.Value.CodeBits)),
                Diagnostics = diagnostics.ToJson(config, context)
            };
        }

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

        /// <summary>
        /// Convert operation results to json
        /// </summary>
        /// <param name="results"></param>
        /// <param name="config"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static JToken ToJson(this List<OperationResultModel> results, DiagnosticsModel config,
            ServiceMessageContext context) {
            var level = config?.Level ?? Core.Models.DiagnosticsLevel.Status;
            if (level == Core.Models.DiagnosticsLevel.None) {
                return null;
            }
            using (var stream = new MemoryStream()) {
                var root = kDiagnosticsProperty;
                using (var encoder = new JsonEncoderEx(stream, context) {
                    UseAdvancedEncoding = true,
                    IgnoreDefaultValues = true
                }) {
                    switch (level) {
                        case Core.Models.DiagnosticsLevel.Diagnostics:
                        case Core.Models.DiagnosticsLevel.Verbose:
                            encoder.WriteEncodeableArray(root, results);
                            break;
                        case Core.Models.DiagnosticsLevel.Operations:
                            var codes = results
                                .GroupBy(d => d.StatusCode.CodeBits);
                            root = null;
                            foreach (var code in codes) {
                                encoder.WriteStringArray(StatusCode.LookupSymbolicId(code.Key),
                                    code.Select(c => c.Operation).ToArray());
                            }
                            break;
                        case Core.Models.DiagnosticsLevel.Status:
                            var statusCodes = results
                                .Select(d => StatusCode.LookupSymbolicId(d.StatusCode.CodeBits))
                                .Where(s => !string.IsNullOrEmpty(s))
                                .Distinct();
                            if (!statusCodes.Any()) {
                                return null;
                            }
                            encoder.WriteStringArray(root, statusCodes.ToArray());
                            break;
                    }
                }
                var o = JObject.Parse(Encoding.UTF8.GetString(stream.ToArray()));
                return root != null ? o.Property(root).Value : o;
            }
        }

        private const string kDiagnosticsProperty = "diagnostics";
    }
}
