// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk
{
    /// <summary>
    /// Configuration for IoT Edge Opc Publisher api
    /// </summary>
    public interface ISdkConfig
    {
        /// <summary>
        /// Edge target
        /// </summary>
        string Target { get; }
    }
}
