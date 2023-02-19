// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {

    /// <summary>
    /// Samples message extension flags
    /// </summary>
    public static class JsonDataSetMessageContentMaskEx {

        /// <summary>
        /// Extra fields included
        /// </summary>
        public const JsonDataSetMessageContentMask ExtensionFields = (JsonDataSetMessageContentMask)0x02000000;

        /// <summary>
        /// Node id included
        /// </summary>
        public const JsonDataSetMessageContentMask NodeId = (JsonDataSetMessageContentMask)0x10000000;

        /// <summary>
        /// Endpoint url included
        /// </summary>
        public const JsonDataSetMessageContentMask EndpointUrl = (JsonDataSetMessageContentMask)0x20000000;

        /// <summary>
        /// Application uri
        /// </summary>
        public const JsonDataSetMessageContentMask ApplicationUri = (JsonDataSetMessageContentMask)0x40000000;

        /// <summary>
        /// Display name included
        /// </summary>
        public const JsonDataSetMessageContentMask DisplayName = (JsonDataSetMessageContentMask)0x80000000;
    }
}