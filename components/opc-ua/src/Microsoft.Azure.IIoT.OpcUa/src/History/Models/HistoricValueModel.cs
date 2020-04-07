// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System;

    /// <summary>
    /// Historic value
    /// </summary>
    public class HistoricValueModel {

        /// <summary>
        /// The value of data value.
        /// </summary>
        public VariantValue Value { get; set; }

        /// <summary>
        /// The status code associated with the value.
        /// </summary>
        public uint? StatusCode { get; set; }

        /// <summary>
        /// The source timestamp associated with the value.
        /// </summary>
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Additional resolution for the source timestamp.
        /// </summary>
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// The server timestamp associated with the value.
        /// </summary>
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Additional resolution for the server timestamp.
        /// </summary>
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// modification information when reading modifications.
        /// </summary>
        public ModificationInfoModel ModificationInfo { get; set; }
    }
}
