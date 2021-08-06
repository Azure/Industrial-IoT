namespace Microsoft.Azure.IIoT.Validators {
    using Json.Schema;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    
    /// <summary>
    /// JsonSchema.Net schema validator.
    /// </summary>
    public class JsonSchemaDotNetSchemaValidator : IJsonSchemaValidator {

        /// <inheritdoc/>
        public IList<JsonSchemaValidationResult> Validate(byte[] jsonBuffer, TextReader schemaReader) {
            if (jsonBuffer is null) {
                throw new ArgumentNullException(nameof(jsonBuffer));
            }

            if (schemaReader is null) {
                throw new ArgumentNullException(nameof(schemaReader));
            }

            using (var validationDataStream = new MemoryStream(jsonBuffer)) {
                using (var validationReader = new StreamReader(validationDataStream)) {

                    var schemaString = schemaReader.ReadToEnd();
                    var schema = JsonSchema.FromText(schemaString);

                    // Use "Basic" output as that will only generate 
                    // two levels of validation results (Root and one level down)
                    // see the API documentation for Json Everything for further details
                    // https://gregsdennis.github.io/json-everything/api/Json.Schema.OutputFormat.html
                    var validationOptions = new ValidationOptions() {
                        OutputFormat = OutputFormat.Basic,
                    };

                    // Run validation ensuring that trailing commas are supported
                    // as it appears trailing commas have been allowable in the 
                    // configuration files for some time. 
                    var validationResults =  schema.Validate(
                                                JsonDocument.Parse(
                                                validationDataStream.ReadAsString(Encoding.UTF8),
                                                new JsonDocumentOptions() { AllowTrailingCommas = true, }
                                                ).RootElement,
                                                validationOptions);

                    var jsonSchemaValidationResultCollection = new List<JsonSchemaValidationResult>();

                    // Copy and flatten validation result to generic json validation result objects
                    // as we don't want to bleed JsonSchema.Net lib objects outside of this class.

                    // Add the top level error message before iterating the nested errors to add to the collection. 
                    jsonSchemaValidationResultCollection.Add(
                        new JsonSchemaValidationResult(
                            validationResults.IsValid,
                            validationResults.Message,
                            validationResults.SchemaLocation.ToString(),
                            validationResults.InstanceLocation.ToString()));

                    // Iterate the nested errors and push them into the error collection
                    jsonSchemaValidationResultCollection.AddRange(
                        validationResults.NestedResults.Select(nr =>
                            new JsonSchemaValidationResult(
                                nr.IsValid, 
                                nr.Message, 
                                nr.SchemaLocation.ToString(), 
                                nr.InstanceLocation.ToString())
                    ));

                    return jsonSchemaValidationResultCollection;
                }
            }
        }
    }
}