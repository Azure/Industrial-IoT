// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic
{
    using System;

    /// <summary>
    /// Event Arguments for Missing Timestamp event
    /// </summary>
    public class MissingTimestampEventArgs : EventArgs
    {
        /// <summary>
        /// Timestamp that were expected
        /// </summary>
        public string ExpectedTimestamp { get; }

        /// <summary>
        /// Predecessor Timestamp
        /// </summary>
        public string OlderTimestamp { get; }

        /// <summary>
        /// Successor Timestamp
        /// </summary>
        public string NewerTimestamp { get; }

        public MissingTimestampEventArgs(string expectedTimestamp, string olderTimestamp, string newerTimestamp)
        {
            ExpectedTimestamp = expectedTimestamp;
            OlderTimestamp = olderTimestamp;
            NewerTimestamp = newerTimestamp;
        }
    }
}