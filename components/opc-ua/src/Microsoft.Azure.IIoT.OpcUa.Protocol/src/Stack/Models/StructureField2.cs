// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {

    /// <summary>
    /// Structure field extension class
    /// </summary>
    public class StructureField2 : StructureField {

        /// <summary>
        /// Symbolic name
        /// </summary>
        public string SymbolicName { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public LocalizedText DisplayName { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public int Value { get; set; }

        /// <inheritdoc/>
        public override void Decode(IDecoder decoder) {
            base.Decode(decoder);

            decoder.PushNamespace(Namespaces.OpcUa);
            DisplayName = decoder.ReadLocalizedText(nameof(DisplayName));
            SymbolicName = decoder.ReadString(nameof(SymbolicName));
            decoder.PopNamespace();
        }

        /// <inheritdoc/>
        public override void Encode(IEncoder encoder) {
            base.Encode(encoder);

            encoder.PushNamespace(Namespaces.OpcUa);
            encoder.WriteLocalizedText(nameof(DisplayName), DisplayName);
            encoder.WriteString(nameof(SymbolicName), SymbolicName);
            encoder.PopNamespace();
        }
    }
}
