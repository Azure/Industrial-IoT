// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Application document extensions
    /// </summary>
    public static class ApplicationDocumentEx {

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="document"></param>
        public static ApplicationInfoModel ToServiceModel(
            this ApplicationDocument document) {
            return new ApplicationInfoModel {
                ApplicationId = document.ApplicationId,
                State = document.ApplicationState,
                ApplicationUri = document.ApplicationUri,
                ApplicationName = document.GetApplicationName(),
                ApplicationType = document.ApplicationType,
                LocalizedNames = GetLocalizedText(document.ApplicationNames),
                ProductUri = document.ProductUri,
                DiscoveryUrls = document.DiscoveryUrls.ToHashSetSafe(),
                Capabilities = GetServerCapabilities(document.ServerCapabilities),
                GatewayServerUri = document.GatewayServerUri,
                DiscoveryProfileUri = document.DiscoveryProfileUri,
                NotSeenSince = document.NotSeenSince,
                Approved = ToServiceModel(
                    document.ApproveTime, document.ApproveAuthorityId),
                Created = ToServiceModel(
                    document.CreateTime, document.CreateAuthorityId),
                Updated = ToServiceModel(
                    document.UpdateTime, document.UpdateAuthorityId),
            };
        }

        /// <summary>
        /// Convert to document model
        /// </summary>
        /// <returns></returns>
        public static ApplicationDocument ToDocumentModel(
            this ApplicationInfoModel model, string etag = null) {
            var document = new ApplicationDocument {
                ID = 0,
                ApplicationState = model.State,
                ApplicationId = model.ApplicationId,
                ApplicationUri = model.ApplicationUri,
                ApplicationName = model.ApplicationName ??
                    model.LocalizedNames.FirstOrDefault().Value,
                ApplicationType = model.ApplicationType,
                SiteId = model.SiteId,
                ApplicationNames =
                    GetLocalizedText(model.LocalizedNames),
                ProductUri = model.ProductUri,
                DiscoveryUrls = model.DiscoveryUrls?.ToArray(),
                ServerCapabilities =
                    GetServerCapabilitiesAsString(model.Capabilities),
                GatewayServerUri = model.GatewayServerUri,
                DiscoveryProfileUri = model.DiscoveryProfileUri,
                UpdateTime = model.Updated?.Time,
                NotSeenSince = model.NotSeenSince,
                CreateTime = model.Created?.Time,
                ApproveTime = model.Approved?.Time,
                UpdateAuthorityId = model.Updated?.AuthorityId,
                CreateAuthorityId = model.Created?.AuthorityId,
                ApproveAuthorityId = model.Approved?.AuthorityId,
                ETag = etag
            };
            document.Validate();
            return document;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static ApplicationDocument Clone(this ApplicationDocument model) {
            return new ApplicationDocument {
                ID = model.ID,
                ApplicationState = model.ApplicationState,
                ApplicationId = model.ApplicationId,
                ApplicationUri = model.ApplicationUri,
                ApplicationName = model.ApplicationName,
                ApplicationType = model.ApplicationType,
                SiteId = model.SiteId,
                ApplicationNames = model.ApplicationNames?
                    .Select(n => new ApplicationDocument.LocalizedText {
                        Locale = n.Locale,
                        Name = n.Name
                    })
                    .ToArray(),
                ProductUri = model.ProductUri,
                DiscoveryUrls = model.DiscoveryUrls?.ToArray(),
                ServerCapabilities = model.ServerCapabilities,
                GatewayServerUri = model.GatewayServerUri,
                DiscoveryProfileUri = model.DiscoveryProfileUri,
                UpdateTime = model.UpdateTime,
                NotSeenSince = model.NotSeenSince,
                CreateTime = model.CreateTime,
                ApproveTime = model.ApproveTime,
                UpdateAuthorityId = model.UpdateAuthorityId,
                DisableAuthorityId = model.DisableAuthorityId,
                CreateAuthorityId = model.CreateAuthorityId,
                ApproveAuthorityId = model.ApproveAuthorityId,
                ETag = model.ETag,
                ClassType = model.ClassType
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static ApplicationDocument ToDocumentModel(
            this ApplicationRegistrationRequestModel model, uint id) {
            var document = new ApplicationDocument {
                ID = id,
                ApplicationState = ApplicationState.New,
                CreateTime = DateTime.UtcNow,
                SiteId = model.SiteId,
                ApplicationId = ApplicationInfoModelEx.CreateApplicationId(
                    model.SiteId, model.ApplicationUri, model.ApplicationType),
                ApplicationUri = model.ApplicationUri,
                ApplicationName = model.ApplicationName ??
                    model.LocalizedNames.FirstOrDefault().Value,
                ApplicationType = model.ApplicationType ?? ApplicationType.ClientAndServer,
                ApplicationNames =
                    GetLocalizedText(model.LocalizedNames),
                ProductUri = model.ProductUri,
                DiscoveryUrls = model.DiscoveryUrls?.ToArray(),
                ServerCapabilities =
                    GetServerCapabilitiesAsString(model.Capabilities),
                GatewayServerUri = model.GatewayServerUri,
                DiscoveryProfileUri = model.DiscoveryProfileUri,
            };
            document.Validate();
            return document;
        }

        /// <summary>
        /// Patch document
        /// </summary>
        /// <param name="document"></param>
        /// <param name="request"></param>
        public static void Patch(this ApplicationDocument document,
            ApplicationRegistrationUpdateModel request) {
            // Patch
            if (!string.IsNullOrEmpty(request.ApplicationName)) {
                document.ApplicationName = request.ApplicationName;
            }
            if (request.Capabilities != null) {
                document.ServerCapabilities =
                    GetServerCapabilitiesAsString(request.Capabilities);
            }
            if (!string.IsNullOrEmpty(request.DiscoveryProfileUri)) {
                document.DiscoveryProfileUri = request.DiscoveryProfileUri;
            }
            if (!string.IsNullOrEmpty(request.GatewayServerUri)) {
                document.GatewayServerUri = request.GatewayServerUri;
            }
            if (!string.IsNullOrEmpty(request.ProductUri)) {
                document.ProductUri = request.ProductUri;
            }
            if (request.DiscoveryUrls != null) {
                document.DiscoveryUrls = request.DiscoveryUrls.ToArray();
            }
            if (request.LocalizedNames != null) {
                var table = GetLocalizedText(document.ApplicationNames);
                foreach (var item in request.LocalizedNames) {
                    if (item.Value == null) {
                        table.Remove(item.Key);
                    }
                    else {
                        table.AddOrUpdate(item.Key, item.Value);
                    }
                }
                document.ApplicationNames = GetLocalizedText(table);
            }
            document.Validate();
        }

        /// <summary>
        /// Validates all fields in an application record to be consistent with
        /// the OPC UA specification.
        /// </summary>
        /// <param name="document">The application</param>
        public static void Validate(this ApplicationDocument document) {
            if (document == null) {
                throw new ArgumentNullException(nameof(document));
            }

            if (document.ApplicationUri == null) {
                throw new ArgumentNullException(nameof(document.ApplicationUri));
            }

            if (!Uri.IsWellFormedUriString(document.ApplicationUri, UriKind.Absolute)) {
                throw new ArgumentException(document.ApplicationUri +
                    " is not a valid URI.", nameof(document.ApplicationUri));
            }

            if ((document.ApplicationType < ApplicationType.Server) ||
                (document.ApplicationType > ApplicationType.DiscoveryServer)) {
                throw new ArgumentException(document.ApplicationType.ToString() +
                    " is not a valid ApplicationType.", nameof(document.ApplicationType));
            }

            if (string.IsNullOrEmpty(document.GetApplicationName())) {
                throw new ArgumentException(
                    "At least one ApplicationName must be provided.",
                    nameof(document.ApplicationNames));
            }

            if (string.IsNullOrEmpty(document.ProductUri)) {
                throw new ArgumentException(
                    "A ProductUri must be provided.", nameof(document.ProductUri));
            }

            if (!Uri.IsWellFormedUriString(document.ProductUri, UriKind.Absolute)) {
                throw new ArgumentException(document.ProductUri +
                    " is not a valid URI.", nameof(document.ProductUri));
            }

            if (document.DiscoveryUrls != null) {
                foreach (var discoveryUrl in document.DiscoveryUrls) {
                    if (string.IsNullOrEmpty(discoveryUrl)) {
                        continue;
                    }

                    if (!Uri.IsWellFormedUriString(discoveryUrl, UriKind.Absolute)) {
                        throw new ArgumentException(discoveryUrl + " is not a valid URL.",
                            nameof(document.DiscoveryUrls));
                    }

                    // TODO: check for https:/hostname:62541, typo is not detected here
                }
            }

            if ((int)document.ApplicationType != (int)Opc.Ua.ApplicationType.Client) {
                if (document.DiscoveryUrls == null || document.DiscoveryUrls.Length == 0) {
                    throw new ArgumentException(
                        "At least one DiscoveryUrl must be provided.",
                        nameof(document.DiscoveryUrls));
                }

                if (string.IsNullOrEmpty(document.ServerCapabilities)) {
                    throw new ArgumentException(
                        "At least one Server Capability must be provided.",
                        nameof(document.ServerCapabilities));
                }

                // TODO: check for valid servercapabilities
            }
            else {
                if (document.DiscoveryUrls != null && document.DiscoveryUrls.Length > 0) {
                    throw new ArgumentException(
                        "DiscoveryUrls must not be specified for clients.",
                        nameof(document.DiscoveryUrls));
                }
            }
        }

        /// <summary>
        /// Returns server capabilities as comma separated string.
        /// </summary>
        /// <param name="document">The application record.</param>
        public static string GetServerCapabilitiesAsString(this ApplicationDocument document) {
            if ((int)document.ApplicationType != (int)ApplicationType.Client) {
                if (string.IsNullOrEmpty(document.ServerCapabilities)) {
                    return "NA";
                }
            }
            return GetServerCapabilitiesAsString(document.ServerCapabilities);
        }

        /// <summary>
        /// Returns server capabilities as comma separated string.
        /// </summary>
        /// <param name="document">The application record.</param>
        public static string GetApplicationName(this ApplicationDocument document) {
            if (!string.IsNullOrEmpty(document.ApplicationName)) {
                return document.ApplicationName;
            }
            if (document.ApplicationNames != null &&
                document.ApplicationNames.Length != 0 &&
                !string.IsNullOrEmpty(document.ApplicationNames[0].Name)) {
                return document.ApplicationNames[0].Name;
            }
            return null;
        }

        /// <summary>
        /// Returns server capabilities hash set
        /// </summary>
        /// <param name="caps">Capabilities.</param>
        /// <returns></returns>
        internal static HashSet<string> GetServerCapabilities(string caps) {
            return GetServerCapabilitiesAsString(caps)
.Split(',').Where(x => !string.IsNullOrEmpty(x))
.ToHashSet();
        }

        /// <summary>
        /// Returns server capabilities string
        /// </summary>
        /// <param name="caps">Capabilities.</param>
        /// <returns></returns>
        internal static string GetServerCapabilitiesAsString(HashSet<string> caps) {
            return caps?.Any() ?? false ?
                caps.Aggregate((x, y) => $"{x},{y}") : string.Empty;
        }

        /// <summary>
        /// Returns server capabilities as comma separated string.
        /// </summary>
        /// <param name="caps">Capabilities.</param>
        /// <returns></returns>
        private static string GetServerCapabilitiesAsString(string caps) {
            var capabilities = new StringBuilder();
            if (caps != null) {
                var sortedCaps = caps.Split(",").ToList();
                sortedCaps.Sort();
                foreach (var capability in sortedCaps) {
                    if (string.IsNullOrEmpty(capability)) {
                        continue;
                    }
                    if (capabilities.Length > 0) {
                        capabilities.Append(',');
                    }
                    capabilities.Append(capability);
                }
            }
            return capabilities.ToString();
        }

        /// <summary>
        /// Registry registry operation model from fields
        /// </summary>
        /// <param name="time"></param>
        /// <param name="authorityId"></param>
        /// <returns></returns>
        private static RegistryOperationContextModel ToServiceModel(DateTime? time,
            string authorityId) {
            if (time == null) {
                return null;
            }
            return new RegistryOperationContextModel {
                AuthorityId = string.IsNullOrEmpty(authorityId) ?
                    "Unknown" : authorityId,
                Time = time.Value
            };
        }

        /// <summary>
        /// Convert table to localized text
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private static ApplicationDocument.LocalizedText[] GetLocalizedText(
            Dictionary<string, string> table) {
            return table?
                .Select(kv => new ApplicationDocument.LocalizedText {
                    Locale = kv.Key,
                    Name = kv.Value
                })
                .ToArray();
        }

        /// <summary>
        /// Convert table to localized text
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private static Dictionary<string, string> GetLocalizedText(
            ApplicationDocument.LocalizedText[] table) {
            return table?.ToDictionary(n => n.Locale, n => n.Name);
        }
    }
}
