// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Base twin registration
    /// </summary>
    public abstract class OpcUaTwinRegistration {

        /// <summary>
        /// Device id for registration
        /// </summary>
        public virtual string DeviceId {
            get => _deviceId;
            set => _deviceId = value;
        }

        /// <summary>
        /// Etag id
        /// </summary>
        public string Etag { get; set; }

        #region Twin Tags

        /// <summary>
        /// Registration type
        /// </summary>
        public abstract string DeviceType { get; }

        /// <summary>
        /// Application id of twin
        /// </summary>
        public virtual string ApplicationId { get; set; }

        /// <summary>
        /// Whether registration is enabled or not
        /// </summary>
        public bool? IsEnabled { get; set; }

        /// <summary>
        /// Edge supervisor that owns the twin.
        /// </summary>
        public string SupervisorId { get; set; }

        #endregion Twin Tags

        /// <summary>
        /// Convert twin to registration information.
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static OpcUaTwinRegistration ToRegistration(DeviceTwinModel twin) {
            if (twin == null || twin.Tags == null) {
                return null;
            }
            switch (twin.Tags.Get(nameof(DeviceType), "unknown")) {
                case "Endpoint":
                    return OpcUaEndpointRegistration.FromTwin(twin, false);
                case "Application":
                    return OpcUaApplicationRegistration.FromTwin(twin);
            }
            return null;
        }

        protected string _deviceId;
    }
}
