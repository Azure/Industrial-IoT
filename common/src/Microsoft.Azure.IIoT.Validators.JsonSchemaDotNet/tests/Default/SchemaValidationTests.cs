// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Validators.JsonSchemaDotNet.Tests.Default {
    using Microsoft.Azure.IIoT.Validators;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using Xunit;

    /// <summary>
    /// These tests are designed to exercise the generated schema for configuration file
    /// validation. Tests can be additionally found in the schema generator tool located
    /// at: https://github.com/WilliamBerryiii/opcpublisherschemavalidator
    ///
    /// The referenced schema file is a linked asset in the project file set to
    /// copy to the output build directory, so that it can be easily referenced here.
    /// </summary>
    public class SchemaValidationTests {

        // Allow training commas in the test parsers as the main serializer for
        // this solution supports training commas.
        private static JsonDocumentOptions parseOptions = new JsonDocumentOptions {
            AllowTrailingCommas = true,
        };

        [Fact]
        public void EnsureTestConfigurationPassesSchemaValidation() {
            using var schemaReader = new StreamReader("Default/publishednodesschema.json");
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(testConfiguration), schemaReader);
            Assert.True(results.All(r => r.IsValid));
        }

        [Fact]
        public void ByteStringNodeIdsThatAreNotByteStingsReturnErrors() {
            using var schemaReader = new StreamReader("Default/publishednodesschema.json");

            // Break the id of the first Node to ensure an error is thrown
            var alteredConfig = testConfiguration.Replace("i=1001", "b=12345");
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(alteredConfig), schemaReader);

            // Wnsure that we failed the regex NodeID check
            Assert.Equal("The string value was not a match for the indicated regular expression", results.ElementAt(1).Message);
        }

        [Fact]
        public void GuidNodeIdsThatAreNotGuidsReturnErrors() {
            using var schemaReader = new StreamReader("Default/publishednodesschema.json");

            // Break the id of the first Node to ensure an error is thrown
            var alteredConfig = testConfiguration.Replace("i=1001", "g=12345f");
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(alteredConfig), schemaReader);

            // Ensure that we failed the regex NodeID check
            Assert.Equal("The string value was not a match for the indicated regular expression", results.ElementAt(1).Message);
        }

        [Fact]
        public void IntNodeIdsThatAreNotIntsReturnErrors() {
            using var schemaReader = new StreamReader("Default/publishednodesschema.json");

            // Break the id of the first Node to ensure an error is thrown
            var alteredConfig = testConfiguration.Replace("i=1001", "'i=12345f'");
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(alteredConfig), schemaReader);

            // Ensure that we failed the regex NodeID check
            Assert.Equal("The string value was not a match for the indicated regular expression", results.ElementAt(1).Message);
        }

        [Fact]
        public void IncorrectlyFormattedNsuBasedNodeIdsReturnErrors() {
            using var schemaReader = new StreamReader("Default/publishednodesschema.json");

            // Break the id of the first Node to ensure an error is thrown
            var alteredConfig = testConfiguration.Replace("i=1001", "nsu=http://opcfoundation.org/UA/;i=string");
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(alteredConfig), schemaReader);

            // Ensure that we failed the regex NodeID check
            Assert.Equal("The string value was not a match for the indicated regular expression", results.ElementAt(1).Message);
        }

        [Fact]
        public void IncorrectlyFormattedEndpointUrlsReturnErrors() {
            using var schemaReader = new StreamReader("Default/publishednodesschema.json");

            // Break the EndpointUrl by removing the `.` between `opc` and `tcp` which will
            // trigger a validation failure
            var alteredConfig = testConfiguration.Replace("opc.tcp://20.185.195.172:53530/OPCUA/SimulationServer", "opctcp://20.185.195.172:53530/OPCUA/SimulationServer");
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(alteredConfig), schemaReader);

            // Ensure that we failed the regex NodeID check
            Assert.Equal("The string value was not a match for the indicated regular expression", results.ElementAt(1).Message);
        }

        [Fact]
        public void ConfigurationFileWithLargeErrorListIsHandledSuccessfully() {
            using var schemaReader = new StreamReader("Default/publishednodesschema.json");
            var configFileShell = @"
[
  {
    ""EndpointUrl"": ""opc.tcp://20.185.195.172:53530/OPCUA/SimulationServer"",
    ""UseSecurity"": false,
    ""OpcNodes"": [ placeholder ]
  }
]
";
            var longNodeList = new List<string>();

            // Add 10000 nodes into the temp config and make a broken replacement
            // of `Id` with `ID` to trigger schema failures.
            for (int i = 0; i < 10000; i++) {
                longNodeList.Add(@"
      {
        ""Id"": ""i = 1001"",
        ""OpcSamplingInterval"": 2000,
        ""OpcPublishingInterval"": 5000,
      }
".Replace("Id", "ID"));
            }

            var longBadConfig = configFileShell.Replace("placeholder", string.Join(",", longNodeList));
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(longBadConfig), schemaReader);

            // ensure that we only get 4 errors back
            Assert.Equal(4, results.Count);

            // Ensure that we failed on all four primary schema checks correctly
            Assert.Equal("Expected 1 matching subschema but found 0", results.ElementAt(0).Message);
            Assert.Equal("Required properties [Id] were not present", results.ElementAt(1).Message);
            Assert.Equal("Required properties [ExpandedNodeId] were not present", results.ElementAt(2).Message);
            Assert.Equal("Required properties [NodeId] were not present", results.ElementAt(3).Message);

        }

        private string testConfiguration = @"
