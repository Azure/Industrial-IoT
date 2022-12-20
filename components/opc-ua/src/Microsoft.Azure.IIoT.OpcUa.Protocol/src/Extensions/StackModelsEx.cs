// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Linq;

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
            if (diagnostics == null && timeoutHint == 0) {
                return null;
            }
            return new RequestHeader {
                AuditEntryId = diagnostics?.AuditId ?? Guid.NewGuid().ToString(),
                ReturnDiagnostics =
                    (uint)(diagnostics?.Level ?? Core.Models.DiagnosticsLevel.None)
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
            IServiceMessageContext context) {
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
            IServiceMessageContext context) {
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
        /// Convert service model to role permission type
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static RolePermissionType ToStackModel(this RolePermissionModel model,
            IServiceMessageContext context) {
            if (model == null) {
                return null;
            }
            return new RolePermissionType {
                RoleId = model.RoleId.ToNodeId(context),
                Permissions = (uint)model.Permissions.ToStackType()
            };
        }

        /// <summary>
        /// Convert role permission type to service model
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static RolePermissionModel ToServiceModel(this RolePermissionType type,
            IServiceMessageContext context) {
            if (type == null) {
                return null;
            }
            return new RolePermissionModel {
                RoleId = type.RoleId.AsString(context),
                Permissions = ((PermissionType)type.Permissions).ToServiceType()
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataChangeFilter ToStackModel(this DataChangeFilterModel model) {
            if (model == null) {
                return null;
            }
            return new DataChangeFilter {
                DeadbandValue = model.DeadbandValue ?? 0.0,
                DeadbandType = (uint)model.DeadbandType.ToStackType(),
                Trigger = model.DataChangeTrigger.ToStackType()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataChangeFilterModel ToServiceModel(this DataChangeFilter model) {
            if (model == null) {
                return null;
            }
            return new DataChangeFilterModel {
                DeadbandValue = (int)model.DeadbandValue == 0 ? (double?)null :
                    model.DeadbandValue,
                DeadbandType = ((DeadbandType)model.DeadbandType).ToServiceType(),
                DataChangeTrigger = model.Trigger.ToServiceType()
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static AggregateFilter ToStackModel(this AggregateFilterModel model,
            IServiceMessageContext context) {
            if (model == null) {
                return null;
            }
            return new AggregateFilter {
                AggregateConfiguration = model.AggregateConfiguration.ToStackModel(),
                AggregateType = model.AggregateTypeId.ToNodeId(context),
                StartTime = model.StartTime ?? DateTime.MinValue,
                ProcessingInterval = model.ProcessingInterval ?? 0.0
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static AggregateFilterModel ToServiceModel(this AggregateFilter model,
            IServiceMessageContext context) {
            if (model == null) {
                return null;
            }
            return new AggregateFilterModel {
                AggregateConfiguration = model.AggregateConfiguration.ToServiceModel(),
                AggregateTypeId = model.AggregateType.AsString(context),
                StartTime = model.StartTime == DateTime.MinValue ? (DateTime?)null :
                    model.StartTime,
                ProcessingInterval = (int)model.ProcessingInterval == 0 ? (double?)null :
                    model.ProcessingInterval
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AggregateConfiguration ToStackModel(
            this AggregateConfigurationModel model) {
            if (model == null) {
                return new AggregateConfiguration();
            }
            return new AggregateConfiguration {
                UseServerCapabilitiesDefaults = model.UseServerCapabilitiesDefaults ?? true,
                PercentDataBad = model.PercentDataBad ?? 0,
                PercentDataGood = model.PercentDataGood ?? 0,
                TreatUncertainAsBad = model.TreatUncertainAsBad ?? true,
                UseSlopedExtrapolation = model.UseSlopedExtrapolation ?? true
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AggregateConfigurationModel ToServiceModel(
            this AggregateConfiguration model) {
            if (model == null) {
                return null;
            }
            return new AggregateConfigurationModel {
                UseServerCapabilitiesDefaults = model.UseServerCapabilitiesDefaults ? (bool?)null :
                    model.UseServerCapabilitiesDefaults,
                PercentDataBad = model.PercentDataBad == 0 ? (byte?)null :
                    model.PercentDataBad,
                PercentDataGood = model.PercentDataGood == 0 ? (byte?)null :
                    model.PercentDataGood,
                TreatUncertainAsBad = model.TreatUncertainAsBad ? (bool?)null :
                    model.TreatUncertainAsBad,
                UseSlopedExtrapolation = model.UseSlopedExtrapolation ? (bool?)null :
                    model.UseSlopedExtrapolation
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static SimpleAttributeOperand ToStackModel(this SimpleAttributeOperandModel model,
            IServiceMessageContext context) {
            if (model == null) {
                return null;
            }
            return new SimpleAttributeOperand {
                TypeDefinitionId = model.TypeDefinitionId.ToNodeId(context),
                AttributeId = (uint)(model.AttributeId ?? NodeAttribute.Value),
                BrowsePath = new QualifiedNameCollection(model.BrowsePath == null ?
                    Enumerable.Empty<QualifiedName>() :
                    model.BrowsePath?.Select(n => n.ToQualifiedName(context))),
                IndexRange = model.IndexRange
            };
        }

        /// <summary>
        /// Convert user token policies to service model
        /// </summary>
        /// <param name="policies"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static List<AuthenticationMethodModel> ToServiceModel(
            this UserTokenPolicyCollection policies, IJsonSerializer serializer) {
            if (policies == null || policies.Count == 0) {
                return new List<AuthenticationMethodModel>{
                     new AuthenticationMethodModel {
                         Id = "Anonymous",
                         CredentialType = CredentialType.None
                     }
                };
            }
            return policies
                .Select(p => p.ToServiceModel(serializer))
                .Where(p => p != null)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Convert service model to user token policy collection
        /// </summary>
        /// <param name="policies"></param>
        /// <returns></returns>
        public static UserTokenPolicyCollection ToStackModel(
            this List<AuthenticationMethodModel> policies) {
            if (policies == null || policies.Count == 0) {
                return new UserTokenPolicyCollection{
                     new UserTokenPolicy {
                         PolicyId = "Anonymous",
                         TokenType = UserTokenType.Anonymous
                     }
                };
            }
            return new UserTokenPolicyCollection(policies
                .Select(p => p.ToStackModel())
                .Where(p => p != null)
                .Distinct());
        }

        /// <summary>
        /// Convert user token policy to service model
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static AuthenticationMethodModel ToServiceModel(this UserTokenPolicy policy,
            IJsonSerializer serializer) {
            if (policy == null) {
                return null;
            }
            var result = new AuthenticationMethodModel {
                Id = policy.PolicyId,
                SecurityPolicy = policy.SecurityPolicyUri
            };
            switch (policy.TokenType) {
                case UserTokenType.Anonymous:
                    result.CredentialType = CredentialType.None;
                    break;
                case UserTokenType.UserName:
                    result.CredentialType = CredentialType.UserName;
                    break;
                case UserTokenType.Certificate:
                    result.CredentialType = CredentialType.X509Certificate;
                    result.Configuration = policy.IssuerEndpointUrl;
                    break;
                case UserTokenType.IssuedToken:
                    switch (policy.IssuedTokenType) {
                        case "http://opcfoundation.org/UA/UserToken#JWT":
                            result.CredentialType = CredentialType.JwtToken;
                            try {
                                // See part 6
                                result.Configuration = serializer.Parse(
                                    policy.IssuerEndpointUrl);
                            }
                            catch {
                                // Store as string
                                result.Configuration = policy.IssuerEndpointUrl;
                            }
                            break;
                        default:
                            // TODO
                            return null;
                    }
                    break;
                default:
                    return null;
            }
            return result;
        }

        /// <summary>
        /// Convert service model to user token policy
        /// </summary>
        /// <param name="policy"></param>
        /// <returns></returns>
        public static UserTokenPolicy ToStackModel(this AuthenticationMethodModel policy) {
            if (policy == null) {
                return null;
            }
            var result = new UserTokenPolicy {
                SecurityPolicyUri = policy.SecurityPolicy,
                PolicyId = policy.Id
            };
            switch (policy.CredentialType) {
                case CredentialType.None:
                    result.TokenType = UserTokenType.Anonymous;
                    break;
                case CredentialType.UserName:
                    result.TokenType = UserTokenType.UserName;
                    break;
                case CredentialType.X509Certificate:
                    result.TokenType = UserTokenType.Certificate;
                    break;
                case CredentialType.JwtToken:
                    result.TokenType = UserTokenType.IssuedToken;
                    result.IssuedTokenType = "http://opcfoundation.org/UA/UserToken#JWT";
                    result.IssuerEndpointUrl = policy.Configuration?.ToString();
                    break;
                default:
                    return null;
            }
            return result;
        }

        /// <summary>
        /// Makes a user identity
        /// </summary>
        /// <param name="authentication"></param>
        /// <returns></returns>
        public static IUserIdentity ToStackModel(this CredentialModel authentication) {
            switch (authentication?.Type ?? CredentialType.None) {
                case CredentialType.UserName:
                    if (authentication.Value != null &&
                        authentication.Value.IsObject &&
                        authentication.Value.TryGetProperty("user", out var user) &&
                            user.IsString &&
                        authentication.Value.TryGetProperty("password", out var password) &&
                            password.IsString) {
                        return new UserIdentity((string)user, (string)password);
                    }
                    throw new ServiceResultException(StatusCodes.BadNotSupported,
                        $"User/passord token format is not supported.");
                case CredentialType.X509Certificate:
                    return new UserIdentity(new X509Certificate2(
                        authentication.Value?.ConvertTo<byte[]>()));
                case CredentialType.JwtToken:
                    return new UserIdentity(new IssuedIdentityToken {
                        DecryptedTokenData = authentication.Value?.ConvertTo<byte[]>()
                    });
                case CredentialType.None:
                    return new UserIdentity(new AnonymousIdentityToken());
                default:
                    throw new ServiceResultException(StatusCodes.BadNotSupported,
                        $"Token type {authentication.Type} is not supported");
            }
        }

        /// <summary>
        /// Makes a user identity token
        /// </summary>
        /// <param name="authentication"></param>
        /// <returns></returns>
        public static UserIdentityToken ToUserIdentityToken(this CredentialModel authentication) {
            switch (authentication?.Type ?? CredentialType.None) {
                case CredentialType.UserName:
                    if (authentication.Value.IsObject &&
                        authentication.Value.TryGetProperty("user", out var user) &&
                            user.IsString &&
                        authentication.Value.TryGetProperty("password", out var password) &&
                            password.IsString) {
                        return new UserNameIdentityToken {
                            DecryptedPassword = (string)password,
                            UserName = (string)user
                        };
                    }
                    throw new ServiceResultException(StatusCodes.BadNotSupported,
                        $"User/passord token format is not supported.");
                case CredentialType.X509Certificate:
                    return new X509IdentityToken {
                        Certificate = new X509Certificate2(
                        authentication.Value?.ConvertTo<byte[]>())
                    };
                case CredentialType.JwtToken:
                    return new IssuedIdentityToken {
                        DecryptedTokenData = authentication.Value?.ConvertTo<byte[]>()
                    };
                case CredentialType.None:
                    return new AnonymousIdentityToken();
                default:
                    throw new ServiceResultException(StatusCodes.BadNotSupported,
                        $"Token type {authentication.Type} is not supported");
            }
        }

        /// <summary>
        /// Convert user identity to service model
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static CredentialModel ToServiceModel(this IUserIdentity identity, IJsonSerializer serializer) {
            return ToServiceModel(identity?.GetIdentityToken(), serializer);
        }

        /// <summary>
        /// Convert user identity token to service model
        /// </summary>
        /// <param name="token"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static CredentialModel ToServiceModel(this UserIdentityToken token,
            IJsonSerializer serializer) {
            if (token == null) {
                return null;  // Treat as anonymous
            }
            switch (token) {
                case IssuedIdentityToken it:
                    switch (it.IssuedTokenType) {
                        case IssuedTokenType.JWT:
                            return new CredentialModel {
                                Type = CredentialType.JwtToken,
                                Value = serializer.FromObject(it.DecryptedTokenData)
                            };
                        case IssuedTokenType.SAML:
                        // TODO?
                        default:
                            throw new ServiceResultException(
                                StatusCodes.BadNotSupported,
                                $"Token type {it.IssuedTokenType} is not supported");
                    }
                case AnonymousIdentityToken ai:
                    return null;
                case UserNameIdentityToken un:
                    return new CredentialModel {
                        Type = CredentialType.UserName,
                        Value = serializer.FromObject(new {
                            user = un.UserName,
                            password = un.DecryptedPassword
                        })
                    };
                case X509IdentityToken x5:
                    return new CredentialModel {
                        Type = CredentialType.X509Certificate,
                        Value = serializer.FromObject(x5.CertificateData)
                    };
                default:
                    throw new ServiceResultException(StatusCodes.BadNotSupported,
                        $"User identity token type {token.GetType()} is unsupported");
            }
        }
    }
}
