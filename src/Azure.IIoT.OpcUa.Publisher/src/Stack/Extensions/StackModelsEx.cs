// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using DiagnosticsLevel = Publisher.Models.DiagnosticsLevel;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Stack models extensions
    /// </summary>
    public static class StackModelsEx
    {
        /// <summary>
        /// Convert diagnostics to request header
        /// </summary>
        /// <param name="header"></param>
        /// <param name="timeProvider"></param>
        /// <returns></returns>
        public static RequestHeader ToRequestHeader(this RequestHeaderModel? header,
            TimeProvider? timeProvider = null)
        {
            var timestamp = header?.Diagnostics?.TimeStamp
                ?? (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
            return new RequestHeader
            {
                AuditEntryId = header?.Diagnostics?.AuditId ?? Guid.NewGuid().ToString(),
                ReturnDiagnostics =
                    (uint)(header?.Diagnostics?.Level ?? DiagnosticsLevel.Status)
                    .ToStackType(),
                Timestamp = timestamp,
                TimeoutHint = (uint)(header?.OperationTimeout ?? 0),
                AdditionalHeader = null // TODO
            };
        }

        /// <summary>
        /// Convert diagnostics to request header
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static ViewDescription? ToStackModel(this BrowseViewModel? model,
            IServiceMessageContext context)
        {
            if (model is null)
            {
                return null;
            }
            return new ViewDescription
            {
                Timestamp = model.Timestamp ?? DateTime.MinValue,
                ViewVersion = model.Version ?? 0,
                ViewId = model.ViewId.ToNodeId(context)
            };
        }

        /// <summary>
        /// Convert role permission type to service model
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="namespaceFormat"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static RolePermissionModel ToServiceModel(this RolePermissionType type,
            IServiceMessageContext context, NamespaceFormat namespaceFormat)
        {
            var roleId = type.RoleId.AsString(context, namespaceFormat);
            if (roleId == null)
            {
                throw new ArgumentException("Permission type not a valid node id");
            }
            return new RolePermissionModel
            {
                RoleId = roleId,
                Permissions = ((PermissionType)type.Permissions).ToServiceType()
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static DataChangeFilter? ToStackModel(this DataChangeFilterModel? model)
        {
            if (model is null)
            {
                return null;
            }
            return new DataChangeFilter
            {
                DeadbandValue = model.DeadbandValue ?? 0.0,
                DeadbandType = (uint)model.DeadbandType.ToStackType(),
                Trigger = model.DataChangeTrigger.ToStackType()
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static AggregateFilter? ToStackModel(this AggregateFilterModel? model,
            IServiceMessageContext context)
        {
            if (model is null)
            {
                return null;
            }
            return new AggregateFilter
            {
                AggregateConfiguration = model.AggregateConfiguration.ToStackModel(),
                AggregateType = model.AggregateTypeId.ToNodeId(context),
                StartTime = model.StartTime ?? DateTime.MinValue,
                ProcessingInterval = (model.ProcessingInterval?.TotalMilliseconds) ?? 0.0
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AggregateConfiguration ToStackModel(
            this AggregateConfigurationModel? model)
        {
            if (model is null)
            {
                return new AggregateConfiguration
                {
                    UseServerCapabilitiesDefaults = true
                };
            }
            return new AggregateConfiguration
            {
                PercentDataBad = model.PercentDataBad ?? 0,
                PercentDataGood = model.PercentDataGood ?? 0,
                TreatUncertainAsBad = model.TreatUncertainAsBad ?? true,
                UseSlopedExtrapolation = model.UseSlopedExtrapolation ?? true
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static SimpleAttributeOperand? ToStackModel(this SimpleAttributeOperandModel? model,
            IServiceMessageContext context)
        {
            if (model is null)
            {
                return null;
            }
            return new SimpleAttributeOperand
            {
                TypeDefinitionId = model.TypeDefinitionId.ToNodeId(context),
                AttributeId = (uint)(model.AttributeId ?? NodeAttribute.Value),
                BrowsePath = new QualifiedNameCollection(model.BrowsePath == null ?
                    Enumerable.Empty<QualifiedName>() :
                    model.BrowsePath.Select(n => n.ToQualifiedName(context))),
                IndexRange = model.IndexRange
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <param name="namespaceFormat"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static SimpleAttributeOperandModel? ToServiceModel(this SimpleAttributeOperand? model,
            IServiceMessageContext context, NamespaceFormat namespaceFormat)
        {
            if (model is null)
            {
                return null;
            }
            return new SimpleAttributeOperandModel
            {
                TypeDefinitionId = model.TypeDefinitionId
                    .AsString(context, namespaceFormat),
                AttributeId = (NodeAttribute)model.AttributeId,
                BrowsePath = model.BrowsePath?
                    .Select(p => p.AsString(context, namespaceFormat))
                    .ToArray(),
                IndexRange = model.IndexRange
            };
        }

        /// <summary>
        /// Convert user token policies to service model
        /// </summary>
        /// <param name="policies"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static IReadOnlyList<AuthenticationMethodModel> ToServiceModel(
            this UserTokenPolicyCollection policies, IJsonSerializer serializer)
        {
            if (policies == null || policies.Count == 0)
            {
                return new List<AuthenticationMethodModel>
                {
                     new() {
                         Id = "Anonymous",
                         CredentialType = CredentialType.None
                     }
                };
            }
            return policies
                .Select(p => p.ToServiceModel(serializer)!)
                .Where(p => p != null)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Convert user token policy to service model
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static AuthenticationMethodModel? ToServiceModel(
            this UserTokenPolicy? policy, IJsonSerializer serializer)
        {
            if (policy == null)
            {
                return null;
            }
            var configuration = VariantValue.Null;
            var credentialType = CredentialType.None;
            switch (policy.TokenType)
            {
                case UserTokenType.Anonymous:
                    break;
                case UserTokenType.UserName:
                    credentialType = CredentialType.UserName;
                    break;
                case UserTokenType.Certificate:
                    credentialType = CredentialType.X509Certificate;
                    configuration = policy.IssuerEndpointUrl;
                    break;
                case UserTokenType.IssuedToken:
                    switch (policy.IssuedTokenType)
                    {
                        case "http://opcfoundation.org/UA/UserToken#JWT":
                            credentialType = CredentialType.JwtToken;
                            try
                            {
                                // See part 6
                                configuration = serializer.Parse(policy.IssuerEndpointUrl);
                            }
                            catch
                            {
                                // Store as string
                                configuration = policy.IssuerEndpointUrl;
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
            return new AuthenticationMethodModel
            {
                Id = policy.PolicyId,
                SecurityPolicy = policy.SecurityPolicyUri,
                Configuration = configuration,
                CredentialType = credentialType
            };
        }

        /// <summary>
        /// Makes a user identity
        /// </summary>
        /// <param name="credential"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        public static async ValueTask<IUserIdentity> ToUserIdentityAsync(
            this CredentialModel? credential, ApplicationConfiguration configuration)
        {
            if (credential == null || credential.Type == CredentialType.None)
            {
                return new UserIdentity(new AnonymousIdentityToken());
            }
            var identity = credential.Value;
            if (identity == null)
            {
                throw new ServiceResultException(StatusCodes.BadInvalidArgument,
                    $"Credential type {credential.Type} requires providing a credential value.");
            }
            switch (credential.Type)
            {
                case CredentialType.UserName:
                    return new UserIdentity(identity.User, identity.Password);
                case CredentialType.X509Certificate:
                    var subjectName = identity.User;
                    var thumbprint = identity.Thumbprint;
                    var passCode = identity.Password;
                    if (thumbprint != null || subjectName != null)
                    {
                        using var users = configuration.SecurityConfiguration
                            .TrustedUserCertificates.OpenStore();
                        var userCertWithPrivateKey = await users.LoadPrivateKey(
                            thumbprint, subjectName, passCode).ConfigureAwait(false);
                        if (userCertWithPrivateKey == null)
                        {
                            throw new ServiceResultException(StatusCodes.BadCertificateInvalid,
                                $"User certificate for {subjectName ?? thumbprint} missing " +
                                "or provided password invalid. Please configure the User " +
                                "Certificate correctly in the User certificate store.");
                        }
                        return new UserIdentity(userCertWithPrivateKey);
                    }
                    throw new ServiceResultException(StatusCodes.BadNotSupported,
                       "X509Certificate credential requires to set either a thumbprint or subject name (user).");
                case CredentialType.None:
                    return new UserIdentity(new AnonymousIdentityToken());
                default:
                    throw new ServiceResultException(StatusCodes.BadNotSupported,
                        $"Credential type {credential!.Type} is not supported");
            }
        }
    }
}
