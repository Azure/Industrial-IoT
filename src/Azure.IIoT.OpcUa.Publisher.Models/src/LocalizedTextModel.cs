// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Localized text.
    /// </summary>
    [DataContract]
    public sealed record class LocalizedTextModel
    {
        /// <summary>
        /// Text
        /// </summary>
        [DataMember(Name = "text", Order = 0)]
        public required string Text { get; set; }

        /// <summary>
        /// Locale or null for default locale
        /// </summary>
        [DataMember(Name = "locale", Order = 1,
            EmitDefaultValue = false)]
        public string? Locale { get; set; }
    }
}
