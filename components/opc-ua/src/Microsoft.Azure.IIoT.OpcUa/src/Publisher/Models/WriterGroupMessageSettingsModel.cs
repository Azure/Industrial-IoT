// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Writer group message configuration
    /// </summary>
    public class WriterGroupMessageSettingsModel {

        /// <summary>
        /// Network message content
        /// </summary>
        public NetworkMessageContentMask? NetworkMessageContentMask { get; set; }

        /// <summary>
        /// Group version
        /// </summary>
        public uint? GroupVersion { get; set; }

        /// <summary>
        /// Uadp dataset ordering
        /// </summary>
        public DataSetOrderingType? DataSetOrdering { get; set; }

        /// <summary>
        /// Uadp Sampling offset
        /// </summary>
        public double? SamplingOffset { get; set; }

        /// <summary>
        /// Publishing offset for uadp messages
        /// </summary>
        public List<double> PublishingOffset { get; set; }

        /// <summary>
        /// Max messages per publish
        /// </summary>
        public uint? MaxMessagesPerPublish { get; set; }
    }
}