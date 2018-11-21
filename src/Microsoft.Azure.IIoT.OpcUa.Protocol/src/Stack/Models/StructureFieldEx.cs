// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {
    using Opc.Ua;

    /// <summary>
    /// Structure field extension class
    /// </summary>
    public class StructureFieldEx : StructureField {

        /// <summary>
        /// Symbolic name
        /// </summary>
        public string SymbolicName {
            get; set;
        }


        /// <summary>
        /// Display name
        /// </summary>
        public LocalizedText DisplayName {
            get;
            internal set;
        }

        /// <summary>
        /// Value
        /// </summary>
        public int Value {
            get;
            internal set;
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public override void Decode(IDecoder decoder) {
            base.Decode(decoder);

            decoder.PushNamespace(Namespaces.OpcUa);
            SymbolicName = decoder.ReadString("SymbolicName");
            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public override void Encode(IEncoder encoder) {
            base.Encode(encoder);

            encoder.PushNamespace(Namespaces.OpcUa);
            encoder.WriteString("SymbolicName", SymbolicName);
            encoder.PopNamespace();
        }
    }
}
