// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Services {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Entity info resolver - get entity information from all registry sources.
    /// </summary>
    public sealed class EntityInfoResolver : IEntityInfoResolver {

        /// <summary>
        /// Create certificate request
        /// </summary>
        /// <param name="applications"></param>
        /// <param name="publishers"></param>
        /// <param name="supervisors"></param>
        /// <param name="endpoints"></param>
        /// <param name="groups"></param>
        public EntityInfoResolver(IApplicationRegistry applications, IPublisherRegistry publishers,
            ISupervisorRegistry supervisors, IEndpointRegistry endpoints, IGroupRepository groups) {
            _applications = applications ?? throw new ArgumentNullException(nameof(applications));
            _publishers = publishers ?? throw new ArgumentNullException(nameof(publishers));
            _supervisors = supervisors ?? throw new ArgumentNullException(nameof(supervisors));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _groups = groups ?? throw new ArgumentNullException(nameof(groups));
        }

        /// <inheritdoc/>
        public async Task<EntityInfoModel> FindEntityAsync(string entityId, CancellationToken ct) {
            if (string.IsNullOrEmpty(entityId)) {
                throw new ArgumentNullException(nameof(entityId));
            }
            // Resolve
            var entities = await Task.WhenAll(
                FindApplicationAsync(entityId, ct),
                FindEndpointAsync(entityId, ct),
                FindSupervisorAsync(entityId, ct),
                FindGroupAsync(entityId, ct),
                FindPublisherAsync(entityId, ct));

            if (!entities.Any()) {
                return null;
            }
            var entity = entities.SingleOrDefault();
            if (entity == null) {
                throw new ConflictingResourceException(
                    $"Unexpected : Found more than one entity for {entityId}.");
            }
            try {
                return entity.Validate();
            }
            catch (Exception ex) {
                throw new ResourceInvalidStateException(
                   $"Unexpected : Failed to validate entity for {entityId}.", ex);
            }
        }

        /// <summary>
        /// Resolve application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<EntityInfoModel> FindApplicationAsync(string applicationId,
            CancellationToken ct) {
            var application = await _applications.FindApplicationAsync(applicationId, ct);
            if (application?.Application == null) {
                return null;
            }

            var addresses = new HashSet<string>(application.Application.HostAddresses
                ?? new HashSet<string>());
            if (application.Endpoints != null) {
                foreach (var ep in application.Endpoints) {
                    var parsed = Opc.Ua.Utils.ParseUri(ep.Endpoint.Url);
                    if (parsed != null) {
                        addresses.Add(parsed.DnsSafeHost);
                    }
                    parsed = Opc.Ua.Utils.ParseUri(ep.EndpointUrl);
                    if (parsed != null) {
                        addresses.Add(parsed.DnsSafeHost);
                    }
                    foreach (var url in ep.Endpoint.AlternativeUrls) {
                        parsed = Opc.Ua.Utils.ParseUri(url);
                        if (parsed != null) {
                            addresses.Add(parsed.DnsSafeHost);
                        }
                    }
                }
            }
            return new EntityInfoModel {
                Name = application.Application.ApplicationName,
                Uris = new List<string> { application.Application.ApplicationUri },
                Id = applicationId,
                Role = application.Application.ApplicationType == ApplicationType.Client ?
                    EntityRoleType.Client : EntityRoleType.Server,
                Type = EntityType.Application,
                Addresses = addresses.ToList(),
                SubjectName = null
            };
        }

        /// <summary>
        /// Resolve endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<EntityInfoModel> FindEndpointAsync(string endpointId,
            CancellationToken ct) {
            var ep = await _endpoints.FindEndpointAsync(endpointId, ct);
            if (ep == null) {
                return null;
            }
            var addresses = new HashSet<string>();
            var uris = new HashSet<string>();
            var parsed = Opc.Ua.Utils.ParseUri(ep.Registration.Endpoint.Url);
            if (parsed != null) {
                addresses.Add(parsed.DnsSafeHost);
                uris.Add(ep.Registration.Endpoint.Url);
            }
            parsed = Opc.Ua.Utils.ParseUri(ep.Registration.EndpointUrl);
            if (parsed != null) {
                addresses.Add(parsed.DnsSafeHost);
                uris.Add(ep.Registration.EndpointUrl);
            }
            if (ep.Registration.Endpoint.AlternativeUrls != null) {
                foreach (var url in ep.Registration.Endpoint.AlternativeUrls) {
                    parsed = Opc.Ua.Utils.ParseUri(url);
                    if (parsed != null) {
                        addresses.Add(parsed.DnsSafeHost);
                        uris.Add(url);
                    }
                }
            }
            return new EntityInfoModel {
                Name = ep.Registration.Endpoint.Url,
                Uris = uris.ToList(),
                Id = endpointId,
                Role = EntityRoleType.Server,
                Type = EntityType.Endpoint,
                Addresses = addresses.ToList(),
                SubjectName = null
            };
        }

        /// <summary>
        /// Resolve supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<EntityInfoModel> FindSupervisorAsync(string supervisorId,
            CancellationToken ct) {
            var twinModule = await _supervisors.FindSupervisorAsync(supervisorId, ct);
            if (twinModule == null) {
                return null;
            }
            return new EntityInfoModel {
                Name = supervisorId,
                Uris = new List<string> { $"urn:twin:{twinModule.SiteId}:{supervisorId}" },
                Id = supervisorId,
                Role = EntityRoleType.Client,
                Type = EntityType.Twin,
                Addresses = new List<string>(),
                SubjectName = null
            };
        }

        /// <summary>
        /// Resolve publisher
        /// </summary>
        /// <param name="publisherId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<EntityInfoModel> FindPublisherAsync(string publisherId,
            CancellationToken ct) {
            var publisher = await _publishers.FindPublisherAsync(publisherId, ct);
            if (publisher == null) {
                return null;
            }
            return new EntityInfoModel {
                Name = publisherId,
                Uris = new List<string> { $"urn:publisher:{publisher.SiteId}:{publisherId}" },
                Id = publisherId,
                Role = EntityRoleType.Client,
                Type = EntityType.Publisher,
                Addresses = new List<string>(),
                SubjectName = null
            };
        }

        /// <summary>
        /// Resolve group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<EntityInfoModel> FindGroupAsync(string groupId,
            CancellationToken ct) {
            var group = await _groups.FindAsync(groupId, ct);
            if (group == null) {
                return null;
            }
            return new EntityInfoModel {
                Name = group.Group.Name,
                Uris = new List<string> { $"urn:group:{group.Id}" },
                Id = group.Id,
                Type = EntityType.Group,
                Addresses = new List<string>(),
                SubjectName = group.Group.SubjectName
            };
        }

        private readonly IApplicationRegistry _applications;
        private readonly IEndpointRegistry _endpoints;
        private readonly IGroupRepository _groups;
        private readonly ISupervisorRegistry _supervisors;
        private readonly IPublisherRegistry _publishers;
    }
}
