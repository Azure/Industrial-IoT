// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License. See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Serialization
{
    using System.Runtime.Serialization;
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
