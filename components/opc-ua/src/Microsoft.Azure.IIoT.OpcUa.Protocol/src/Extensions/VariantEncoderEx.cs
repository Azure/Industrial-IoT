// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using Opc.Ua.Encoders;
    using Opc.Ua.Extensions;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Variant encoder extensions
    /// </summary>
    public static class VariantEncoderEx {

        /// <summary>
        /// Format variant as string
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder encoder, Variant value) {
            return encoder.Encode(value, out var tmp);
        }

        /// <summary>
        /// Decode with data type as string
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Variant Decode(this IVariantEncoder encoder, VariantValue value,
            string type) {
            return encoder.Decode(value, string.IsNullOrEmpty(type) ? BuiltInType.Null :
                TypeInfo.GetBuiltInType(type.ToNodeId(encoder.Context)));
        }

        /// <summary>
        /// Convert from diagnostics info to service result
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <param name="code"></param>
        /// <param name="operation"></param>
        /// <param name="codec"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ServiceResultModel Encode(this IVariantEncoder codec,
            DiagnosticInfo diagnostics,
            StatusCode code, string operation, DiagnosticsModel config) {
            if (code == StatusCodes.Good) {
                return null;
            }
            return codec.Encode(new List<OperationResultModel> {
                new OperationResultModel {
                    DiagnosticsInfo = diagnostics,
                    Operation = operation,
                    StatusCode = code
                }
            }, config);
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <param name="codec"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ServiceResultModel Encode(this IVariantEncoder codec,
            List<OperationResultModel> diagnostics, DiagnosticsModel config) {
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
                Diagnostics = codec.Write(diagnostics, config)
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="diagnosticsInfo"></param>
        /// <param name="codec"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ServiceResultModel Encode(this IVariantEncoder codec,
            StatusCode? statusCode, DiagnosticInfo diagnosticsInfo = null,
            DiagnosticsModel config = null) {
            if ((statusCode?.Code ?? StatusCodes.Good) == StatusCodes.Good) {
                return null; // All well
            }
            return new ServiceResultModel {
                // The last operation result is the one that caused the service to fail.
                StatusCode = statusCode?.Code,
                ErrorMessage = diagnosticsInfo?.AdditionalInfo ?? (statusCode == null ?
                    null : StatusCode.LookupSymbolicId(statusCode.Value.CodeBits)),
                Diagnostics = config == null ? null : codec.Write(
                    new List<OperationResultModel> {
                        new OperationResultModel {
                            DiagnosticsInfo = diagnosticsInfo,
                            StatusCode = statusCode.Value
                        }
                    }, config)
            };
        }

        /// <summary>
        /// Convert operation results to json
        /// </summary>
        /// <param name="results"></param>
        /// <param name="codec"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private static VariantValue Write(this IVariantEncoder codec,
            List<OperationResultModel> results, DiagnosticsModel config) {
            var level = config?.Level ?? Core.Models.DiagnosticsLevel.Status;
            if (level == Core.Models.DiagnosticsLevel.None) {
                return null;
            }
            using (var stream = new MemoryStream()) {
                var root = kDiagnosticsProperty;
                using (var encoder = new JsonEncoderEx(stream, codec.Context) {
                    UseAdvancedEncoding = true,
                    IgnoreDefaultValues = true
                }) {
                    switch (level) {
                        case Core.Models.DiagnosticsLevel.Diagnostics:
                        case Core.Models.DiagnosticsLevel.Verbose:
                            encoder.WriteEncodeableArray(root, results.ToArray(), typeof(OperationResultModel));
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
                var o = codec.Serializer.Parse(stream.ToArray());
                return root != null ? o[root] : o;
            }
        }

        private const string kDiagnosticsProperty = "diagnostics";
    }
}
