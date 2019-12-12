// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using Newtonsoft.Json.Linq;
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
        /// Convert service model to role permission type
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static RolePermissionType ToStackModel(this RolePermissionModel model,
            ServiceMessageContext context) {
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
            ServiceMessageContext context) {
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
        /// <param name="codec"></param>
        /// <returns></returns>
        public static EventFilter ToStackModel(this EventFilterModel model,
            IVariantEncoder codec) {
            if (model == null) {
                return null;
            }
            return new EventFilter {
                SelectClauses = new SimpleAttributeOperandCollection(
                    model.SelectClauses == null ? Enumerable.Empty<SimpleAttributeOperand>() :
                    model.SelectClauses.Select(c => c.ToStackModel(codec.Context))),
                //
                // Per Part 4 only allow simple attribute operands in where clause
                // elements of event filters.
                //
                WhereClause = model.WhereClause.ToStackModel(codec, true)

            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        public static EventFilterModel ToServiceModel(this EventFilter model,
            IVariantEncoder codec) {
            if (model == null) {
                return null;
            }
            return new EventFilterModel {
                SelectClauses = model.SelectClauses?
                    .Select(c => c.ToServiceModel(codec.Context))
                    .ToList(),
                WhereClause = model.WhereClause.ToServiceModel(codec)
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="codec"></param>
        /// <param name="onlySimpleAttributeOperands"></param>
        /// <returns></returns>
        public static ContentFilter ToStackModel(this ContentFilterModel model,
            IVariantEncoder codec, bool onlySimpleAttributeOperands = false) {
            if (model == null) {
                return null;
            }
            return new ContentFilter {
                Elements = new ContentFilterElementCollection(model.Elements == null ?
                    Enumerable.Empty<ContentFilterElement>() : model.Elements
                        .Select(e => e.ToStackModel(codec, onlySimpleAttributeOperands)))
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        public static ContentFilterModel ToServiceModel(this ContentFilter model,
            IVariantEncoder codec) {
            if (model == null) {
                return null;
            }
            return new ContentFilterModel {
                Elements = model.Elements?
                    .Select(e => e.ToServiceModel(codec))
                    .ToList()
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="codec"></param>
        /// <param name="onlySimpleAttributeOperands"></param>
        /// <returns></returns>
        public static ContentFilterElement ToStackModel(this ContentFilterElementModel model,
            IVariantEncoder codec, bool onlySimpleAttributeOperands = false) {
            if (model == null) {
                return null;
            }
            return new ContentFilterElement {
                FilterOperands = new ExtensionObjectCollection(model?.FilterOperands == null ?
                    Enumerable.Empty<ExtensionObject>() : model.FilterOperands
                        .Select(e => new ExtensionObject(e.ToStackModel(codec, onlySimpleAttributeOperands)))),
                FilterOperator = model.FilterOperator.ToStackType()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        public static ContentFilterElementModel ToServiceModel(this ContentFilterElement model,
            IVariantEncoder codec) {
            if (model == null) {
                return null;
            }
            return new ContentFilterElementModel {
                FilterOperands = model.FilterOperands
                    .Select(e => e.Body)
                    .Cast<FilterOperand>()
                    .Select(o => o.ToServiceModel(codec))
                    .ToList(),
                FilterOperator = model.FilterOperator.ToServiceType()
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="codec"></param>
        /// <param name="onlySimpleAttributeOperands"></param>
        /// <returns></returns>
        public static FilterOperand ToStackModel(this FilterOperandModel model,
            IVariantEncoder codec, bool onlySimpleAttributeOperands = false) {
            if (model == null) {
                return null;
            }
            if (model.Index != null) {
                return new ElementOperand {
                    Index = model.Index.Value
                };
            }
            if (model.Value != null) {
                return new LiteralOperand {
                    Value = codec.Decode(model.Value)
                };
            }
            if (model.Alias != null && !onlySimpleAttributeOperands) {
                return new AttributeOperand {
                    Alias = model.Alias,
                    NodeId = model.NodeId.ToNodeId(codec.Context),
                    AttributeId = (uint)(model.AttributeId ?? NodeAttribute.Value),
                    BrowsePath = model.BrowsePath.ToRelativePath(codec.Context),
                    IndexRange = model.IndexRange
                };
            }
            return ((SimpleAttributeOperandModel)model).ToStackModel(codec.Context);
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        public static FilterOperandModel ToServiceModel(this FilterOperand model,
            IVariantEncoder codec) {
            if (model == null) {
                return null;
            }
            switch (model) {
                case ElementOperand elem:
                    return new FilterOperandModel {
                        Index = elem.Index
                    };
                case LiteralOperand lit:
                    return new FilterOperandModel {
                        Value = codec.Encode(lit.Value)
                    };
                case AttributeOperand attr:
                    return new FilterOperandModel {
                        NodeId = attr.NodeId.AsString(codec.Context),
                        AttributeId = (NodeAttribute)attr.AttributeId,
                        BrowsePath = attr.BrowsePath.AsString(codec.Context),
                        IndexRange = attr.IndexRange,
                        Alias = attr.Alias
                    };
                case SimpleAttributeOperand sattr:
                    return new FilterOperandModel {
                        NodeId = sattr.TypeDefinitionId.AsString(codec.Context),
                        AttributeId = (NodeAttribute)sattr.AttributeId,
                        BrowsePath = sattr.BrowsePath?.Select(p => p.AsString(codec.Context)).ToArray(),
                        IndexRange = sattr.IndexRange
                    };
                default:
                    throw new NotSupportedException("Operand not supported");
            }
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static SimpleAttributeOperand ToStackModel(this SimpleAttributeOperandModel model,
            ServiceMessageContext context) {
            if (model == null) {
                return null;
            }
            return new SimpleAttributeOperand {
                TypeDefinitionId = model.NodeId.ToNodeId(context),
                AttributeId = (uint)(model.AttributeId ?? NodeAttribute.Value),
                BrowsePath = new QualifiedNameCollection(model.BrowsePath == null ?
                    Enumerable.Empty<QualifiedName>() :
                    model.BrowsePath?.Select(n => n.ToQualifiedName(context))),
                IndexRange = model.IndexRange
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static SimpleAttributeOperandModel ToServiceModel(this SimpleAttributeOperand model,
            ServiceMessageContext context) {
            if (model == null) {
                return null;
            }
            return new FilterOperandModel {
                NodeId = model.TypeDefinitionId.AsString(context),
                AttributeId = (NodeAttribute)model.AttributeId,
                BrowsePath = model.BrowsePath?.Select(p => p.AsString(context)).ToArray(),
                IndexRange = model.IndexRange
            };
        }

        /// <summary>
        /// Convert user token policies to service model
        /// </summary>
        /// <param name="policies"></param>
        /// <returns></returns>
        public static List<AuthenticationMethodModel> ToServiceModel(
            this UserTokenPolicyCollection policies) {
            if (policies == null || policies.Count == 0) {
                return new List<AuthenticationMethodModel>{
                     new AuthenticationMethodModel {
                         Id = "Anonymous",
                         CredentialType = CredentialType.None
                     }
                };
            }
            return policies
                .Select(p => p.ToServiceModel())
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
        /// <returns></returns>
        public static AuthenticationMethodModel ToServiceModel(this UserTokenPolicy policy) {
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
                                result.Configuration = JToken.Parse(
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
                    if (authentication.Value is JObject o &&
                        o.TryGetValue("user", StringComparison.InvariantCultureIgnoreCase,
                            out var user) && user.Type == JTokenType.String &&
                        o.TryGetValue("password", StringComparison.InvariantCultureIgnoreCase,
                            out var password) && password.Type == JTokenType.String) {
                        return new UserIdentity((string)user, (string)password);
                    }
                    throw new ServiceResultException(StatusCodes.BadNotSupported,
                        $"User/passord token format is not supported.");
                case CredentialType.X509Certificate:
                    return new UserIdentity(new X509Certificate2(
                        authentication.Value?.ToObject<byte[]>()));
                case CredentialType.JwtToken:
                    return new UserIdentity(new IssuedIdentityToken {
                        DecryptedTokenData = authentication.Value?.ToObject<byte[]>()
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
                    if (authentication.Value is JObject o &&
                        o.TryGetValue("user", StringComparison.InvariantCultureIgnoreCase,
                            out var user) && user.Type == JTokenType.String &&
                        o.TryGetValue("password", StringComparison.InvariantCultureIgnoreCase,
                            out var password) && password.Type == JTokenType.String) {
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
                        authentication.Value?.ToObject<byte[]>())
                    };
                case CredentialType.JwtToken:
                    return new IssuedIdentityToken {
                        DecryptedTokenData = authentication.Value?.ToObject<byte[]>()
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
        /// <returns></returns>
        public static CredentialModel ToServiceModel(this IUserIdentity identity) {
            return ToServiceModel(identity?.GetIdentityToken());
        }

        /// <summary>
        /// Convert user identity token to service model
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static CredentialModel ToServiceModel(this UserIdentityToken token) {
            if (token == null) {
                return null;  // Treat as anonymous
            }
            switch (token) {
                case IssuedIdentityToken it:
                    switch (it.IssuedTokenType) {
                        case IssuedTokenType.JWT:
                            return new CredentialModel {
                                Type = CredentialType.JwtToken,
                                Value = JToken.FromObject(it.DecryptedTokenData)
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
                        Value = JToken.FromObject(new {
                            user = un.UserName,
                            password = un.DecryptedPassword
                        })
                    };
                case X509IdentityToken x5:
                    return new CredentialModel {
                        Type = CredentialType.X509Certificate,
                        Value = JToken.FromObject(x5.CertificateData)
                    };
                default:
                    throw new ServiceResultException(StatusCodes.BadNotSupported,
                        $"User identity token type {token.GetType()} is unsupported");
            }
        }
    }
}
