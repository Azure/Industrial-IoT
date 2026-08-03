// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using Xunit;
    using PublisherStructureType = global::Azure.IIoT.OpcUa.Publisher.Models.StructureType;

    public sealed class DataSetMetaDataExTests
    {
        [Fact]
        public void IsSameAsHandlesNulls()
        {
            PublishedDataSetMetaDataModel? model = null;

            Assert.Equal(true, model.IsSameAs(null));
            Assert.False(model.IsSameAs(CreateMetaData()));
            Assert.False(CreateMetaData().IsSameAs(null));
        }

        [Fact]
        public void IsSameAsReturnsTrueForEquivalentRichModels()
        {
            var first = CreateMetaData();
            var second = CreateMetaData();

            Assert.Equal(true, first.IsSameAs(second));
            Assert.Equal(true, second.IsSameAs(first));
        }

        [Theory]
        [MemberData(nameof(GetDifferentMetaData))]
        public void IsSameAsReturnsFalseForSingleFieldDifference(string name,
            Func<PublishedDataSetMetaDataModel, PublishedDataSetMetaDataModel> mutate)
        {
            var first = CreateMetaData();
            var second = mutate(CreateMetaData());

            Assert.False(first.IsSameAs(second));
            Assert.False(second.IsSameAs(first));
        }

        [Fact]
        public void ToStackModelMapsDataSetMetadataAndNestedDescriptions()
        {
            var context = CreateContext();
            var model = CreateMetaData();

            var stack = model.ToStackModel(context);

            Assert.Equal("DataSet", stack.Name);
            Assert.Equal("Data set description", stack.Description.Text);
            Assert.Equal(model.DataSetMetaData.DataSetClassId, (Guid)stack.DataSetClassId);
            Assert.Equal(7u, stack.ConfigurationVersion.MajorVersion);
            Assert.Equal(3u, stack.ConfigurationVersion.MinorVersion);
            Assert.Contains(kCustomNamespace, stack.Namespaces.ToArray());
            var field = Assert.Single(stack.Fields.ToArray());
            Assert.Equal("Field", field.Name);
            Assert.Equal((byte)BuiltInType.String, field.BuiltInType);
            Assert.Equal(ValueRanks.OneDimension, field.ValueRank);
            Assert.Equal(new uint[] { 2, 3 }, field.ArrayDimensions);
            Assert.Equal(11u, field.MaxStringLength);
            Assert.Equal((ushort)5, field.FieldFlags);
            var enumType = Assert.Single(stack.EnumDataTypes.ToArray());
            Assert.Equal("EnumType", enumType.Name.Name);
            Assert.NotNull(enumType.EnumDefinition);
            Assert.Equal(2, enumType.EnumDefinition!.Fields.Count);
            Assert.Equal(true, enumType.EnumDefinition.IsOptionSet);
            var structureType = Assert.Single(stack.StructureDataTypes.ToArray());
            Assert.NotNull(structureType.StructureDefinition);
            Assert.Equal(Opc.Ua.StructureType.StructureWithOptionalFields,
                structureType.StructureDefinition!.StructureType);
            Assert.Equal(true, Assert.Single(
                structureType.StructureDefinition.Fields.ToArray()).IsOptional);
            var simpleType = Assert.Single(stack.SimpleDataTypes.ToArray());
            Assert.Equal((byte)BuiltInType.Double, simpleType.BuiltInType);
        }

        [Fact]
        public void ToStackModelDefaultsMissingMajorVersionToOne()
        {
            var model = CreateMetaData() with
            {
                DataSetMetaData = CreateMetaData().DataSetMetaData with
                {
                    MajorVersion = null
                }
            };

            var stack = model.ToStackModel(CreateContext());

            Assert.Equal(1u, stack.ConfigurationVersion.MajorVersion);
        }

        [Fact]
        public void ToServiceModelReturnsNullForNullStackModel()
        {
            DataSetMetaDataType? stack = null;

            var model = stack.ToServiceModel();

            Assert.Null(model);
        }

        [Fact]
        public void ToServiceModelRoundTripsStackModelWithNamespaceTable()
        {
            var context = CreateContext();
            var original = CreateMetaData();
            var stack = original.ToStackModel(context);

            var model = stack.ToServiceModel();

            Assert.NotNull(model);
            Assert.Equal(true, original.IsSameAs(model));
        }

        public static TheoryData<string,
            Func<PublishedDataSetMetaDataModel, PublishedDataSetMetaDataModel>>
            GetDifferentMetaData()
        {
            return new TheoryData<string,
                Func<PublishedDataSetMetaDataModel, PublishedDataSetMetaDataModel>>
            {
                { "data set name", model => model with
                    {
                        DataSetMetaData = model.DataSetMetaData with { Name = "Other" }
                    }
                },
                { "data set description", model => model with
                    {
                        DataSetMetaData = model.DataSetMetaData with { Description = "Other" }
                    }
                },
                { "data set class id", model => model with
                    {
                        DataSetMetaData = model.DataSetMetaData with { DataSetClassId = Guid.NewGuid() }
                    }
                },
                { "major version", model => model with
                    {
                        DataSetMetaData = model.DataSetMetaData with { MajorVersion = 8 }
                    }
                },
                { "minor version", model => model with { MinorVersion = 4 } },
                { "field name", model => model with
                    {
                        Fields = [model.Fields[0] with { Name = "OtherField" }]
                    }
                },
                { "field id", model => model with
                    {
                        Fields = [model.Fields[0] with { Id = Guid.NewGuid() }]
                    }
                },
                { "field dimensions", model => model with
                    {
                        Fields = [model.Fields[0] with { ArrayDimensions = [4] }]
                    }
                },
                { "field built in type", model => model with
                    {
                        Fields = [model.Fields[0] with { BuiltInType = (byte)BuiltInType.Double }]
                    }
                },
                { "field data type", model => model with
                    {
                        Fields = [model.Fields[0] with { DataType = "i=11" }]
                    }
                },
                { "field flags", model => model with
                    {
                        Fields = [model.Fields[0] with { Flags = 6 }]
                    }
                },
                { "enum name", model => model with
                    {
                        EnumDataTypes = [model.EnumDataTypes![0] with { Name = "OtherEnum" }]
                    }
                },
                { "enum option set", model => model with
                    {
                        EnumDataTypes = [model.EnumDataTypes![0] with { IsOptionSet = false }]
                    }
                },
                { "enum field value", model => model with
                    {
                        EnumDataTypes =
                        [
                            model.EnumDataTypes![0] with
                            {
                                Fields =
                                [
                                    model.EnumDataTypes![0].Fields[0] with { Value = 10 },
                                    model.EnumDataTypes![0].Fields[1]
                                ]
                            }
                        ]
                    }
                },
                { "structure type", model => model with
                    {
                        StructureDataTypes =
                        [
                            model.StructureDataTypes![0] with { StructureType = PublisherStructureType.Union }
                        ]
                    }
                },
                { "structure field optional", model => model with
                    {
                        StructureDataTypes =
                        [
                            model.StructureDataTypes![0] with
                            {
                                Fields =
                                [
                                    model.StructureDataTypes![0].Fields[0] with
                                    {
                                        IsOptional = false
                                    }
                                ]
                            }
                        ]
                    }
                },
                { "simple built in type", model => model with
                    {
                        SimpleDataTypes =
                        [
                            model.SimpleDataTypes![0] with { BuiltInType = (byte)BuiltInType.Float }
                        ]
                    }
                },
                { "missing enum list", model => model with { EnumDataTypes = null } },
                { "additional simple type", model => model with
                    {
                        SimpleDataTypes =
                        [
                            model.SimpleDataTypes![0],
                            new SimpleTypeDescriptionModel
                            {
                                DataTypeId = "i=1234",
                                Name = "Extra"
                            }
                        ]
                    }
                }
            };
        }

        private static PublishedDataSetMetaDataModel CreateMetaData()
        {
            return new PublishedDataSetMetaDataModel
            {
                MinorVersion = 3,
                DataSetMetaData = new DataSetMetaDataModel
                {
                    Name = "DataSet",
                    Description = "Data set description",
                    DataSetClassId = Guid.Parse("91D70F74-E169-49CB-A42D-2F5231F7CCEE"),
                    MajorVersion = 7
                },
                Fields =
                [
                    new PublishedFieldMetaDataModel
                    {
                        Name = "Field",
                        Id = Guid.Parse("55E05817-0E10-459D-B41C-53062995D3D9"),
                        Description = "Field description",
                        Flags = 5,
                        BuiltInType = (byte)BuiltInType.String,
                        DataType = "i=12",
                        ValueRank = ValueRanks.OneDimension,
                        ArrayDimensions = [2, 3],
                        MaxStringLength = 11
                    }
                ],
                EnumDataTypes =
                [
                    new EnumDescriptionModel
                    {
                        BuiltInType = (byte)BuiltInType.Int32,
                        DataTypeId = "nsu=" + kCustomNamespace + ";i=5001",
                        Name = "nsu=" + kCustomNamespace + ";EnumType",
                        IsOptionSet = true,
                        Fields =
                        [
                            new EnumFieldDescriptionModel
                            {
                                Name = "Zero",
                                DisplayName = "Zero display",
                                Value = 0
                            },
                            new EnumFieldDescriptionModel
                            {
                                Name = "One",
                                DisplayName = "One display",
                                Value = 1
                            }
                        ]
                    }
                ],
                StructureDataTypes =
                [
                    new StructureDescriptionModel
                    {
                        DataTypeId = "nsu=" + kCustomNamespace + ";s=StructureType",
                        Name = "nsu=" + kCustomNamespace + ";StructureType",
                        BaseDataType = "i=22",
                        DefaultEncodingId = "nsu=" + kCustomNamespace + ";s=StructureEncoding",
                        StructureType = PublisherStructureType.StructureWithOptionalFields,
                        Fields =
                        [
                            new StructureFieldDescriptionModel
                            {
                                Name = "Nested",
                                Description = "Nested description",
                                DataType = "i=12",
                                IsOptional = true,
                                ValueRank = ValueRanks.Scalar,
                                ArrayDimensions = [9],
                                MaxStringLength = 10
                            }
                        ]
                    }
                ],
                SimpleDataTypes =
                [
                    new SimpleTypeDescriptionModel
                    {
                        BaseDataType = "i=11",
                        BuiltInType = (byte)BuiltInType.Double,
                        DataTypeId = "nsu=" + kCustomNamespace + ";s=SimpleType",
                        Name = "nsu=" + kCustomNamespace + ";SimpleType"
                    }
                ]
            };
        }

        private static ServiceMessageContext CreateContext()
        {
            var context = new ServiceMessageContext();
            context.NamespaceUris.GetIndexOrAppend(kCustomNamespace);
            return context;
        }

        private const string kCustomNamespace = "http://microsoft.com/Opc/OpcPublisher/Tests";
    }
}

