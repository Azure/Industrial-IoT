// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System.Collections.Generic;
    using Xunit;

    public sealed class DataSetTests
    {
        [Fact]
        public void Constructor_WithDictionary_CopiesEntriesInDictionaryOrder()
        {
            var values = new Dictionary<string, DataValue?>
            {
                ["temperature"] = CreateDataValue(42),
                ["pressure"] = CreateDataValue(100)
            };

            var dataSet = new DataSet(values, DataSetFieldContentFlags.RawData);

            Assert.Equal(DataSetFieldContentFlags.RawData, dataSet.DataSetFieldContentMask);
            Assert.Collection(dataSet.DataSetFields,
                field =>
                {
                    Assert.Equal("temperature", field.Name);
                    Assert.Equal(42, field.Value?.Value);
                },
                field =>
                {
                    Assert.Equal("pressure", field.Name);
                    Assert.Equal(100, field.Value?.Value);
                });
        }

        [Fact]
        public void Constructor_WithList_KeepsListReference()
        {
            IReadOnlyList<(string, DataValue?)> values =
            [
                ("temperature", CreateDataValue(42))
            ];

            var dataSet = new DataSet(values, DataSetFieldContentFlags.StatusCode);

            Assert.Same(values, dataSet.DataSetFields);
            Assert.Equal(DataSetFieldContentFlags.StatusCode, dataSet.DataSetFieldContentMask);
        }

        [Fact]
        public void Constructor_WithSingleField_CreatesSingleEntry()
        {
            var value = CreateDataValue(42);

            var dataSet = new DataSet("temperature", value, DataSetFieldContentFlags.RawData);

            var field = Assert.Single(dataSet.DataSetFields);
            Assert.Equal("temperature", field.Name);
            Assert.Equal(value, field.Value);
        }

        [Fact]
        public void Constructor_WithoutMask_UsesDefaultMask()
        {
            var dataSet = new DataSet();

            Assert.Equal(PubSubMessageDefaults.DefaultDataSetFieldContentFlags,
                dataSet.DataSetFieldContentMask);
            Assert.Empty(dataSet.DataSetFields);
        }

        [Fact]
        public void Equals_NonDataSet_ReturnsFalse()
        {
            var dataSet = new DataSet();

            Assert.NotEqual(new object(), dataSet);
        }

        [Fact]
        public void Equals_SameFieldsAndValues_ReturnsTrue()
        {
            var left = CreateDataSet("temperature", 42);
            var right = CreateDataSet("temperature", 42);

            Assert.Equal(left, right);
        }

        [Fact]
        public void Equals_DifferentFieldName_ReturnsFalse()
        {
            var left = CreateDataSet("temperature", 42);
            var right = CreateDataSet("pressure", 42);

            Assert.NotEqual(left, right);
        }

        [Fact]
        public void Equals_DifferentFieldValue_ReturnsFalse()
        {
            var left = CreateDataSet("temperature", 42);
            var right = CreateDataSet("temperature", 43);

            Assert.NotEqual(left, right);
        }

        [Fact]
        public void Equals_DifferentFieldCount_ReturnsFalse()
        {
            var left = CreateDataSet("temperature", 42);
            var right = left.Add("pressure", CreateDataValue(100));

            Assert.NotEqual(left, right);
        }

        [Fact]
        public void GetHashCode_ReturnsHashValue()
        {
            var dataSet = CreateDataSet("temperature", 42);

            var hash = dataSet.GetHashCode();

            Assert.IsType<int>(hash);
        }

        [Fact]
        public void Remove_ExistingField_ReturnsDataSetWithoutField()
        {
            var dataSet = CreateDataSet("temperature", 42)
                .Add("pressure", CreateDataValue(100));

            var result = dataSet.Remove("temperature");

            var field = Assert.Single(result.DataSetFields);
            Assert.Equal("pressure", field.Name);
            Assert.Equal(dataSet.DataSetFieldContentMask, result.DataSetFieldContentMask);
        }

        [Fact]
        public void Remove_MissingField_ReturnsEquivalentDataSet()
        {
            var dataSet = CreateDataSet("temperature", 42);

            var result = dataSet.Remove("pressure");

            Assert.Equal(dataSet, result);
            Assert.NotSame(dataSet, result);
        }

        [Fact]
        public void Set_ExistingField_ReplacesOnlyThatValue()
        {
            var dataSet = CreateDataSet("temperature", 42)
                .Add("pressure", CreateDataValue(100));

            var result = dataSet.Set("temperature", CreateDataValue(43));

            Assert.Collection(result.DataSetFields,
                field =>
                {
                    Assert.Equal("temperature", field.Name);
                    Assert.Equal(43, field.Value?.Value);
                },
                field =>
                {
                    Assert.Equal("pressure", field.Name);
                    Assert.Equal(100, field.Value?.Value);
                });
        }

        [Fact]
        public void Set_MissingField_LeavesDataSetEquivalent()
        {
            var dataSet = CreateDataSet("temperature", 42);

            var result = dataSet.Set("pressure", CreateDataValue(100));

            Assert.Equal(dataSet, result);
            Assert.NotSame(dataSet, result);
        }

        [Fact]
        public void Add_WithoutAdditionalFlags_AppendsField()
        {
            var dataSet = CreateDataSet("temperature", 42);

            var result = dataSet.Add("pressure", CreateDataValue(100));

            Assert.Equal(2, result.DataSetFields.Count);
            Assert.Equal(dataSet.DataSetFieldContentMask, result.DataSetFieldContentMask);
        }

        [Fact]
        public void Add_WithAdditionalFlags_CombinesMasks()
        {
            var dataSet = new DataSet(DataSetFieldContentFlags.StatusCode);

            var result = dataSet.Add("temperature", CreateDataValue(42),
                DataSetFieldContentFlags.SourceTimestamp);

            Assert.Equal(DataSetFieldContentFlags.StatusCode |
                DataSetFieldContentFlags.SourceTimestamp, result.DataSetFieldContentMask);
        }

        private static DataSet CreateDataSet(string field, object value)
        {
            return new DataSet(field, CreateDataValue(value),
                DataSetFieldContentFlags.RawData);
        }

        private static DataValue CreateDataValue(object value)
        {
            return new DataValue(new Variant(value));
        }
    }
}
