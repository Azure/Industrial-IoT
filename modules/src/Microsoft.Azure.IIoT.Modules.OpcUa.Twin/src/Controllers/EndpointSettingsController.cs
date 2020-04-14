// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Hub;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Endpoint settings controller
    /// </summary>
    [Version(1)]
    [Version(2)]
    public class EndpointSettingsController : ISettingsController {

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        public string EndpointUrl {
            get => _endpointUrl;
            set => _endpointUrl = string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        public Dictionary<string, string> AlternativeUrls {
            get => _alternativeUrls;
            set => _alternativeUrls = value;
        }

        /// <summary>
        /// Endpoint security policy to use.
        /// </summary>
        public string SecurityPolicy {
            get => _securityPolicy;
            set => _securityPolicy = string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// Security mode to use for communication
        /// </summary>
        public SecurityMode? SecurityMode {
            get => _securityMode;
            set => _securityMode = value;
        }

        /// <summary>
        /// Endpoint certificate (Legacy)
        /// </summary>
        public Dictionary<string, string> Certificate {
            get => _certificate.EncodeAsDictionary();
            set => _certificate = value.DecodeAsByteArray();
        }

        /// <summary>
        /// Endpoint certificate thumbprint to validate
        /// </summary>
        public string Thumbprint {
            get => string.IsNullOrEmpty(_thumbprint) ? null : _thumbprint;
            set => _thumbprint = string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// State of the endpoint
        /// </summary>
        public EndpointConnectivityState State {
            get => _twin.State;
            set { /* Only reporting */ }
        }

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twin"></param>
        public EndpointSettingsController(ITwinServices twin) {
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
        }

        /// <summary>
        /// Apply endpoint update
        /// </summary>
        /// <returns></returns>
        public Task ApplyAsync() {
            return _twin.SetEndpointAsync(
                new EndpointModel {
                    SecurityMode = _securityMode,
                    SecurityPolicy = _securityPolicy,
                    Url = _endpointUrl,
                    AlternativeUrls = _alternativeUrls?.DecodeAsList().ToHashSetSafe(),
                    Certificate = _thumbprint ?? _certificate?.ToThumbprint()
                });
        }

        private string _endpointUrl;
        private string _securityPolicy;
        private byte[] _certificate;
        private string _thumbprint;
#pragma warning disable IDE0032 // Use auto property
        private SecurityMode? _securityMode;
        private Dictionary<string, string> _alternativeUrls;
#pragma warning restore IDE0032 // Use auto property
        private readonly ITwinServices _twin;
    }
}
