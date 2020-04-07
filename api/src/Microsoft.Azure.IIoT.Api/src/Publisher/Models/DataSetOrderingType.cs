// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Ordering model
    /// </summary>
    [DataContract]
    public enum DataSetOrderingType {

        /// <summary>
        /// Ascending writer id
        /// </summary>
        [EnumMember]
        AscendingWriterId = 1,

        /// <summary>
        /// Single
        /// </summary>
        [EnumMember]
        AscendingWriterIdSingle = 2,
    }
}
