// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Validators.JsonSchemaDotNet.Tests {
    using Microsoft.Azure.IIoT.Validators;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Xunit;

    /// <summary>
    /// These tests are designed to exercise the JSON Schema
    /// validation functions.
    /// </summary>
    public class JsonSchemaDotNetValidatorTests {

        [Fact]
        public void EnsureSchemaTrailingCommaSupportInBuffers() {
            var schema = @"
{
    ""type"": ""object"",
    ""properties"": {
                ""prop"": { ""type"": ""string"" }
    }
}
";

            // The following object includes a trailing comma
            var pn = @" { ""prop"": ""this is a string"", } ";

            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(pn), new StringReader(schema));
            Assert.True(results.All(r => r.IsValid));

        }

        [Fact]
        public void ValidateTrivialStringTypeSchema() {
            var schema = @"
{
    ""type"": ""object"",
    ""properties"": {
                ""prop"": { ""type"": ""string"" }
    }
}
";
            var pn = @"{""prop"": ""this is a string""}";

            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(pn), new StringReader(schema));
            Assert.True(results.All(r => r.IsValid));
        }

        [Fact]
        public void EnsureNumericDataFailsOnStringTypeValidation() {
            var schema = @"
{
    ""type"": ""object"",
    ""properties"": {
                ""prop"": { ""type"": ""string"" }
    }
}
";

            var pn = @"{""prop"": 1000}";

            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(pn), new StringReader(schema));
            Assert.True(results.All(r => !r.IsValid));
            Assert.Equal("Value is \"integer\" but should be \"string\"", results.First().Message);
        }

        [Fact]
        public void EnsureMultipleValidationErrorsAreFlattened() {
            var schema = @"
{
    ""type"": ""object"",
    ""properties"": {
                ""prop"": { ""type"": ""string"" },
                ""prop2"": { ""type"": ""integer"" }
    }
}
";

            var pn = @" { ""prop"": 1000, ""prop2"":""this is a string"" } ";

            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(pn), new StringReader(schema));
            Assert.Equal(3, results.Count);
            Assert.True(results.All(r => !r.IsValid));

            // Ensure that the return error messages are what we expect from the
            // schema validation library
            Assert.Equal(1, results.Where(r =>
                !r.IsValid &&
                r.InstanceLocation == "#" &&
                r.SchemaLocation == "#/properties" &&
                r.Message == null).Take(3).Count());
            Assert.Equal(1, results.Where(r =>
                !r.IsValid &&
                r.InstanceLocation == "#/prop" &&
                r.SchemaLocation == "#/properties/prop/type" &&
                r.Message == "Value is \"integer\" but should be \"string\"").Take(3).Count());
            Assert.Equal(1, results.Where(r =>
                !r.IsValid &&
                r.InstanceLocation == "#/prop2" &&
                r.SchemaLocation == "#/properties/prop2/type" &&
                r.Message == "Value is \"string\" but should be \"integer\"").Take(3).Count());
        }
    }
}
