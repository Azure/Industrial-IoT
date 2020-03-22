// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Storage {
    using Microsoft.Azure.IIoT.Auth.IdentityServer4.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.AspNetCore.Identity;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Security.Claims;

    /// <summary>
    /// Represents a store for Identity roles
    /// </summary>
    public class RoleDatabase : IRoleClaimStore<RoleModel> {

        /// <summary>
        /// Create role database
        /// </summary>
        /// <param name="factory"></param>
        public RoleDatabase(IItemContainerFactory factory) {
            if (factory == null) {
                throw new ArgumentNullException(nameof(factory));
            }
            _documents = factory.OpenAsync("identity").Result.AsDocuments();
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> CreateAsync(RoleModel role,
            CancellationToken ct) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            try {
                var document = role.ToDocumentModel();
                await _documents.AddAsync(document, ct);
                return IdentityResult.Success;
            }
            catch {
                return IdentityResult.Failed();
            }
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> UpdateAsync(RoleModel role,
            CancellationToken ct) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            while (true) {
                var document = await _documents.FindAsync<RoleDocumentModel>(
                    role.Id, ct);
                if (document == null) {
                    return IdentityResult.Failed();
                }
                try {
                    var newDocument = document.Value.UpdateFrom(role);
                    document = await _documents.ReplaceAsync(document,
                        newDocument, ct);
                    return IdentityResult.Success;
                }
                catch (ResourceOutOfDateException) {
                    continue; // Replace failed due to etag out of date - retry
                }
            }
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> DeleteAsync(RoleModel role,
            CancellationToken ct) {
            try {
                await _documents.DeleteAsync(role.Id, ct, null,
                    role.ConcurrencyStamp);
                return IdentityResult.Success;
            }
            catch {
                return IdentityResult.Failed();
            }
        }

        /// <inheritdoc/>
        public async Task<RoleModel> FindByIdAsync(string roleId,
            CancellationToken ct) {
            var role = await _documents.FindAsync<RoleDocumentModel>(roleId, ct);
            if (role?.Value == null) {
                return null;
            }
            return role.Value.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<RoleModel> FindByNameAsync(string normalizedRoleName,
            CancellationToken ct) {
            if (normalizedRoleName == null) {
                throw new ArgumentNullException(nameof(normalizedRoleName));
            }
            var client = _documents.OpenSqlClient();
            var queryString = $"SELECT * FROM r WHERE ";
            queryString +=
                $"r.{nameof(RoleDocumentModel.NormalizedName)} = @name";
            var queryParameters = new Dictionary<string, object> {
                { "@name", normalizedRoleName.ToLowerInvariant() }
            };
            var results = client.Query<RoleDocumentModel>(queryString,
                queryParameters, 1);
            if (results.HasMore()) {
                var documents = await results.ReadAsync();
                return documents.FirstOrDefault().Value.ToServiceModel();
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<IList<Claim>> GetClaimsAsync(RoleModel role,
            CancellationToken ct) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            var doc = await _documents.GetAsync<RoleDocumentModel>(role.Id, ct);
            return doc.Value.Claims.Select(c => c.ToServiceModel()).ToList();
        }

        /// <inheritdoc/>
        public Task AddClaimAsync(RoleModel role, Claim claim,
            CancellationToken ct) {
            if (claim == null) {
                throw new ArgumentNullException(nameof(claim));
            }
            return UpdateRoleDocumentAsync(role, document => {
                document.Claims.Add(claim.ToDocumentModel());
                return document;
            }, ct);
        }

        /// <inheritdoc/>
        public Task RemoveClaimAsync(RoleModel role, Claim claim,
            CancellationToken ct) {
            if (claim == null) {
                throw new ArgumentNullException(nameof(claim));
            }
            return UpdateRoleDocumentAsync(role, document => {
                document.Claims.RemoveAll(c =>
                    c.Type == claim.Type &&
                    c.Value == claim.Value);
                return document;
            }, ct);
        }

        /// <inheritdoc/>
        public Task<string> GetRoleIdAsync(RoleModel role,
            CancellationToken ct) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            return Task.FromResult(role.Id);
        }

        /// <inheritdoc/>
        public Task<string> GetRoleNameAsync(RoleModel role,
            CancellationToken ct) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            return Task.FromResult(role.Name);
        }

        /// <inheritdoc/>
        public Task SetRoleNameAsync(RoleModel role,
            string roleName, CancellationToken ct) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            role.Name = roleName ??
                throw new ArgumentNullException(nameof(roleName));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<string> GetNormalizedRoleNameAsync(
            RoleModel role, CancellationToken ct) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            return Task.FromResult(role.NormalizedName);
        }

        /// <inheritdoc/>
        public Task SetNormalizedRoleNameAsync(
            RoleModel role, string normalizedName,
            CancellationToken ct) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            role.NormalizedName = normalizedName ??
                throw new ArgumentNullException(nameof(normalizedName));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose() {
        }

        /// <summary>
        /// Update role document
        /// </summary>
        /// <param name="role"></param>
        /// <param name="update"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<T> UpdateRoleDocumentAsync<T>(RoleModel role,
            Func<RoleDocumentModel, T> update, CancellationToken ct) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            if (update == null) {
                throw new ArgumentNullException(nameof(update));
            }
            while (true) {
                var document = await _documents.FindAsync<RoleDocumentModel>(
                    role.Id, ct);
                if (document == null) {
                    throw new ResourceNotFoundException("Role was not found");
                }
                if (document.Etag != role.ConcurrencyStamp) {
                    throw new ResourceOutOfDateException("Role was out of date");
                }
                try {
                    var newDocument = document.Value.Clone();
                    var result = update(newDocument);
                    document = await _documents.ReplaceAsync(document,
                        newDocument, ct);
                    return result;
                }
                catch (ResourceOutOfDateException) {
                    continue; // Replace failed due to etag out of date - retry
                }
            }
        }

        private readonly IDocuments _documents;
    }
}
