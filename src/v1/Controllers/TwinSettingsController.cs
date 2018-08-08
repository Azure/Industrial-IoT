// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework;
    using System;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Twin settings controller
    /// </summary>
    [Version(1)]
    public class TwinSettingsController : ISettingsController {

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        public string EndpointUrl {
            get => _endpointUrl;
            set => _endpointUrl = string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// User name to use
        /// </summary>
        public string User {
            get => _user;
            set => _user = string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// User token to pass to server
        /// </summary>
        public JToken Token {
            get => _token;
            set => _token = value;
        }

        /// <summary>
        /// Type of token
        /// </summary>
        public JToken TokenType {
            get => JToken.FromObject(_tokenType);
            set => _tokenType = value?.ToObject<TokenType>();
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
        /// Endpoint certificate to validate against
        /// </summary>
        public Dictionary<string, string> Validation {
            get => _validation.EncodeAsDictionary();
            set => _validation = value.DecodeAsByteArray();
        }

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="logger"></param>
        public TwinSettingsController(IPublisherServices twin, ILogger logger) {
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
                    Authentication =
                        _tokenType == OpcUa.Models.TokenType.None ? null :
                        new AuthenticationModel {
                            User = _user,
                            Token = _token,
                            TokenType = _tokenType,
                        },
                    Url = _endpointUrl,
                    Validation = _validation
                });
        }

        private TokenType? _tokenType;
        private JToken _token;
        private string _user;
        private string _endpointUrl;
        private string _securityPolicy;
        private SecurityMode? _securityMode;
        private byte[] _validation;

        private readonly IPublisherServices _twin;
        private readonly ILogger _logger;
    }
}
