// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Opc.Ua;

    /// <summary>
    /// A result of a client operation
    /// </summary>
    public class OperationResultModel : IEncodeable {

        /// <summary>
        /// Operation
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// Status code of operation
        /// </summary>
        public StatusCode StatusCode { get; set; }

        /// <summary>
        /// Diagnostics information
        /// </summary>
        public DiagnosticInfo DiagnosticsInfo { get; set; }

        /// <summary>
        /// Operation is just for tracing
        /// </summary>
        public bool TraceOnly { get; set; }

        /// <inheritdoc/>
        public void Decode(IDecoder decoder) {
            Operation = decoder.ReadString("Operation");
            StatusCode = decoder.ReadStatusCode("StatusCode");
            DiagnosticsInfo = decoder.ReadDiagnosticInfo("DiagnosticsInfo");
            TraceOnly = decoder.ReadBoolean("TraceOnly");
        }

        /// <inheritdoc/>
        public void Encode(IEncoder encoder) {
            encoder.WriteString("Operation", Operation);
            encoder.WriteStatusCode("StatusCode", StatusCode);
            encoder.WriteDiagnosticInfo("DiagnosticsInfo", DiagnosticsInfo);
            encoder.WriteBoolean("TraceOnly", TraceOnly);
        }

        /// <inheritdoc/>
        public bool IsEqual(IEncodeable encodeable) {
            if (ReferenceEquals(this, encodeable)) {
                return true;
            }
            return (encodeable is OperationResultModel value) &&
                Utils.IsEqual(Operation, value.Operation) &&
                Utils.IsEqual(DiagnosticsInfo, value.DiagnosticsInfo) &&
                Utils.IsEqual(StatusCode, value.StatusCode);
        }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId => null;

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId => null;

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId => null;
    }
}