[
  {
    ""EndpointUrl"": ""opc.tcp://20.185.195.172:53530/OPCUA/SimulationServer"",
    ""UseSecurity"": false,
    ""OpcNodes"": [
      {
        ""Id"": ""i=1001"",
        ""OpcSamplingInterval"": 2000,
        ""OpcPublishingInterval"": 5000,
      },
      {
        ""Id"": ""i=1002"",
        ""OpcSamplingInterval"": 2000,
        ""OpcPublishingInterval"": 5000,
      },
      {
        ""Id"": ""s=this is a string nodeid"",
        ""OpcSamplingInterval"": 2000,
        ""OpcPublishingInterval"": 5000,
      },
      {
        ""Id"": ""s=this is another string nodeid"",
        ""OpcSamplingInterval"": 2000,
        ""OpcPublishingInterval"": 5000,
      },
      {
        ""Id"": ""g=ac85ad0a-1f3f-4ee5-af3d-a7f4f33902d6"",
        ""OpcSamplingInterval"": 2000,
        ""OpcPublishingInterval"": 5000,
      },
      {
        ""Id"": ""g=ac85ad0a-1f3f-4ee5-af3d-a7f4f33902d7"",
        ""OpcSamplingInterval"": 2000,
        ""OpcPublishingInterval"": 5000,
      },
      {
        ""Id"": ""b=dGhpcyBpcyBhIG5vZGUgaWQ="",
        ""OpcSamplingInterval"": 2000,
        ""OpcPublishingInterval"": 5000,
      },
      {
        ""Id"": ""b=dGhpcyBpcyBhbm90aGVyIG5vZGUgaWQ="",
        ""OpcSamplingInterval"": 2000,
        ""OpcPublishingInterval"": 5000,
      },
      {
        ""Id"": ""nsu=http://opcfoundation.org/UA/;i=12345"",
        ""OpcSamplingInterval"": 2000,
        ""OpcPublishingInterval"": 5000,
      },
      {
        ""Id"": ""nsu=http://opcfoundation.org/UA/;i=123456"",
        ""OpcSamplingInterval"": 2000,
        ""OpcPublishingInterval"": 5000,
      },
    ],
  },
]";

    }
}
