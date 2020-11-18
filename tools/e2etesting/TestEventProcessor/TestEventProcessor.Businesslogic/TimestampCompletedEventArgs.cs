// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic
{
    using System;

    /// <summary>
    /// Event Arguments for Timestamp completed event
    /// </summary>
    public class TimestampCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Timestamp of value changes
        /// </summary>
        public string Timestamp { get; }

        /// <summary>
        /// OPC UA Value Changes received for Timestamp
        /// </summary>
        public int NumberOfValueChanges { get; }

        public TimestampCompletedEventArgs(string timestamp, int numberOfValueChanges)
        {
            Timestamp = timestamp;
            NumberOfValueChanges = numberOfValueChanges;
        }
    }
}