// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// diagnostics info extensions
    /// </summary>
    public static class DiagnosticInfoEx {

        /// <summary>
        /// Convert from diagnostics info to service result
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <param name="code"></param>
        /// <param name="operation"></param>
        /// <param name="config"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ServiceResultModel ToServiceModel(this DiagnosticInfo diagnostics,
            StatusCode code, string operation, DiagnosticsModel config, ServiceMessageContext context) {
            if (code == StatusCodes.Good) {
                return null;
            }
            return new List<OperationResultModel> {
                new OperationResultModel {
                    DiagnosticsInfo = diagnostics,
                    Operation = operation,
                    StatusCode = code
                }
            }.ToServiceModel(config, context);
        }

        /// <summary>
        /// Convert from service result to diagnostics info
        /// </summary>
        /// <param name="result"></param>
        /// <param name="config"></param>
        /// <param name="context"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static DiagnosticInfo ToDiagnosticsInfo(this ServiceResultModel result,
            DiagnosticsModel config, ServiceMessageContext context, out StatusCode code) {
            if (result == null) {
                code = StatusCodes.Good;
                return null;
            }
            code = new StatusCode(result.StatusCode ?? StatusCodes.Good);
            var results = result.ToOperationResults(config, context);
            return results?.LastOrDefault()?.DiagnosticsInfo;
        }
    }
}
