// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic
{
    using System;

    /// <summary>
    /// Event Arguments for DurationExceeded event
    /// </summary>
    public class DurationExceededEventArgs : EventArgs
    {
        /// <summary>
        /// The date and time, in UTC, of when the IoT Hub Message was enqueued in the Event Hub partition.
        /// </summary>
        public DateTime IotHubEnqueuedTime { get; }
        
        /// <summary>
        /// OPC UA Server Timestamp of MonitoredItems notification
        /// </summary>
        public DateTime TimeStamp { get; }

        public DurationExceededEventArgs(DateTime timeStamp, DateTime iotHubEnqueuedTime)
        {
            IotHubEnqueuedTime = iotHubEnqueuedTime;
            TimeStamp = timeStamp;
        }
    }
}