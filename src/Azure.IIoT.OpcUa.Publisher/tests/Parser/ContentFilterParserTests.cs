// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
#nullable enable
namespace Azure.IIoT.OpcUa.Publisher.Parser.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Json;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class ContentFilterParserTests
    {
        private readonly IJsonSerializer _serializer = new DefaultJsonSerializer();
        private readonly ITestOutputHelper _output;

        public ContentFilterParserTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task AnnexBPart1Example1Async()
        {
            // (((AType.A = 5) or InList(BType.B, 3,5,7)) and BaseObjectType.displayName LIKE "Main%")
            const string query = @"
    select *
    from BaseObjectType, AType, BType
    where ((AType/A = 5) or BType/B in (3,5,7)) and BaseObjectType.displayName like 'Main%'
";
            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData("AType", new [] { "/A" }, "A"),
                new IdentifierMetaData("BType", new [] { "/B" }, "B")
            };

            var filter = await parser.ParseEventFilterAsync(query, context, default).ConfigureAwait(false);

            Assert.NotNull(filter);
            Assert.NotNull(filter.SelectClauses);
            Assert.NotEmpty(filter.SelectClauses);
            Assert.NotNull(filter.WhereClause);

            var expectedWhere = new ContentFilterModel
            {
                Elements = new[]
                {
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.And,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                Index = 1
                            },
                            new FilterOperandModel
                            {
                                Index = 2
                            }
                        }
                    },
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.Like,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                NodeId = "i=58",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.DisplayName
                            },
                            new FilterOperandModel
                            {
                                Value = "Main%"
                            }
                        }
                    },
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.Or,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                Index = 3
                            },
                            new FilterOperandModel
                            {
                                Index = 4
                            }
                        }
                    },
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.InList,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                NodeId = "BType",
                                BrowsePath = new [] { "/B" },
                                AttributeId = NodeAttribute.Value
                            },
                            new FilterOperandModel
                            {
                                Value = 3
                            },
                            new FilterOperandModel
                            {
                                Value = 5
                            },
                            new FilterOperandModel
                            {
                                Value = 7
                            }
                        }
                    },
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.Equals,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                NodeId = "AType",
                                BrowsePath = new [] { "/A" },
                                AttributeId = NodeAttribute.Value
                            },
                            new FilterOperandModel
                            {
                                Value = 5
                            }
                        }
                    }
                }
            };

            var evtFilter = _serializer.SerializeToString(filter.WhereClause);
            var expFilter = _serializer.SerializeToString(expectedWhere);
            Assert.Equal(expFilter, evtFilter);
        }

        [Fact]
        public async Task AnnexBPart1Example2Async()
        {
            const string query = @"
    select *
    from SystemEventType, Area1, Area2
    where oftype SystemEventType and (inview Area1 or inview Area2)
";
            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.SystemEventType.ToString(), new [] { "/A" }, "A"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.SystemEventType.ToString(), new [] { "/B" }, "B")
            };

            var filter = await parser.ParseEventFilterAsync(query, context, default).ConfigureAwait(false);

            Assert.NotNull(filter);
            Assert.NotNull(filter.SelectClauses);
            Assert.NotEmpty(filter.SelectClauses);
            Assert.NotNull(filter.WhereClause);

            var expectedWhere = new ContentFilterModel
            {
                Elements = new[]
                {
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.And,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                Index = 1
                            },
                            new FilterOperandModel
                            {
                                Index = 4
                            }
                        }
                    },
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.Or,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                Index = 2
                            },
                            new FilterOperandModel
                            {
                                Index = 3
                            }
                        }
                    },
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.InView,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                NodeId = "Area2",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            }
                        }
                    },
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.InView,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                NodeId = "Area1",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            }
                        }
                    },
                    new ContentFilterElementModel
                    {
                        FilterOperator = FilterOperatorType.OfType,
                        FilterOperands = new [] {
                            new FilterOperandModel
                            {
                                // Literal
                                Value = Opc.Ua.ObjectTypeIds.SystemEventType.ToString(),
                                DataType = "NodeId"
                            }
                        }
                    }
                }
            };

            var evtFilter = _serializer.SerializeToString(filter.WhereClause);
            var expFilter = _serializer.SerializeToString(expectedWhere);
            Assert.Equal(expFilter, evtFilter);
        }

        [Fact]
        public async Task AnnexBPart2Example1Async()
        {
            const string query1 = @"
    select PersonType/LastName, AnimalType/Name, ScheduleType/Period
    from PersonType, AnimalType, ScheduleType, HasSchedule, HasPet
    where PersonType relatedTo (
        AnimalType relatedTo (ScheduleType, HasSchedule, 1), HasPet, 1)
";
            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData("PersonType", new [] { "/LastName" }, "LastName"),
                new IdentifierMetaData("HasPet", Array.Empty<string>(), "HasPet"),
                new IdentifierMetaData("AnimalType", new [] { "/Name" }, "Name"),
                new IdentifierMetaData("HasSchedule", Array.Empty<string>(), "HasSchedule"),
                new IdentifierMetaData("ScheduleType", new [] { "/Period" }, "Period")
            };

            var filter = await parser.ParseEventFilterAsync(query1, context, default).ConfigureAwait(false);
            _output.WriteLine(_serializer.SerializeToString(filter, SerializeOption.Indented));

            Assert.NotNull(filter);
            Assert.NotNull(filter.SelectClauses);
            Assert.NotEmpty(filter.SelectClauses);
            Assert.NotNull(filter.WhereClause);

            var expectedWhere = new ContentFilterModel
            {
                Elements = new[]
                {
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.RelatedTo,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                NodeId = "PersonType",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                Index = 1
                            },
                            new FilterOperandModel
                            {
                                NodeId = "HasPet",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                Value = 1
                            }
                        }
                    },
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.RelatedTo,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                NodeId = "AnimalType",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                NodeId = "ScheduleType",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                NodeId = "HasSchedule",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                Value = 1
                            }
                        }
                    }
                }
            };

            var evtFilter = _serializer.SerializeToString(filter.WhereClause);
            var expFilter = _serializer.SerializeToString(expectedWhere);
            Assert.Equal(expFilter, evtFilter);

            const string query2 = @"
    select /LastName, /Name, /Period
    from PersonType, AnimalType, ScheduleType, HasSchedule, HasPet
    where PersonType relatedTo (
        AnimalType relatedTo (ScheduleType, HasSchedule, 1), HasPet, 1)
