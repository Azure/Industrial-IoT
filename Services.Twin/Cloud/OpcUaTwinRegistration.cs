// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.Cloud {
    using Microsoft.Azure.IIoT.OpcTwin.Services.External.Models;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

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
        /// Edge supervisor that owns the twin.
        /// </summary>
        public virtual string SupervisorId { get; set; }

        /// <summary>
        /// Application id of twin
        /// </summary>
        public virtual string ApplicationId { get; set; }

        /// <summary>
        /// Whether registration is enabled or not
        /// </summary>
        public bool? IsDisabled { get; set; }

        /// <summary>
        /// Certificate hash
        /// </summary>
        public virtual string Thumbprint { get; set; }

        #endregion Twin Tags

        #region Twin Tags or reported properties

        /// <summary>
        /// Returns the public certificate presented by the application
        /// </summary>
        public Dictionary<string, string> Certificate { get; set; }

        #endregion Twin Tags or reported properties


        /// <summary>
        /// Convert twin to registration information.
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static OpcUaTwinRegistration ToRegistration(DeviceTwinModel twin) {
            if (twin == null || twin.Tags == null) {
                return null;
            }
            switch (twin.Tags.Get(nameof(DeviceType),
                twin.Properties.Reported.Get("type", "Unknown")).ToLowerInvariant()) {
                case "endpoint":
                    return OpcUaEndpointRegistration.FromTwin(twin, false);
                case "application":
                    return OpcUaApplicationRegistration.FromTwin(twin);
                case "supervisor":
                    return OpcUaSupervisorRegistration.FromTwin(twin);
            }
            return null;
        }

        protected string _deviceId;
    }
}
