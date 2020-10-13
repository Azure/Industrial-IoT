// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Abstractions {

    /// <summary>
    /// Common IoT Edge runtime environment variables.
    /// </summary>
    public static class IoTEdgeVariables {

        /// <summary> IoT Edge device id </summary>
        public const string IOTEDGE_DEVICEID = "IOTEDGE_DEVICEID";
        /// <summary> IoT Edge module id </summary>
        public const string IOTEDGE_MODULEID = "IOTEDGE_MODULEID";
        /// <summary> IoT Edge gateway hostname </summary>
        public const string IOTEDGE_GATEWAYHOSTNAME = "IOTEDGE_GATEWAYHOSTNAME";
        /// <summary> IoT Edge workload URI </summary>
        public const string IOTEDGE_WORKLOADURI = "IOTEDGE_WORKLOADURI";
        /// <summary> IoT Edge module generation id </summary>
        public const string IOTEDGE_MODULEGENERATIONID = "IOTEDGE_MODULEGENERATIONID";
        /// <summary> IoT Edge API version </summary>
        public const string IOTEDGE_APIVERSION = "IOTEDGE_APIVERSION";
        /// <summary> IoT Hub hostname </summary>
        public const string IOTEDGE_IOTHUBHOSTNAME = "IOTEDGE_IOTHUBHOSTNAME";
    }
}
