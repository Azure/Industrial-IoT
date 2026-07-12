// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License. See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Serialization
{
    using System.Runtime.Serialization;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Xunit;

    /// <summary>
    /// Tests for shared JSON serializer options.
    /// </summary>
    public sealed class JsonTests
    {
        [Fact]
        public void ApplyToPreservesSourceGeneratedResolverAndDataContractWireFormat()
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonTestsContext.Default
            };

            Json.ApplyTo(options);

            Assert.Same(JsonTestsContext.Default,
                options.GetTypeInfo(typeof(DataContractModel)).OriginatingResolver);

            var model = new DataContractModel
            {
                Value = "value",
                OmitWhenDefault = 0,
                NotADataMember = "excluded"
            };
            var json = JsonSerializer.Serialize(model, options);

            Assert.Equal("""{"ExactName":"value"}""", json);
            var result = JsonSerializer.Deserialize<DataContractModel>(
                """{"ExactName":"deserialized"}""", options);
            Assert.NotNull(result);
            Assert.Equal("deserialized", result.Value);
            Assert.Equal(0, result.OmitWhenDefault);
            Assert.Null(result.NotADataMember);
        }

        [Fact]
        public void ClosedConvertersPreserveSetAndMatrixWireFormats()
        {
            IReadOnlySet<string> values = new HashSet<string> { "one", "two" };
            var matrix = new[,] { { 1, 2 }, { 3, 4 } };

            Assert.Equal("""["one","two"]""", JsonSerializer.Serialize(values, Json.Options));
            Assert.Equivalent(values, JsonSerializer.Deserialize<IReadOnlySet<string>>(
                """["one","two"]""", Json.Options));
            Assert.Equal("""[[1,2],[3,4]]""", JsonSerializer.Serialize(matrix, Json.Options));
            Assert.Equal(matrix.Cast<int>(), JsonSerializer.Deserialize<int[,]>(
                """[[1,2],[3,4]]""", Json.Options)!.Cast<int>());
        }

    }

    [DataContract]
    internal sealed class DataContractModel
    {
        [DataMember(Name = "ExactName")]
        public string Value { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int OmitWhenDefault { get; set; }

        public string NotADataMember { get; set; }
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(DataContractModel))]
    internal sealed partial class JsonTestsContext : JsonSerializerContext
    {
    }
}
