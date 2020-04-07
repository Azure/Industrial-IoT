// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Users {
    using Microsoft.Azure.IIoT.Services.Common.Users.Filters;
    using Microsoft.Azure.IIoT.Services.Common.Users.Auth;
    using Microsoft.Azure.IIoT.Services.Common.Users.Models;
    using Microsoft.Azure.IIoT.Api.Identity.Models;
    using Microsoft.Azure.IIoT.Auth.IdentityServer4.Models;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using System.ComponentModel.DataAnnotations;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Role manager controller
    /// </summary>
    [ApiVersion("2")]
    [Route("v{version:apiVersion}/roles")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanManage)]
    [ApiController]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [SecurityHeaders]
    public class RolesController : Controller {

        /// <summary>
        /// Create role manager
        /// </summary>
        /// <param name="manager"></param>
        public RolesController(RoleManager<RoleModel> manager) {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        /// <summary>
        /// Create role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task CreateRoleAsync(
            [FromBody] [Required] RoleApiModel role) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            var result = await _manager.CreateAsync(role.ToServiceModel());
            result.Validate();
        }

        /// <summary>
        /// Get role
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [HttpGet("{roleId}")]
        public async Task<RoleApiModel> GetRoleByIdAsync(string roleId) {
            if (string.IsNullOrWhiteSpace(roleId)) {
                throw new ArgumentNullException(nameof(roleId));
            }
            var role = await _manager.FindByIdAsync(roleId);
            return role.ToApiModel();
        }

        /// <summary>
        /// Delete role
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [HttpDelete("{roleId}")]
        public async Task DeleteRoleAsync(string roleId) {
            if (string.IsNullOrWhiteSpace(roleId)) {
                throw new ArgumentNullException(nameof(roleId));
            }
            var role = await _manager.FindByIdAsync(roleId);
            var result = await _manager.DeleteAsync(role);
            result.Validate();
        }

        private readonly RoleManager<RoleModel> _manager;
    }
}
