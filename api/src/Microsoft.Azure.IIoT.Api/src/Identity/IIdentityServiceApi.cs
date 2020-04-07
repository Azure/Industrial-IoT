// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Identity {
    using Microsoft.Azure.IIoT.Api.Identity.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// User manager api calls
    /// </summary>
    public interface IIdentityServiceApi {

        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <returns></returns>
        Task<string> GetServiceStatusAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Add new user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task CreateUserAsync(UserApiModel user,
            CancellationToken ct = default);

        /// <summary>
        /// Get user by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<UserApiModel> GetUserByNameAsync(string name,
            CancellationToken ct = default);

        /// <summary>
        /// Get user by email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<UserApiModel> GetUserByEmailAsync(string email,
            CancellationToken ct = default);

        /// <summary>
        /// Get user by id
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<UserApiModel> GetUserByIdAsync(
            string userId, CancellationToken ct = default);

        /// <summary>
        /// Delete user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeleteUserAsync(string userId,
            CancellationToken ct = default);

        /// <summary>
        /// Add new claim
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="model"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddClaimAsync(string userId,
            ClaimApiModel model, CancellationToken ct = default);

        /// <summary>
        /// Remove claim
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="model"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RemoveClaimAsync(string userId,
            ClaimApiModel model, CancellationToken ct = default);

        /// <summary>
        /// Add role to user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="role"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddRoleToUserAsync(string userId,
            string role, CancellationToken ct = default);

        /// <summary>
        /// Get users in role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<UserApiModel>> GetUsersInRoleAsync(
            string role, CancellationToken ct = default);

        /// <summary>
        /// Remove role from user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="role"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RemoveRoleFromUserAsync(string userId,
            string role, CancellationToken ct = default);

        /// <summary>
        /// Create role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task CreateRoleAsync(RoleApiModel role,
            CancellationToken ct = default);

        /// <summary>
        /// Get role
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<RoleApiModel> GetRoleByIdAsync(string roleId,
            CancellationToken ct = default);

        /// <summary>
        /// Delete role
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeleteRoleAsync(string roleId,
            CancellationToken ct = default);
    }
}
