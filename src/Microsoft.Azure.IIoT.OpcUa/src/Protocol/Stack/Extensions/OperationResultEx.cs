// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {
    using Opc.Ua.Encoders;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
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
        /// Convert to json token
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <param name="config"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static JToken ToJToken(this List<OperationResult> diagnostics,
            DiagnosticsModel config, ServiceMessageContext context) {
            var level = config?.Level ?? DiagnosticsLevel.Status;
            if ((diagnostics?.Count ?? 0) == 0 || level == DiagnosticsLevel.None) {
                return null;
            }
            using (var stream = new MemoryStream()) {
                var root = nameof(diagnostics);
                using (var encoder = new JsonEncoderEx(context, stream) {
                    UseMicrosoftVariant = true,
                    IgnoreNullValues = true
                }) {
                    switch (level) {
                        case DiagnosticsLevel.Operations:
                            var codes = diagnostics
                                .GroupBy(d => d.StatusCode.CodeBits);
                            root = null;
                            foreach (var code in codes) {
                                encoder.WriteStringArray(StatusCode.LookupSymbolicId(code.Key),
                                    code.Select(c => c.Operation).ToArray());
                            }
                            break;
                        case DiagnosticsLevel.Diagnostics:
                        case DiagnosticsLevel.Verbose:
                            encoder.WriteEncodeableArray(root,
                                diagnostics.Cast<IEncodeable>().ToList(),
                                typeof(OperationResult));
                            break;
                        default:
                            var statusCodes = diagnostics
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
                if (root != null) {
                    return o.Property(root).Value;
                }
                return o;
            }
        }

        /// <summary>
        /// Convert from json token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="config"></param>
        /// <param name="context"></param>
        /// <param name="code"></param>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        public static void ToDiagnosticsInfo(JToken token,
            DiagnosticsModel config, ServiceMessageContext context,
            out StatusCode code, out DiagnosticInfo diagnostics) {

            code = StatusCodes.Good;
            diagnostics = null;
            if ((token?.Type ?? JTokenType.Null) == JTokenType.Null) {
                return;
            }
            var level = config?.Level ?? DiagnosticsLevel.Status;
            if (level != DiagnosticsLevel.Diagnostics &&
                level != DiagnosticsLevel.Verbose) {
                return;
            }

            var root = nameof(diagnostics);
            using (var decoder = new JsonDecoderEx(context, token.CreateReader())) {
                var array = decoder.ReadEncodeableArray(root, typeof(OperationResult));
                var results = array.OfType<OperationResult>().ToList();
                if (results.Count == 0) {
                    return;
                }
                code = results[0].StatusCode;
                diagnostics = results[0].DiagnosticsInfo;
            }
        }
    }
}
