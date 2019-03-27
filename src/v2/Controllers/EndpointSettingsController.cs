// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Supervisor {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Hub;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;

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
        /// User token to pass to server
        /// </summary>
        public JToken Credential {
            get => _credential;
            set => _credential = value;
        }

        /// <summary>
        /// Type of token
        /// </summary>
        public JToken CredentialType {
            get => JToken.FromObject(_credentialType);
            set => _credentialType = value?.ToObject<CredentialType>();
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
        public JToken SecurityMode {
            get => JToken.FromObject(_securityMode);
            set => _securityMode = value?.ToObject<SecurityMode>();
        }

        /// <summary>
        /// Endpoint certificate thumbprint to validate
        /// </summary>
        public Dictionary<string, string> ServerThumbprint {
            get => _serverThumbprint.EncodeAsDictionary();
            set => _serverThumbprint = value.DecodeAsByteArray();
        }

        /// <summary>
        /// Full client certificate to use when connecting to endpoint
        /// </summary>
        public Dictionary<string, string> ClientCertificate {
            get => _clientCertificate.EncodeAsDictionary();
            set => _clientCertificate = value.DecodeAsByteArray();
        }

        /// <summary>
        /// State of the endpoint
        /// </summary>
        public EndpointConnectivityState State { get; set; }

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="logger"></param>
        public EndpointSettingsController(ITwinServices twin, ILogger logger) {
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                    User =
                        _credentialType == IIoT.OpcUa.Registry.Models.CredentialType.None ?
                            null : new CredentialModel {
                                Value = _credential,
                                Type = _credentialType,
                            },
                    Url = _endpointUrl,
                    AlternativeUrls = _alternativeUrls == null ? null :
                        _alternativeUrls.DecodeAsList().ToHashSetSafe(),
                    ServerThumbprint = _serverThumbprint,
                    ClientCertificate = _clientCertificate
                });
        }

        private CredentialType? _credentialType;
        private JToken _credential;
        private string _endpointUrl;
        private string _securityPolicy;
        private SecurityMode? _securityMode;
        private byte[] _serverThumbprint;
        private byte[] _clientCertificate;
        private Dictionary<string, string> _alternativeUrls;
        private readonly ITwinServices _twin;
        private readonly ILogger _logger;
    }
}
