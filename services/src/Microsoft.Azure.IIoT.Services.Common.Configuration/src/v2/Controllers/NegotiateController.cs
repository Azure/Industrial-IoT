// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Configuration.v2.Controllers {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.AspNetCore.Mvc;
    using System;

    /// <summary>
    /// Services to negotiate endpoints
    /// </summary>
    [Produces(ContentMimeType.Json)]
    [ApiController]
    public class NegotiateController : ControllerBase {

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="auth"></param>
        /// <param name="endpoint"></param>
        public NegotiateController(IIdentityTokenGenerator auth, IEndpoint endpoint) {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        /// <summary>
        /// Negotiation method that signalr clients call to get access token.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPost("{hub}/negotiate")]
        [ProducesResponseType(typeof(JsonResult), 200)]
        [ProducesResponseType(typeof(BadRequestObjectResult), 400)]
        public IActionResult Index(string hub, string user) {
            if (string.IsNullOrEmpty(user)) {
                return BadRequest("User ID is null or empty.");
            }
            if (string.IsNullOrEmpty(hub)) {
                return BadRequest("Hub is null or empty.");
            }
            if (!hub.EqualsIgnoreCase(_endpoint.Resource)) {
                return BadRequest("Hub not found.");
            }
            return new JsonResult(new {
                url = _endpoint.EndpointUrl,
                accessToken = _auth.GenerateIdentityToken(user).Key
            });
        }

        private readonly IIdentityTokenGenerator _auth;
        private readonly IEndpoint _endpoint;
    }
}