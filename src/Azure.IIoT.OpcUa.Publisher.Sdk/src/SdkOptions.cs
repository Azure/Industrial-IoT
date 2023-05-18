// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk
{
    using System;

    /// <summary>
    /// Configuration for IoT Edge Opc Publisher sdk
    /// </summary>
    public sealed class SdkOptions
    {
        /// <summary>
        /// Edge target path. This is the mount path of the
        /// publisher'smethod router which using the publisher
        /// module's command line arguments and is defaulting to
        /// <code>
        /// {PublisherId}/methods
        /// </code>
        /// or the device and module identifier in the form of
        /// <code>
        /// {deviceId}_module_{moduleId}
        /// </code>.
        /// </summary>
        public string? Target { get; set; }

        /// <summary>
        /// Call timeout
        /// </summary>
        public TimeSpan? Timeout { get; set; }
    }
}
