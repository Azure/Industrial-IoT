// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Opc.Ua;
    using System.CodeDom.Compiler;
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Samples message extension flags
    /// </summary>
    public static class JsonDataSetMessageContentMaskEx
    {
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

    /// <summary>
    /// Extensions for fields
    /// </summary>
    public static class DataSetFieldContentMaskEx
    {
        /// <summary>
        /// Degrade a single data set field to just value instead of writing key value dictionary object
        /// </summary>
        public const DataSetFieldContentMask SingleFieldDegradeToValue = (DataSetFieldContentMask)64;
    }

    /// <summary>
    /// Avro extension
    /// </summary>
    [Flags]
    public enum AvroNetworkMessageContentMask
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,

        /// <summary>
        /// Network message header
        /// </summary>
        NetworkMessageHeader = 1,

        /// <summary>
        /// DataSet message header
        /// </summary>
        DataSetMessageHeader = 2,

        /// <summary>
        /// Single
        /// </summary>
        SingleDataSetMessage = 4
    }
}
