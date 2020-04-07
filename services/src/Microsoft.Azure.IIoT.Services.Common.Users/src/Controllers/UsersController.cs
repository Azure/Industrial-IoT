// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Users {
    using Microsoft.Azure.IIoT.Services.Common.Users.Filters;
    using Microsoft.Azure.IIoT.Services.Common.Users.Models;
    using Microsoft.Azure.IIoT.Services.Common.Users.Auth;
    using Microsoft.Azure.IIoT.Api.Identity.Models;
    using Microsoft.Azure.IIoT.Auth.IdentityServer4.Models;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Security.Claims;
    using System.Linq;

    /// <summary>
    /// User manager controller
    /// </summary>
    [ApiVersion("2")]
    [Route("v{version:apiVersion}/users")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanManage)]
    [ApiController]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [SecurityHeaders]
    public class UsersController : Controller {

        /// <summary>
        /// User controller
        /// </summary>
        /// <param name="service"></param>
        public UsersController(UserManager<UserModel> service) {
            _manager = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Add new user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task CreateUserAsync(
            [FromBody] [Required] UserApiModel user) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            var result = await _manager.CreateAsync(user.ToServiceModel());
            result.Validate();
        }

        /// <summary>
        /// Get user by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<UserApiModel> GetUserByNameAsync(
            [FromQuery] [Required] string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentNullException(nameof(name));
            }
            var user = await _manager.FindByNameAsync(name);
            return user.ToApiModel();
        }

        /// <summary>
        /// Get user by email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<UserApiModel> GetUserByEmailAsync(
            [FromQuery] [Required] string email) {
            if (string.IsNullOrWhiteSpace(email)) {
                throw new ArgumentNullException(nameof(email));
            }
            var user = await _manager.FindByEmailAsync(email);
            return user.ToApiModel();
        }

        /// <summary>
        /// Get user by id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("{userId}")]
        public async Task<UserApiModel> GetUserByIdAsync(string userId) {
            if (string.IsNullOrWhiteSpace(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var user = await _manager.FindByIdAsync(userId);
            return user.ToApiModel();
        }

        /// <summary>
        /// Delete user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpDelete("{userId}")]
        public async Task DeleteUserAsync(string userId) {
            if (string.IsNullOrWhiteSpace(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }

            var user = await _manager.FindByIdAsync(userId);
            var result = await _manager.DeleteAsync(user);
            result.Validate();
        }

        /// <summary>
        /// Add new claim
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("{userId}/claims")]
        public async Task AddClaimAsync(string userId,
            [FromBody] [Required] ClaimApiModel model) {
            if (!_manager.SupportsUserClaim) {
                throw new NotSupportedException("Claim management not supported");
            }
            if (string.IsNullOrWhiteSpace(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var user = await _manager.FindByIdAsync(userId);
            var result = await _manager.AddClaimAsync(user, model.ToClaim());
            result.Validate();
        }

        /// <summary>
        /// Remove claim
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpDelete("{userId}/claims/{type}/{value}")]
        public async Task RemoveClaimAsync(string userId, string type, string value) {
            if (!_manager.SupportsUserClaim) {
                throw new NotSupportedException("Claim management not supported");
            }
            if (string.IsNullOrWhiteSpace(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            if (string.IsNullOrWhiteSpace(type)) {
                throw new ArgumentNullException(nameof(type));
            }
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentNullException(nameof(value));
            }
            var user = await _manager.FindByIdAsync(userId);
            var result = await _manager.RemoveClaimAsync(user, new Claim(type, value));
            result.Validate();
        }

        /// <summary>
        /// Add role to user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        [HttpPut("{userId}/roles/{role}")]
        public async Task AddRoleToUserAsync(string userId, string role) {
            if (!_manager.SupportsUserRole) {
                throw new NotSupportedException("Role management not supported");
            }
            if (string.IsNullOrWhiteSpace(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var user = await _manager.FindByIdAsync(userId);
            var result = await _manager.AddToRoleAsync(user, role);
            result.Validate();
        }

        /// <summary>
        /// Get users in role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        [HttpGet("/roles/{role}")]
        public async Task<IEnumerable<UserApiModel>> GetUsersInRoleAsync(
            string role) {
            var result = await _manager.GetUsersInRoleAsync(role);
            return result?.Select(u => u.ToApiModel());
        }

        /// <summary>
        /// Remove role from user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        [HttpDelete("{userId}/roles/{role}")]
        public async Task RemoveRoleFromUserAsync(string userId, string role) {
            if (!_manager.SupportsUserRole) {
                throw new NotSupportedException("Role management not supported");
            }
            if (string.IsNullOrWhiteSpace(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var user = await _manager.FindByIdAsync(userId);
            var result = await _manager.RemoveFromRoleAsync(user, role);
            result.Validate();
        }

        private readonly UserManager<UserModel> _manager;
    }
}
