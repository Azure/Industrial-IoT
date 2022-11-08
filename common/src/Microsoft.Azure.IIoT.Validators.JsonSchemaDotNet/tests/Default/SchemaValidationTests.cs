// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Validators.JsonSchemaDotNet.Tests.Default {
    using Json.Schema;
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

        private bool DefaultSchemaIncludesIdValuePatterns;
        private static string PublishedNodesSchema = "Default/publishednodesschema.json";

        public SchemaValidationTests() {

            // Check each subschema to see if any of them include a pattern check
            // for their Id values we'll assume that if Id validation is enabled for 
            // one, it's likely enabled for all. Remove this constructor
            // and associated test if checks on `DefaultSchemaIncludesIdValuePatterns`
            // if value validation in the schema is ever enabled by default
            using var schemaReader = new StreamReader(PublishedNodesSchema);
            var schema = JsonDocument.Parse(schemaReader.ReadToEnd());
            var temp = new JsonElement();
            var schemaIdElement = 
                schema.RootElement.GetProperty("items").GetProperty("oneOf")[0]
                .GetProperty("properties").GetProperty("OpcNodes")
                .GetProperty("items").GetProperty("properties").GetProperty("Id");
            var schemaExpandedNodeIdElement =
                schema.RootElement.GetProperty("items").GetProperty("oneOf")[1]
                .GetProperty("properties").GetProperty("OpcNodes")
                .GetProperty("items").GetProperty("properties").GetProperty("ExpandedNodeId");
            var schemaNodeIdElement =
                schema.RootElement.GetProperty("items").GetProperty("oneOf")[2]
                .GetProperty("properties").GetProperty("NodeId")
                .GetProperty("properties").GetProperty("Identifier");
            if (schemaIdElement.TryGetProperty("pattern", out temp) 
                || schemaIdElement.TryGetProperty("pattern", out temp)
                || schemaNodeIdElement.TryGetProperty("pattern", out temp)) {
                DefaultSchemaIncludesIdValuePatterns = true;
            }
        }

        [Fact]
        public void EnsureTestConfigurationWithBOMHeaderPassesSchemaValidation() {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, new UTF8Encoding(true)); // Write with BOM header.
            writer.Write(testConfiguration);
            writer.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            var testConfigurationBytes = ms.ReadAsBuffer().ToArray();

            using var schemaReader = new StreamReader("Default/publishednodesschema.json");
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(testConfigurationBytes, schemaReader);
            Assert.True(results.All(r => r.IsValid));
        }

        [Fact]
        public void EnsureTestConfigurationPassesSchemaValidation() {
            using var schemaReader = new StreamReader(PublishedNodesSchema);
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(testConfiguration), schemaReader);
            Assert.True(results.All(r => r.IsValid));
        }

        [Fact]
        public void ByteStringNodeIdsThatAreNotByteStingsReturnErrors() {
            using var schemaReader = new StreamReader(PublishedNodesSchema);

            // Break the id of the first Node to ensure an error is thrown
            var alteredConfig = testConfiguration.Replace("i=1001", "b=12345");
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(alteredConfig), schemaReader);

            // if the default schema includes ID Value Checks
            if (DefaultSchemaIncludesIdValuePatterns) {

                // Ensure that we failed the regex NodeID check
                Assert.Equal("The string value was not a match for the indicated regular expression", results.ElementAt(1).Message);
            }
        }

        [Fact]
        public void GuidNodeIdsThatAreNotGuidsReturnErrors() {
            using var schemaReader = new StreamReader(PublishedNodesSchema);

            // Break the id of the first Node to ensure an error is thrown
            var alteredConfig = testConfiguration.Replace("i=1001", "g=12345f");
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(alteredConfig), schemaReader);

            // if the default schema includes ID Value Checks
            if (DefaultSchemaIncludesIdValuePatterns) {

                // Ensure that we failed the regex NodeID check
                Assert.Equal("The string value was not a match for the indicated regular expression", results.ElementAt(1).Message);
            }
        }

        [Fact]
        public void IntNodeIdsThatAreNotIntsReturnErrors() {
            using var schemaReader = new StreamReader(PublishedNodesSchema);

            // Break the id of the first Node to ensure an error is thrown
            var alteredConfig = testConfiguration.Replace("i=1001", "'i=12345f'");
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(alteredConfig), schemaReader);

            // if the default schema includes ID Value Checks
            if (DefaultSchemaIncludesIdValuePatterns) {

                // Ensure that we failed the regex NodeID check
                Assert.Equal("The string value was not a match for the indicated regular expression", results.ElementAt(1).Message);
            }
        }

        [Fact]
        // There are nuances to the use of "items" schema for arrays in JSON Schema.
        // Particularly the difference between "items: [{ ... }]" and "items: { ... }"
        // The first checks ONLY the schema of the array element in a given array index position,
        // whereas just object notation for "items" (e.g. '{}') will ensure that all elements are
        // checked against a given schema.
        // See: https://datatracker.ietf.org/doc/html/draft-handrews-json-schema-validation-01#section-6.4.1
        public void MultipleIntNodeIdsThatAreNotIntsReturnExpectedErrors() {
            using var schemaReader = new StreamReader(PublishedNodesSchema);

            // Break the id of the second Node to ensure an error is thrown
            var alteredConfig = testConfiguration.Replace("i=1002", "'i=12345f'");
            //Break the id of the second to last Node (#8) to ensure an error is thrown
            alteredConfig = alteredConfig.Replace("i=12345", "'i=12345g'");
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(alteredConfig), schemaReader);

            // if the default schema includes ID Value Checks
            if (DefaultSchemaIncludesIdValuePatterns) {
                Assert.NotEmpty(results);
                // Ensure that we failed the regex Id check on the 2nd node
                Assert.Equal("The string value was not a match for the indicated regular expression", results.ElementAt(1).Message);
                // Ensure that we failed the regex ExpandedNodeId check on the 9th node.
                Assert.Equal("Required properties [ExpandedNodeId] were not present", results.ElementAt(8).Message);
            }
        }

        [Fact]
        public void IncorrectlyFormattedNsuBasedNodeIdsReturnErrors() {
            using var schemaReader = new StreamReader(PublishedNodesSchema);

            // Break the id of the first Node to ensure an error is thrown
            var alteredConfig = testConfiguration.Replace("i=1001", "nsu=http://opcfoundation.org/UA/;i=string");
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(alteredConfig), schemaReader);

            // if the default schema includes ID Value Checks
            if (DefaultSchemaIncludesIdValuePatterns) {

                // Ensure that we failed the regex NodeID check
                Assert.Equal("The string value was not a match for the indicated regular expression", results.ElementAt(1).Message);
            }
        }

        [Fact]
        public void ValidateHistoricalMixedModeSchema() {
            var configFileShell = @"
[
    {
        ""EndpointUrl"": ""opc.tcp://crpogace01:49320"",
        ""UseSecurity"": false,
        ""OpcNodes"": [
            {

                ""DisplayName"": ""Kep_1_DisplayName"",
		        ""DataSetFieldId"": ""Kep_1_DataSetFieldId"",
		        ""Id"": ""nsu=KEPServerEX;s=Sim.CH1.SIM_CH1_TAG1\\234754a-c63-b9601"",
		        ""OpcSamplingInterval"": 1000,
		        ""OpcPublishingInterval"": 1000
            },
	        {
                ""DisplayName"": ""Kep_2_DisplayName"",
		        ""DataSetFieldId"": ""Kep_2_DataSetFieldId"",
		        ""Id"": ""ns=2;s=Sim.CH1.SIM_CH1_TAG10\\2347798-c63-19401"",
		        ""ExpandedNodeId"":""nsu=KEPServerEX;s=Sim.CH1.SIM_CH1_TAG10\\2347798-c63-19401"",
		        ""OpcSamplingInterval"": 1000,
		        ""OpcPublishingInterval"": 1000
            }
        ]
	},
	{
	    ""EndpointUrl"": ""opc.tcp://crpogace01:51210/UA/DemoServer"",
	    ""UseSecurity"": false,
	    ""OpcNodes"": [
			{
				""DisplayName"": ""Softing_1_DisplayName"",
				""DataSetFieldId"": ""Softing_1_DataSetFieldId"",
				""Id"": ""http://test.org/UA/Data/#i=10847"",
				""OpcSamplingInterval"": 1000,
				""OpcPublishingInterval"": 1000

            },
			{
				""DisplayName"": ""Softing_2_DisplayName"",
				""DataSetFieldId"": ""Softing_2_DataSetFieldId"",
				""Id"": ""nsu=http://test.org/UA/Data/;i=10848"",
				""OpcSamplingInterval"": 1000,
				""OpcPublishingInterval"": 1000
			},
			{
                ""DisplayName"": ""Softing_3_DisplayName"",
				""DataSetFieldId"": ""Softing_3_DataSetFieldId"",
				""Id"": ""ns=3;i=10849"",
				""ExpandedNodeId"": ""http://test.org/UA/Data/#i=10849"",
				""OpcSamplingInterval"": 1000,
				""OpcPublishingInterval"": 1000
            }
		]
	}
]";
            using var schemaReader = new StreamReader(PublishedNodesSchema);

            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(configFileShell), schemaReader);

            if (!DefaultSchemaIncludesIdValuePatterns) {
                Assert.True(results.First().IsValid);
            } else {
                Assert.Equal(12, results.Count());
                // Ensure that we failed the regex Id check on the 2nd node
                Assert.Equal("Expected 1 matching subschema but found 0", results.ElementAt(1).Message);
                // Ensure that we failed the regex ExpandedNodeId check on the 9th node.
                Assert.Equal("The string value was not a match for the indicated regular expression", results.ElementAt(2).Message);
            }

        }

        [Fact]
        public void IncorrectlyFormattedEndpointUrlsReturnErrors() {
            using var schemaReader = new StreamReader(PublishedNodesSchema);

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
            using var schemaReader = new StreamReader(PublishedNodesSchema);
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

            // if the default schema includes ID Value Checks
            if (DefaultSchemaIncludesIdValuePatterns) {

                // Ensure that we get all 20002 errors back.
                Assert.Equal(20002, results.Count);

                // Ensure that we failed on schema checks correctly.
                Assert.Equal(1, results.Count(r => r.Message.Equals("Expected 1 matching subschema but found 0")));
                Assert.Equal(1, results.Count(r => r.Message.Equals("Required properties [NodeId] were not present")));
                Assert.Equal(10000, results.Count(r => r.Message.Equals("Required properties [Id] were not present")));
                Assert.Equal(10000, results.Count(r => r.Message.Equals("Required properties [ExpandedNodeId] were not present")));
            }
        }

        [Fact]
        public void ConfigurationFileWithCommendIsAllowed() {
            using var schemaReader = new StreamReader(PublishedNodesSchema);
            var configFileShell = @"
// This header commend does not throws validation error
[
  {
    /* This endpoint is simulated */
    ""EndpointUrl"": ""opc.tcp://20.185.195.172:53530/OPCUA/SimulationServer"",
    // No Security 😉
    ""UseSecurity"": false,
    ""OpcNodes"": [
      {
        // First Node
        ""Id"": ""i=1001"",
        ""OpcSamplingInterval"": 2000,
        ""OpcPublishingInterval"": 5000,
      }
    ]
  }
]
";
            var validator = new JsonSchemaDotNetSchemaValidator();
            var results = validator.Validate(Encoding.UTF8.GetBytes(configFileShell), schemaReader);

            Assert.True(results.First().IsValid);
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
