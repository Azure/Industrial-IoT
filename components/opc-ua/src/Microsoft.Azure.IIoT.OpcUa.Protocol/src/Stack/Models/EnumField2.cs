// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {

    /// <summary>
    /// Enum field extension class
    /// </summary>
    public class EnumField2 : EnumField {

        /// <summary>
        /// Symbolic name
        /// </summary>
        public string SymbolicName { get; set; }

        /// <inheritdoc/>
        public override void Decode(IDecoder decoder) {
            base.Decode(decoder);
            decoder.PushNamespace(Namespaces.OpcUa);
            SymbolicName = decoder.ReadString(nameof(SymbolicName));
            decoder.PopNamespace();
        }

        /// <inheritdoc/>
        public override void Encode(IEncoder encoder) {
            base.Encode(encoder);
            encoder.PushNamespace(Namespaces.OpcUa);
            encoder.WriteString(nameof(SymbolicName), SymbolicName);
            encoder.PopNamespace();
        }
    }
}
