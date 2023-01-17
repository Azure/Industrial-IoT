// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using Opc.Ua;

    /// <summary>
    /// Missing data set flags. TODO: Remove when moving to 1.05
    /// </summary>
    public static class JsonDataSetMessageContentMask2 {

        /// <summary>
        /// Missing definition in stack (1.05)
        /// </summary>
        public const JsonDataSetMessageContentMask DataSetWriterName = (JsonDataSetMessageContentMask)64;

        /// <summary>
        /// Missing definition in stack (1.05)
        /// </summary>
        public const JsonDataSetMessageContentMask ReversibleFieldEncoding = (JsonDataSetMessageContentMask)128;
    }
}