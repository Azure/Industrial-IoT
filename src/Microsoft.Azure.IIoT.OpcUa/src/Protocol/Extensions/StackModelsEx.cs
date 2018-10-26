// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Stack models extensions
    /// </summary>
    public static class StackModelsEx {

        /// <summary>
        /// Convert diagnostics to request header
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <param name="timeoutHint"></param>
        /// <returns></returns>
        public static RequestHeader ToStackModel(this DiagnosticsModel diagnostics,
            uint timeoutHint = 0) {
            return new RequestHeader {
                AuditEntryId = diagnostics?.AuditId ?? Guid.NewGuid().ToString(),
                ReturnDiagnostics =
                    (uint)(diagnostics?.Level ?? Twin.Models.DiagnosticsLevel.None)
                     .ToStackType(),
                Timestamp = diagnostics?.TimeStamp ?? DateTime.UtcNow,
                TimeoutHint = timeoutHint,
                AdditionalHeader = null // TODO
            };
        }

        /// <summary>
        /// Convert request header to diagnostics configuration model
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <returns></returns>
        public static DiagnosticsModel ToServiceModel(this RequestHeader requestHeader) {
            return new DiagnosticsModel {
                AuditId = requestHeader.AuditEntryId,
                Level = ((DiagnosticsMasks)requestHeader.ReturnDiagnostics)
                    .ToServiceType(),
                TimeStamp = requestHeader.Timestamp
            };
        }


        /// <summary>
        /// Convert diagnostics to request header
        /// </summary>
        /// <param name="viewModel"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ViewDescription ToStackModel(this BrowseViewModel viewModel,
            ServiceMessageContext context) {
            if (viewModel == null) {
                return null;
            }
            return new ViewDescription {
                Timestamp = viewModel.Timestamp ??
                    DateTime.MinValue,
                ViewVersion = viewModel.Version ??
                    0,
                ViewId = viewModel.ViewId.ToNodeId(context)
            };
        }

        /// <summary>
        /// Convert request header to diagnostics configuration model
        /// </summary>
        /// <param name="view"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static BrowseViewModel ToServiceModel(this ViewDescription view,
            ServiceMessageContext context) {
            if (view == null) {
                return null;
            }
            return new BrowseViewModel {
                Timestamp = view.Timestamp == DateTime.MinValue ?
                    (DateTime?)null : view.Timestamp,
                Version = view.ViewVersion == 0 ?
                    (uint?)null : view.ViewVersion,
                ViewId = view.ViewId.AsString(context)
            };
        }

        /// <summary>
        /// Convert endpoint description to application registration
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="hostAddress"></param>
        /// <param name="siteId"></param>
        /// <param name="supervisorId"></param>
        /// <returns></returns>
        public static ApplicationRegistrationModel ToServiceModel(this EndpointDescription ep,
            string hostAddress, string siteId, string supervisorId) {
            var caps = new HashSet<string>();
            if (ep.Server.ApplicationType == Opc.Ua.ApplicationType.DiscoveryServer) {
                caps.Add("LDS");
            }
            var type = ep.Server.ApplicationType.ToServiceType() ??
                Registry.Models.ApplicationType.Server;
            return new ApplicationRegistrationModel {
                Application = new ApplicationInfoModel {
                    SiteId = siteId,
                    SupervisorId = supervisorId,
                    ApplicationType = type,
                    HostAddresses = new HashSet<string> { hostAddress },
                    ApplicationId = ApplicationInfoModelEx.CreateApplicationId(
                        siteId, ep.Server.ApplicationUri, type),
                    ApplicationUri = ep.Server.ApplicationUri,
                    DiscoveryUrls = new HashSet<string>(ep.Server.DiscoveryUrls),
                    DiscoveryProfileUri = ep.Server.DiscoveryProfileUri,
                    ProductUri = ep.Server.ProductUri,
                    Certificate = ep.ServerCertificate,
                    ApplicationName = ep.Server.ApplicationName.Text,
                    Locale = ep.Server.ApplicationName.Locale,
                    Capabilities = caps
                },
                Endpoints = new List<TwinRegistrationModel> {
                    ep.ToServiceModel(siteId, supervisorId)
                }
            };
        }

        /// <summary>
        /// Converts an endpoint description to a twin registration model
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="siteId"></param>
        /// <param name="supervisorId"></param>
        /// <returns></returns>
        public static TwinRegistrationModel ToServiceModel(this EndpointDescription ep,
            string siteId, string supervisorId) {
            if (ep == null) {
                return null;
            }
            return new TwinRegistrationModel {
                SiteId = siteId,
                SupervisorId = supervisorId,
                Certificate = ep.ServerCertificate,
                SecurityLevel = ep.SecurityLevel,
                Endpoint = new EndpointModel {
                    Url = ep.EndpointUrl,
                    SecurityMode = ep.SecurityMode.ToServiceType(),
                    SecurityPolicy = ep.SecurityPolicyUri
                }
            };
        }
    }
}
