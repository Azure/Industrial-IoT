﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    /// <summary>
    /// Constants for diagnostics
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// Connection group identifier tag
        /// </summary>
        public const string ConnectionGroupTag = "connectionGroupId";

        /// <summary>
        /// Dataset Writer identifier tag
        /// </summary>
        public const string DataSetWriterIdTag = "dataSetWriterId";

        /// <summary>
        /// Default dataset writer id
        /// </summary>
        public const string DefaultDataSetWriterName = "<<UnknownDataSet>>";

        /// <summary>
        /// Writer group identifier tag
        /// </summary>
        public const string WriterGroupIdTag = "writerGroupId";

        /// <summary>
        /// Default writer group id
        /// </summary>
        public const string DefaultWriterGroupId = "<<UnknownWriterGroup>>";

        /// <summary>
        /// Publisher identifier tag
        /// </summary>
        public const string PublisherIdTag = "publisherId";

        /// <summary>
        /// Default publisher id
        /// </summary>
        public const string DefaultPublisherId = "<<UnknownPublisher>>";

        /// <summary>
        /// Site identifier tag
        /// </summary>
        public const string SiteIdTag = "siteId";

        /// <summary>
        /// Default Site id
        /// </summary>
        public const string DefaultSite = "<<UnknownSite>>";

        /// <summary>
        /// Timestamp tag (start time)
        /// </summary>
        public const string TimeStampTag = "timestamp_utc";
    }
}
