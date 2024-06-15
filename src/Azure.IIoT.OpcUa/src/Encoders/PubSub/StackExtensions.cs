// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Metadata extensions
    /// </summary>
    internal static class StackExtensions
    {
        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this PublishedDataSetMetaDataModel? model,
            PublishedDataSetMetaDataModel? that)
        {
            if (model == null && that == null)
            {
                return true;
            }
            if (model == null || that == null)
            {
                return false;
            }
            return
                model.DataSetMetaData.IsSameAs(that.DataSetMetaData) &&
                model.MinorVersion == that.MinorVersion &&
                model.StructureDataTypes.SequenceEqualsSafe(that.StructureDataTypes, IsSameAs) &&
                model.EnumDataTypes.SequenceEqualsSafe(that.EnumDataTypes, IsSameAs) &&
                model.SimpleDataTypes.SequenceEqualsSafe(that.SimpleDataTypes, IsSameAs) &&
                model.Fields.SequenceEqualsSafe(that.Fields, IsSameAs)
               ;
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static DataSetMetaDataType ToStackModel(
            this PublishedDataSetMetaDataModel model, IServiceMessageContext context)
        {
            return new DataSetMetaDataType
            {
                Name = model.DataSetMetaData.Name,
                Description = model.DataSetMetaData.Description,
                DataSetClassId = (Uuid)model.DataSetMetaData.DataSetClassId,
                Namespaces = context.NamespaceUris.ToArray(),
                StructureDataTypes = model.StructureDataTypes?
                    .Select(s => s.ToStackModel(context)).ToArray(),
                EnumDataTypes = model.EnumDataTypes?
                    .Select(e => e.ToStackModel(context)).ToArray(),
                SimpleDataTypes = model.SimpleDataTypes?
                    .Select(e => e.ToStackModel(context)).ToArray(),
                Fields = model.Fields
                    .Select(e => e.ToStackModel(context)).ToArray(),
                ConfigurationVersion = new ConfigurationVersionDataType
                {
                    MajorVersion = model.DataSetMetaData.MajorVersion ?? 1,
                    MinorVersion = model.MinorVersion
                }
            };
        }

        /// <summary>
        /// Convert to stack model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static PublishedDataSetMetaDataModel? ToServiceModel(
            this DataSetMetaDataType? model, IServiceMessageContext? context = null)
        {
            if (model == null)
            {
                return null;
            }
            var localContext = new ServiceMessageContext();
            if (model.Namespaces != null)
            {
                foreach (var ns in model.Namespaces)
                {
                    localContext.NamespaceUris.GetIndexOrAppend(ns);
                }
            }
            if (context != null)
            {
                for (var i = 0; i < context.NamespaceUris.Count; i++)
                {
                    localContext.NamespaceUris.GetIndexOrAppend(
                        context.NamespaceUris.GetString((uint)i));
                }
            }
            context = localContext;
            return new PublishedDataSetMetaDataModel
            {
                DataSetMetaData = new DataSetMetaDataModel
                {
                    Name = model.Name,
                    Description = model.Description.AsString(),
                    DataSetClassId = model.DataSetClassId,
                    MajorVersion = model.ConfigurationVersion.MajorVersion
                },
                MinorVersion = model.ConfigurationVersion.MinorVersion,
                StructureDataTypes = model.StructureDataTypes?
                    .Select(s => s.ToServiceModel(context)).ToArray(),
                EnumDataTypes = model.EnumDataTypes?
                    .Select(e => e.ToServiceModel(context)).ToArray(),
                SimpleDataTypes = model.SimpleDataTypes?
                    .Select(e => e.ToServiceModel(context)).ToArray(),
                Fields = model.Fields
                    .Select(e => e.ToServiceModel(context)).ToArray()
            };
        }

        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        private static bool IsSameAs(this EnumDescriptionModel? model,
            EnumDescriptionModel? that)
        {
            if (model == null && that == null)
            {
                return true;
            }
            if (model == null || that == null)
            {
                return false;
            }
            return
                model.BuiltInType == that.BuiltInType &&
                model.DataTypeId == that.DataTypeId &&
                model.Name == that.Name &&
                model.IsOptionSet == that.IsOptionSet &&
                model.Fields.SequenceEqualsSafe(that.Fields, IsSameAs)
               ;
        }

        /// <summary>
        /// Convert to type description
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static EnumDescription ToStackModel(this EnumDescriptionModel model,
            IServiceMessageContext context)
        {
            return new EnumDescription
            {
                BuiltInType = model.BuiltInType ?? (byte)BuiltInType.Null,
                DataTypeId = model.DataTypeId.ToNodeId(context),
                Name = model.Name.ToQualifiedName(context),
                EnumDefinition = new EnumDefinition
                {
                    Fields = model.Fields.Select(f => f.ToStackModel()).ToArray(),
                    IsOptionSet = model.IsOptionSet
                }
            };
        }

        /// <summary>
        /// Convert to type description
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static EnumDescriptionModel ToServiceModel(this EnumDescription model,
            IServiceMessageContext context)
        {
            ArgumentNullException.ThrowIfNull(model.EnumDefinition);
            return new EnumDescriptionModel
            {
                BuiltInType = model.BuiltInType,
                DataTypeId = (model.DataTypeId
                    .AsString(context, NamespaceFormat.Expanded)) ?? string.Empty,
                Name = model.Name
                    .AsString(context, NamespaceFormat.Expanded),
                Fields = model.EnumDefinition.Fields
                    .Select(f => f.ToServiceModel()).ToArray(),
                IsOptionSet = model.EnumDefinition.IsOptionSet
            };
        }

        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        private static bool IsSameAs(this EnumFieldDescriptionModel? model,
            EnumFieldDescriptionModel? that)
        {
            if (model == null && that == null)
            {
                return true;
            }
            if (model == null || that == null)
            {
                return false;
            }
            return
                model.Name == that.Name &&
                model.DisplayName == that.DisplayName &&
                model.Value == that.Value;
        }

        /// <summary>
        /// Convert to field
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static EnumField ToStackModel(this EnumFieldDescriptionModel model)
        {
            return new EnumField
            {
                Name = model.Name,
                DisplayName = model.DisplayName,
                Value = model.Value
            };
        }

        /// <summary>
        /// Convert to field
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static EnumFieldDescriptionModel ToServiceModel(this EnumField model)
        {
            return new EnumFieldDescriptionModel
            {
                Name = model.Name,
                DisplayName = model.DisplayName.AsString(),
                Value = model.Value
            };
        }

        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        private static bool IsSameAs(this StructureDescriptionModel? model,
            StructureDescriptionModel? that)
        {
            if (model == null && that == null)
            {
                return true;
            }
            if (model == null || that == null)
            {
                return false;
            }
            return
                model.Name == that.Name &&
                model.DataTypeId == that.DataTypeId &&
                model.BaseDataType == that.BaseDataType &&
                model.DefaultEncodingId == that.DefaultEncodingId &&
                model.StructureType == that.StructureType &&
                model.Fields.SequenceEqualsSafe(that.Fields, IsSameAs);
        }

        /// <summary>
        /// Convert to type description
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static StructureDescription ToStackModel(
            this StructureDescriptionModel model, IServiceMessageContext context)
        {
            return new StructureDescription
            {
                Name = model.Name.ToQualifiedName(context),
                DataTypeId = model.DataTypeId.ToNodeId(context),
                StructureDefinition = new StructureDefinition
                {
                    BaseDataType = model.BaseDataType.ToNodeId(context),
                    DefaultEncodingId = model.DefaultEncodingId.ToNodeId(context),
                    FirstExplicitFieldIndex = 0,
                    StructureType = model.StructureType.ToStackType(),
                    Fields = model.Fields.Select(f => f.ToStackModel(context)).ToArray()
                }
            };
        }

        /// <summary>
        /// Convert to type description
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static StructureDescriptionModel ToServiceModel(
            this StructureDescription model, IServiceMessageContext context)
        {
            ArgumentNullException.ThrowIfNull(model.StructureDefinition);
            return new StructureDescriptionModel
            {
                Name = model.Name
                    .AsString(context, NamespaceFormat.Expanded),
                DataTypeId = (model.DataTypeId
                    .AsString(context, NamespaceFormat.Expanded)) ?? string.Empty,
                BaseDataType = model.StructureDefinition.BaseDataType
                    .AsString(context, NamespaceFormat.Expanded),
                DefaultEncodingId = model.StructureDefinition.DefaultEncodingId
                    .AsString(context, NamespaceFormat.Expanded),
                // FirstExplicitFieldIndex = 0,
                StructureType = model.StructureDefinition.StructureType.ToServiceType(),
                Fields = model.StructureDefinition.Fields
                    .Select(f => f.ToServiceModel(context)).ToArray()
            };
        }

        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        private static bool IsSameAs(this StructureFieldDescriptionModel? model,
            StructureFieldDescriptionModel? that)
        {
            if (model == null && that == null)
            {
                return true;
            }
            if (model == null || that == null)
            {
                return false;
            }
            return
                model.Name == that.Name &&
                model.ArrayDimensions.SequenceEqualsSafe(that.ArrayDimensions) &&
                model.IsOptional == that.IsOptional &&
                model.DataType == that.DataType &&
                model.Description == that.Description &&
                model.MaxStringLength == that.MaxStringLength &&
                model.ValueRank == that.ValueRank;
        }

        /// <summary>
        /// Convert to field
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static StructureField ToStackModel(
            this StructureFieldDescriptionModel model, IServiceMessageContext context)
        {
            return new StructureField
            {
                Name = model.Name,
                ArrayDimensions = model.ArrayDimensions?
                    .ToArray(),
                IsOptional = model.IsOptional,
                DataType = model.DataType
                    .ToNodeId(context),
                Description = model.Description,
                MaxStringLength = model.MaxStringLength,
                ValueRank = model.ValueRank
            };
        }

        /// <summary>
        /// Convert to field
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static StructureFieldDescriptionModel ToServiceModel(
            this StructureField model, IServiceMessageContext context)
        {
            return new StructureFieldDescriptionModel
            {
                Name = model.Name,
                ArrayDimensions = model.ArrayDimensions?
                    .ToArray(),
                IsOptional = model.IsOptional,
                DataType = (model.DataType
                    .AsString(context, NamespaceFormat.Expanded)) ?? string.Empty,
                Description = model.Description.AsString(),
                MaxStringLength = model.MaxStringLength,
                ValueRank = model.ValueRank
            };
        }

        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        private static bool IsSameAs(this SimpleTypeDescriptionModel? model,
            SimpleTypeDescriptionModel? that)
        {
            if (model == null && that == null)
            {
                return true;
            }
            if (model == null || that == null)
            {
                return false;
            }
            return
                model.BaseDataType == that.BaseDataType &&
                model.BuiltInType == that.BuiltInType &&
                model.DataTypeId == that.DataTypeId &&
                model.Name == that.Name;
        }

        /// <summary>
        /// Convert to type description
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static SimpleTypeDescription ToStackModel(
            this SimpleTypeDescriptionModel model, IServiceMessageContext context)
        {
            return new SimpleTypeDescription
            {
                BaseDataType = model.BaseDataType.ToNodeId(context),
                BuiltInType = model.BuiltInType ?? (byte)BuiltInType.Null,
                Name = model.Name.ToQualifiedName(context),
                DataTypeId = model.DataTypeId.ToNodeId(context)
            };
        }

        /// <summary>
        /// Convert to type description
        /// </summary>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static SimpleTypeDescriptionModel ToServiceModel(
            this SimpleTypeDescription model, IServiceMessageContext context)
        {
            return new SimpleTypeDescriptionModel
            {
                BaseDataType = model.BaseDataType
                    .AsString(context, NamespaceFormat.Expanded),
                BuiltInType = model.BuiltInType,
                Name = model.Name
                    .AsString(context, NamespaceFormat.Expanded),
                DataTypeId = (model.DataTypeId
                    .AsString(context, NamespaceFormat.Expanded)) ?? string.Empty
            };
        }

        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        private static bool IsSameAs(this PublishedFieldMetaDataModel? model,
            PublishedFieldMetaDataModel? that)
        {
            if (model == null && that == null)
            {
                return true;
            }
            if (model == null || that == null)
            {
                return false;
            }
            return
                model.Name == that.Name &&
                model.Id == that.Id &&
                model.ArrayDimensions.SequenceEqualsSafe(that.ArrayDimensions) &&
                model.BuiltInType == that.BuiltInType &&
                model.DataType == that.DataType &&
                model.Description == that.Description &&
                model.MaxStringLength == that.MaxStringLength &&
                model.ValueRank == that.ValueRank &&
                model.Flags == that.Flags;
        }

        /// <summary>
        /// Convert to field metadata
        /// </summary>
        /// <param name="field"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static FieldMetaData ToStackModel(
            this PublishedFieldMetaDataModel field, IServiceMessageContext context)
        {
            return new FieldMetaData
            {
                Name = field.Name,
                DataSetFieldId = (Uuid)field.Id,
                ArrayDimensions = field.ArrayDimensions?
                    .ToArray(),
                BuiltInType = field.BuiltInType,
                DataType = field.DataType
                    .ToNodeId(context),
                Description = field.Description,
                MaxStringLength = field.MaxStringLength,
                ValueRank = field.ValueRank,
                FieldFlags = field.Flags,
                Properties = null // TODO
            };
        }

        /// <summary>
        /// Convert to field metadata
        /// </summary>
        /// <param name="field"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static PublishedFieldMetaDataModel ToServiceModel(
            this FieldMetaData field, IServiceMessageContext context)
        {
            return new PublishedFieldMetaDataModel
            {
                Name = field.Name,
                Id = field.DataSetFieldId,
                ArrayDimensions = field.ArrayDimensions?
                    .ToArray(),
                BuiltInType = field.BuiltInType,
                DataType = field.DataType
                    .AsString(context, NamespaceFormat.Expanded),
                Description = field.Description.AsString(),
                MaxStringLength = field.MaxStringLength,
                ValueRank = field.ValueRank,
                Flags = field.FieldFlags,
                Properties = null // TODO
            };
        }

        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        private static bool IsSameAs(this DataSetMetaDataModel? model,
            DataSetMetaDataModel? that)
        {
            if (model == null && that == null)
            {
                return true;
            }
            if (model == null || that == null)
            {
                return false;
            }
            return
                model.Name == that.Name &&
                model.Description == that.Description &&
                model.DataSetClassId == that.DataSetClassId &&
                model.MajorVersion == that.MajorVersion;
        }
    }
}
