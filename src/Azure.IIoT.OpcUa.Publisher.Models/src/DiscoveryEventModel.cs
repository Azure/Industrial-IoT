// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;

    /// <summary>
    /// One of n events with the discovered application info
    /// </summary>
    public sealed record class DiscoveryEventModel
    {
        /// <summary>
        /// Timestamp of the discovery sweep.
        /// </summary>
        public DateTimeOffset TimeStamp { get; set; }

        /// <summary>
        /// Index in the batch with same timestamp.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Discovered endpoint in form of endpoint registration
        /// </summary>
        public EndpointRegistrationModel? Registration { get; set; }

        /// <summary>
        /// Application to which this endpoint belongs
        /// </summary>
        public ApplicationInfoModel? Application { get; set; }

        /// <summary>
        /// Discovery result summary on last element
        /// </summary>
        public DiscoveryResultModel? Result { get; set; }
    }
}
