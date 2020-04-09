// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Onboarding.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.Onboarding.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.Onboarding.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Handle discovery events and onboard applications
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/discovery")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanOnboard)]
    [ApiController]
    public class DiscoveryController : ControllerBase {

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="onboarding"></param>
        public DiscoveryController(IDiscoveryResultProcessor onboarding) {
            _onboarding = onboarding;
        }

        /// <summary>
        /// Process discovery results
        /// </summary>
        /// <remarks>
        /// Bulk processes discovery events and onboards new entities
        /// to the application registry
        /// </remarks>
        /// <param name="discovererId"></param>
        /// <param name="model">Discovery event list model</param>
        /// <returns></returns>
        [HttpPost]
        public async Task ProcessDiscoveryResultsAsync(
            [FromQuery] [Required] string discovererId,
            [FromBody] [Required] DiscoveryResultListApiModel model) {
            if (string.IsNullOrEmpty(discovererId)) {
                throw new ArgumentNullException(nameof(discovererId));
            }
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            if (model.Result == null) {
                throw new ArgumentNullException(nameof(model.Result));
            }
            if (model.Events == null) {
                throw new ArgumentNullException(nameof(model.Events));
            }
            await _onboarding.ProcessDiscoveryResultsAsync(
                discovererId, model.Result.ToServiceModel(),
                model.Events.Select(e => e.ToServiceModel()));
        }

        private readonly IDiscoveryResultProcessor _onboarding;
    }
}
