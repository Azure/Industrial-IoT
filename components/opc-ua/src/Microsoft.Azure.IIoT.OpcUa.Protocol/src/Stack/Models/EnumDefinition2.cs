// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {

    /// <summary>
    /// Enumeration definition extension
    /// </summary>
    public class EnumDefinition2 : EnumDefinition {

        /// <summary>
        /// Symbold name
        /// </summary>
        public string SymbolicName { get; set; }

        /// <summary>
        /// Enumeration name
        /// </summary>
        public QualifiedName Name { get; set; }

        /// <summary>
        /// Description of the enumeration
        /// </summary>
        public LocalizedText Description { get; internal set; }

        /// <summary>
        /// Whether the enum is an option set
        /// </summary>
        public bool IsOptionSet { get; set; }

        /// <summary>
        /// Base type string
        /// </summary>
        public string BaseType { get; internal set; }

        /// <inheritdoc/>
        public override void Decode(IDecoder decoder) {
            base.Decode(decoder);
            decoder.PushNamespace(Namespaces.OpcUa);
            Name = decoder.ReadQualifiedName(nameof(Name));
            Description = decoder.ReadLocalizedText(nameof(Description));
            IsOptionSet = decoder.ReadBoolean(nameof(IsOptionSet));
            BaseType = decoder.ReadString(nameof(BaseType));
            SymbolicName = decoder.ReadString(nameof(SymbolicName));
            decoder.PopNamespace();
        }

        /// <inheritdoc/>
        public override void Encode(IEncoder encoder) {
            base.Encode(encoder);
            encoder.PushNamespace(Namespaces.OpcUa);
            encoder.WriteQualifiedName(nameof(Name), Name);
            encoder.WriteLocalizedText(nameof(Description), Description);
            encoder.WriteBoolean(nameof(IsOptionSet), IsOptionSet);
            encoder.WriteString(nameof(BaseType), BaseType);
            encoder.WriteString(nameof(SymbolicName), SymbolicName);
            encoder.PopNamespace();
        }
    }
}
