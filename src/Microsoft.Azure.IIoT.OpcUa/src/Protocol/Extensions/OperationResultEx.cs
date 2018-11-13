// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Opc.Ua;
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
        public static List<OperationResult> ToOperationResults(this ServiceResultModel result,
            DiagnosticsModel config, ServiceMessageContext context) {

            if (result?.Diagnostics == null) {
                return null;
            }
            var root = kDiagnosticsProperty;
            switch (config?.Level ?? Twin.Models.DiagnosticsLevel.Status) {
                case Twin.Models.DiagnosticsLevel.Diagnostics:
                case Twin.Models.DiagnosticsLevel.Verbose:
                    using (var decoder = new JsonDecoderEx(context, result.Diagnostics.CreateReader())) {
                        var array = decoder.ReadEncodeableArray(root, typeof(OperationResult));
                        var results = array.OfType<OperationResult>().ToList();
                        if (results.Count == 0) {
                            return null;
                        }
                        return results;
                    }
                case Twin.Models.DiagnosticsLevel.Status:
                    // TODO
                    break;
                case Twin.Models.DiagnosticsLevel.Operations:
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
        public static ServiceResultModel ToServiceModel(this List<OperationResult> diagnostics,
            DiagnosticsModel config, ServiceMessageContext context) {
            if ((diagnostics?.Count ?? 0) == 0) {
                return null; // All well
            }
            return new ServiceResultModel {
                // The last operation result is the one that caused the service to fail.
                StatusCode = diagnostics.Last().StatusCode.Code,
                ErrorMessage = diagnostics.Last().DiagnosticsInfo?.AdditionalInfo ??
                    StatusCode.LookupSymbolicId(diagnostics.Last().StatusCode.CodeBits),
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
        public static void Validate(string operation,
            List<OperationResult> operations, IEnumerable<StatusCode> results,
            DiagnosticInfoCollection diagnostics) {
            Validate<object>(operation, operations, results, diagnostics, null);
        }

        /// <summary>
        /// Validates responses against requests
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="results"></param>
        /// <param name="diagnostics"></param>
        /// <param name="requested"></param>
        /// <param name="operations"></param>
        public static void Validate<T>(string operation,
            List<OperationResult> operations, IEnumerable<StatusCode> results,
            DiagnosticInfoCollection diagnostics, IEnumerable<T> requested) {
            if (operations == null) {
                SessionClientEx.Validate(results, diagnostics, requested);
                return;
            }
            var statusCodes = results?.ToList();
            if (statusCodes == null || (statusCodes.Count == 0 && diagnostics.Count == 0)) {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError,
                    "The server returned no results or diagnostics information.");
            }
            // Add diagnostics
            var ids = requested?.ToArray() ?? new T[0];
            for (var index = statusCodes.Count; index < diagnostics.Count; index++) {
                statusCodes.Add(diagnostics[index] == null ?
                    StatusCodes.Good : StatusCodes.BadUnexpectedError);
            }
            operations.AddRange(results
                .Select((status, index) => new OperationResult {
                    Operation = index < ids.Length ? $"{operation}_{ids[index]}" : operation,
                    DiagnosticsInfo = index < diagnostics.Count ? diagnostics[index] : null,
                    StatusCode = status
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
        private static JToken ToJson(this List<OperationResult> results, DiagnosticsModel config,
            ServiceMessageContext context) {
            var level = config?.Level ?? Twin.Models.DiagnosticsLevel.Status;
            if (level == Twin.Models.DiagnosticsLevel.None) {
                return null;
            }
            using (var stream = new MemoryStream()) {
                var root = kDiagnosticsProperty;
                using (var encoder = new JsonEncoderEx(context, stream) {
                    UseMicrosoftVariant = true,
                    IgnoreNullValues = true
                }) {
                    switch (level) {
                        case Twin.Models.DiagnosticsLevel.Diagnostics:
                        case Twin.Models.DiagnosticsLevel.Verbose:
                            encoder.WriteEncodeableArray(root,
                                results.Cast<IEncodeable>().ToList(),
                                    typeof(OperationResult));
                            break;
                        case Twin.Models.DiagnosticsLevel.Operations:
                            var codes = results
                                .GroupBy(d => d.StatusCode.CodeBits);
                            root = null;
                            foreach (var code in codes) {
                                encoder.WriteStringArray(StatusCode.LookupSymbolicId(code.Key),
                                    code.Select(c => c.Operation).ToArray());
                            }
                            break;
                        case Twin.Models.DiagnosticsLevel.Status:
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