";
            var filter2 = await parser.ParseEventFilterAsync(query2, context, default).ConfigureAwait(false);
            var filterJson1 = _serializer.SerializeToString(filter);
            var filterJson2 = _serializer.SerializeToString(filter2);
            Assert.Equal(filterJson1, filterJson2);
        }

        [Fact]
        public async Task AnnexBPart2Example2Async()
        {
            const string query = @"
    select /LastName, AnimalType/Name
    from PersonType, HasChild, CatType AnimalType, HasSchedule, FeedingScheduleType
    where
        PersonType relatedTo (PersonType, HasChild, 1)
    or
        CatType relatedTo (FeedingScheduleType, HasSchedule, 1)
";
            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData("PersonType", new [] { "/LastName" }, "LastName"),
                new IdentifierMetaData("HasPet", Array.Empty<string>(), "HasPet"),
                new IdentifierMetaData("AnimalType", new [] { "/Name" }, "Name"),
                new IdentifierMetaData("CatType", new [] { "/Name" }, "Name"),
                new IdentifierMetaData("HasSchedule", Array.Empty<string>(), "HasSchedule"),
                new IdentifierMetaData("FeedingScheduleType",new [] {  "/Period" }, "Period")
            };

            var filter = await parser.ParseEventFilterAsync(query, context, default).ConfigureAwait(false);
            _output.WriteLine(_serializer.SerializeToString(filter, SerializeOption.Indented));

            Assert.NotNull(filter);
            Assert.NotNull(filter.SelectClauses);
            Assert.NotEmpty(filter.SelectClauses);
            Assert.NotNull(filter.WhereClause);

            var expectedWhere = new ContentFilterModel
            {
                Elements = new[]
                {
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.Or,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                Index = 1
                            },
                            new FilterOperandModel
                            {
                                Index = 2
                            }
                        }
                    },
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.RelatedTo,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                NodeId = "CatType",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                NodeId = "FeedingScheduleType",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                NodeId = "HasSchedule",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                Value = 1
                            }
                        }
                    },
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.RelatedTo,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                NodeId = "PersonType",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                NodeId = "PersonType",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                NodeId = "HasChild",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                Value = 1
                            }
                        }
                    }
                }
            };

            var evtFilter = _serializer.SerializeToString(filter.WhereClause);
            var expFilter = _serializer.SerializeToString(expectedWhere);
            Assert.Equal(expFilter, evtFilter);
        }

        [Fact]
        public async Task AnnexBPart2Example3Async()
        {
            // Get PersonType.LastName, AnimalType.Name, ScheduleType.Period
            // where a person has a pet and the animal has a feeding schedule
            // and the person has a Zipcode = ‘02138’
            // and (the Schedule.Period is Daily or Hourly)
            // and Amount to feed is > 10.

            const string query = @"
    select PersonType/LastName, AnimalType/Name, ScheduleType/Period
    from PersonType, AnimalType, ScheduleType, HasSchedule, HasPet, Int32
    where PersonType relatedTo
        (AnimalType relatedTo (ScheduleType, HasSchedule, 1), HasPet, 1)
    and PersonType/Zipcode = '02138'
    and (PersonType<HasPet>AnimalType<HasSchedule>FeedingSchedule/Period = 'Daily'
      or PersonType<HasPet>AnimalType<HasSchedule>FeedingSchedule/Period = 'Hourly')
    and PersonType<HasPet>AnimalType<HasSchedule>FeedingSchedule/Amount > (10 cast(Int32))
";
            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData("PersonType", new [] { "/LastName" }, "LastName"),
                new IdentifierMetaData("PersonType", new [] { "/Zipcode" }, "Zipcode"),
                new IdentifierMetaData("PersonType",
                    new [] { "<HasPet>AnimalType", "<HasSchedule>FeedingSchedule", "/Period" }, "Period"),
                new IdentifierMetaData("PersonType",
                    new [] { "<HasPet>AnimalType", "<HasSchedule>FeedingSchedule", "/Amount" }, "Amount"),
                new IdentifierMetaData("HasPet", Array.Empty<string>(), "HasPet"),
                new IdentifierMetaData("AnimalType", new [] { "/Name" }, "Name"),
                new IdentifierMetaData("Int32", Array.Empty<string>(), "Int32"),
                new IdentifierMetaData("HasSchedule", Array.Empty<string>(), "HasSchedule"),
                new IdentifierMetaData("ScheduleType", new [] { "/Period" }, "Period")
            };

            var filter = await parser.ParseEventFilterAsync(query, context, default).ConfigureAwait(false);
            _output.WriteLine(_serializer.SerializeToString(filter, SerializeOption.Indented));

            Assert.NotNull(filter);
            Assert.NotNull(filter.SelectClauses);
            Assert.NotEmpty(filter.SelectClauses);
            Assert.NotNull(filter.WhereClause);
        }

        [Fact]
        public async Task AnnexBPart2Example4Async()
        {
            // Get PersonType.LastName where a person has a child who has a pet.

            const string query = @"
    select PersonType/LastName
    from PersonType, AnimalType, HasPet, HasChild
    where PersonType relatedTo
        (PersonType relatedTo (AnimalType, HasPet, 1), HasChild, 1)
";
            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData("PersonType", new [] { "/LastName" }, "LastName"),
                new IdentifierMetaData("HasPet", Array.Empty<string>(), "HasPet"),
                new IdentifierMetaData("AnimalType", new [] { "/Name" }, "Name"),
                new IdentifierMetaData("HasChild", Array.Empty<string>(), "HasChild")
            };

            var filter = await parser.ParseEventFilterAsync(query, context, default).ConfigureAwait(false);
            _output.WriteLine(_serializer.SerializeToString(filter, SerializeOption.Indented));

            Assert.NotNull(filter);
            Assert.NotNull(filter.SelectClauses);
            Assert.NotEmpty(filter.SelectClauses);
            Assert.NotNull(filter.WhereClause);

            var expectedWhere = new ContentFilterModel
            {
                Elements = new[]
                {
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.RelatedTo,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                NodeId = "PersonType",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                Index = 1
                            },
                            new FilterOperandModel
                            {
                                NodeId = "HasChild",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                Value = 1
                            }
                        }
                    },
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.RelatedTo,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                NodeId = "PersonType",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                NodeId = "AnimalType",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                NodeId = "HasPet",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                Value = 1
                            }
                        }
                    }
                }
            };

            var evtFilter = _serializer.SerializeToString(filter.WhereClause);
            var expFilter = _serializer.SerializeToString(expectedWhere);
            Assert.Equal(expFilter, evtFilter);
        }

        [Fact]
        public async Task AnnexBPart2Example5Async()
        {
            // Get the last names of children that have the same first name as a parent of theirs

            const string query = @"
    select parent/LastName
    from PersonType parent, PersonType child, HasChild
    where parent relatedTo (child, HasChild, 1)
      and parent/FirstName = child/FirstName
";
            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData("PersonType", new [] { "/LastName" }, "LastName"),
                new IdentifierMetaData("PersonType", new [] { "/FirstName" }, "FirstName"),
                new IdentifierMetaData("HasChild", Array.Empty<string>(), "HasChild")
            };

            var filter = await parser.ParseEventFilterAsync(query, context, default).ConfigureAwait(false);
            _output.WriteLine(_serializer.SerializeToString(filter, SerializeOption.Indented));

            Assert.NotNull(filter);
            Assert.NotNull(filter.SelectClauses);
            Assert.NotEmpty(filter.SelectClauses);
            Assert.NotNull(filter.WhereClause);

            var expectedWhere = new ContentFilterModel
            {
                Elements = new[]
                {
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.And,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                Index = 1
                            },
                            new FilterOperandModel
                            {
                                Index = 2
                            }
                        }
                    },
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.Equals,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                NodeId = "PersonType",
                                BrowsePath = new [] { "/FirstName" },
                                AttributeId = NodeAttribute.Value,
                                Alias = "parent"
                            },
                            new FilterOperandModel
                            {
                                NodeId = "PersonType",
                                BrowsePath = new [] { "/FirstName" },
                                AttributeId = NodeAttribute.Value,
                                Alias = "child"
                            }
                        }
                    },
                    new ContentFilterElementModel {
                        FilterOperator = FilterOperatorType.RelatedTo,
                        FilterOperands = new[]
                        {
                            new FilterOperandModel
                            {
                                NodeId = "PersonType",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId,
                                Alias = "parent"
                            },
                            new FilterOperandModel
                            {
                                NodeId = "PersonType",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId,
                                Alias = "child"
                            },
                            new FilterOperandModel
                            {
                                NodeId = "HasChild",
                                BrowsePath = Array.Empty<string>(),
                                AttributeId = NodeAttribute.NodeId
                            },
                            new FilterOperandModel
                            {
                                Value = 1
                            }
                        }
                    }
                }
            };

            var evtFilter = _serializer.SerializeToString(filter.WhereClause);
            var expFilter = _serializer.SerializeToString(expectedWhere);
            Assert.Equal(expFilter, evtFilter);
        }
    }
}
