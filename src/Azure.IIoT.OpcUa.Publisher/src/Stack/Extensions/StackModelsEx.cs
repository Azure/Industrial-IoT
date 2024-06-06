// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
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
        /// <param name="timeoutHint"></param>
        /// <returns></returns>
        public static RequestHeader ToRequestHeader(this RequestHeaderModel? header,
            uint timeoutHint = 0)
        {
            return (header?.Diagnostics?.Level).ToRequestHeader(header?.Diagnostics?.AuditId,
                header?.Diagnostics?.TimeStamp, timeoutHint);
        }

        /// <summary>
        /// Convert diagnostics to request header
        /// </summary>
        /// <param name="context"></param>
        /// <param name="level"></param>
        /// <param name="timestamp"></param>
        /// <param name="timeoutHint"></param>
        /// <returns></returns>
        public static RequestHeader ToRequestHeader(this OperationContextModel? context,
            DiagnosticsLevel? level = null, DateTime? timestamp = null,
            uint timeoutHint = 0)
        {
            return level.ToRequestHeader(context?.AuthorityId, timestamp, timeoutHint);
        }

        /// <summary>
        /// Convert diagnostics to request header
        /// </summary>
        /// <param name="level"></param>
        /// <param name="auditId"></param>
        /// <param name="timestamp"></param>
        /// <param name="timeoutHint"></param>
        /// <returns></returns>
        public static RequestHeader ToRequestHeader(this DiagnosticsLevel? level,
            string? auditId = null, DateTime? timestamp = null, uint timeoutHint = 0)
        {
            return new RequestHeader
            {
                AuditEntryId = auditId ?? Guid.NewGuid().ToString(),
                ReturnDiagnostics =
                    (uint)(level ?? DiagnosticsLevel.Status)
                     .ToStackType(),
                Timestamp = timestamp ?? DateTime.UtcNow,
                TimeoutHint = timeoutHint,
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

        /// <summary>
        /// Get metdata from data items
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataSetMetaDataType? EncodeMetaData(this IVariantEncoder encoder,
            DataSetWriterModel model)
        {
            var dataSet = model.DataSet;
            if (dataSet?.DataSetSource == null || dataSet.DataSetMetaData == null)
            {
                return null;
            }

            var types = new Dictionary<string, DataTypeDescription>();
            var fields = new List<FieldMetaData>();
            var minorVersion = 0u;
            if (dataSet.DataSetSource.PublishedVariables?.PublishedData != null)
            {
                foreach (var item in dataSet.DataSetSource.PublishedVariables.PublishedData)
                {
                    CollectFieldMetaData(encoder, item.DataSetFieldName,
                        item.DataSetClassFieldId, item.MetaData, types, fields,
                        ref minorVersion);
                }
            }

            if (dataSet.DataSetSource.PublishedEvents?.PublishedData != null)
            {
                foreach (var evt in dataSet.DataSetSource.PublishedEvents.PublishedData)
                {
                    if (evt.SelectedFields != null)
                    {
                        foreach (var item in evt.SelectedFields)
                        {
                            CollectFieldMetaData(encoder, item.DisplayName,
                                item.DataSetClassFieldId, item.MetaData, types,
                                fields, ref minorVersion);
                        }
                    }
                }
            }

            if (dataSet.ExtensionFields != null)
            {
                foreach (var item in dataSet.ExtensionFields)
                {
                    CollectFieldMetaData(encoder, item.DataSetFieldName,
                        item.DataSetClassFieldId, item.MetaData, types, fields,
                        ref minorVersion);
                }
            }

            return new DataSetMetaDataType
            {
                Name = dataSet.DataSetMetaData.Name,
                DataSetClassId = (Uuid)dataSet.DataSetMetaData.DataSetClassId,
                Description = dataSet.DataSetMetaData.Description,
                ConfigurationVersion = new ConfigurationVersionDataType
                {
                    MajorVersion = dataSet.DataSetMetaData.MajorVersion ?? 1u,
                    MinorVersion = minorVersion
                },
                Fields = fields.ToArray(),
                Namespaces = encoder.Context.NamespaceUris.ToArray(),
                EnumDataTypes = types.Values.OfType<EnumDescription>().ToArray(),
                SimpleDataTypes = types.Values.OfType<SimpleTypeDescription>().ToArray(),
                StructureDataTypes = types.Values.OfType<StructureDescription>().ToArray()
            };
        }

        /// <summary>
        /// Get metdata from data items
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="fieldName"></param>
        /// <param name="fieldGuid"></param>
        /// <param name="fieldMetaData"></param>
        /// <param name="majorVersion"></param>
        /// <returns></returns>
        public static DataSetMetaDataType? EncodeMetaData(this IVariantEncoder encoder,
            string? fieldName, Guid fieldGuid, PublishedMetaDataModel fieldMetaData,
            uint majorVersion = 0)
        {
            var types = new Dictionary<string, DataTypeDescription>();
            var fields = new List<FieldMetaData>();
            var minorVersion = 0u;

            CollectFieldMetaData(encoder, fieldName, fieldGuid, fieldMetaData, types,
                fields, ref minorVersion);
            return new DataSetMetaDataType
            {
                Name = fieldName,
                DataSetClassId = (Uuid)fieldGuid,
                ConfigurationVersion = new ConfigurationVersionDataType
                {
                    MajorVersion = majorVersion,
                    MinorVersion = minorVersion
                },
                Fields = fields.ToArray(),
                Namespaces = encoder.Context.NamespaceUris.ToArray(),
                EnumDataTypes = types.Values.OfType<EnumDescription>().ToArray(),
                SimpleDataTypes = types.Values.OfType<SimpleTypeDescription>().ToArray(),
                StructureDataTypes = types.Values.OfType<StructureDescription>().ToArray()
            };
        }

        /// <summary>
        /// Collect fields
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="fieldName"></param>
        /// <param name="fieldGuid"></param>
        /// <param name="fieldMetaData"></param>
        /// <param name="types"></param>
        /// <param name="fields"></param>
        /// <param name="maxMinorVersion"></param>
        private static void CollectFieldMetaData(IVariantEncoder encoder, string? fieldName,
            Guid fieldGuid, PublishedMetaDataModel? fieldMetaData, Dictionary<string,
                DataTypeDescription> types, List<FieldMetaData> fields, ref uint maxMinorVersion)
        {
            fields.Add(new FieldMetaData
            {
                DataSetFieldId = (Uuid)fieldGuid,
                Name = fieldName,
                DataType = fieldMetaData?.DataType.ToNodeId(encoder.Context),
                BuiltInType = fieldMetaData?.BuiltInType ?? (int)BuiltInType.Variant,
                Description = fieldMetaData?.Description,
                FieldFlags = fieldMetaData?.Flags ?? 0,
                MaxStringLength = fieldMetaData?.MaxStringLength ?? 0u,
                Properties = fieldMetaData?.Properties?
                    .Select(e => new Opc.Ua.KeyValuePair
                    {
                        Key = e.Id,
                        Value = encoder.Decode(e.Value, BuiltInType.Variant)
                    })
                    .ToArray(),
                ArrayDimensions = fieldMetaData?.ArrayDimensions?.ToArray(),
                ValueRank = fieldMetaData?.ValueRank ?? ValueRanks.Scalar
            });

            if (fieldMetaData == null)
            {
                return;
            }

            if (fieldMetaData.MinorVersion > maxMinorVersion)
            {
                maxMinorVersion = fieldMetaData.MinorVersion;
            }

            if (fieldMetaData.SimpleDataTypes != null)
            {
                foreach (var simple in fieldMetaData.SimpleDataTypes)
                {
                    if (types.ContainsKey(simple.DataTypeId))
                    {
                        continue;
                    }
                    types.Add(simple.DataTypeId, new SimpleTypeDescription
                    {
                        BaseDataType = simple.BaseDataType.ToNodeId(encoder.Context),
                        BuiltInType = simple.BuiltInType ?? (int)BuiltInType.Variant,
                        DataTypeId = simple.DataTypeId.ToNodeId(encoder.Context),
                        Name = simple.Name.ToQualifiedName(encoder.Context)
                    });
                }
            }

            if (fieldMetaData.StructureDataTypes != null)
            {
                foreach (var structure in fieldMetaData.StructureDataTypes)
                {
                    if (types.ContainsKey(structure.DataTypeId))
                    {
                        continue;
                    }
                    types.Add(structure.DataTypeId, new StructureDescription
                    {
                        DataTypeId = structure.DataTypeId.ToNodeId(encoder.Context),
                        Name = structure.Name.ToQualifiedName(encoder.Context),
                        StructureDefinition = new StructureDefinition
                        {
                            BaseDataType = structure.BaseDataType.ToNodeId(encoder.Context),
                            DefaultEncodingId = structure.DefaultEncodingId
                                .ToNodeId(encoder.Context),
                            StructureType = (Opc.Ua.StructureType)(structure.StructureType
                                ?? Publisher.Models.StructureType.Structure),
                            Fields = structure.Fields
                                .Select(f => new StructureField
                                {
                                    Name = f.Name,
                                    DataType = f?.DataType.ToNodeId(encoder.Context),
                                    Description = f?.Description,
                                    MaxStringLength = f?.MaxStringLength ?? 0u,
                                    ArrayDimensions = f?.ArrayDimensions?.ToArray(),
                                    ValueRank = f?.ValueRank ?? ValueRanks.Scalar
                                })
                                .ToArray()
                        }
                    });
                }
            }

            if (fieldMetaData.EnumDataTypes != null)
            {
                foreach (var enumType in fieldMetaData.EnumDataTypes)
                {
                    if (types.ContainsKey(enumType.DataTypeId))
                    {
                        continue;
                    }
                    types.Add(enumType.DataTypeId, new EnumDescription
                    {
                        DataTypeId = enumType.DataTypeId.ToNodeId(encoder.Context),
                        Name = enumType.Name.ToQualifiedName(encoder.Context),
                        BuiltInType = enumType.BuiltInType ?? (int)BuiltInType.Variant,
                        EnumDefinition = new EnumDefinition
                        {
                            IsOptionSet = enumType.IsOptionSet,
                            Fields = enumType.Fields
                                .Select(f => new EnumField
                                {
                                    Name = f.Name,
                                    DisplayName = f.DisplayName,
                                    Value = f.Value,
                                    Description = f?.Description
                                })
                                .ToArray()
                        }
                    });
                }
            }
        }
    }
}
