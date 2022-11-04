// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

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

            var schema = JsonSchema.FromText(schemaReader.ReadToEnd());

            var jsonString = Encoding.UTF8.GetString(jsonBuffer);

            // Remove BOM characters from the string if present.
            var jsonStringWithoutBom = jsonString.Trim(new char[] { '\uFEFF', '\u200B' });

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
            // Allow but Skip Commends
            var validationResults =  schema.Validate(
                                        JsonDocument.Parse(
                                        jsonStringWithoutBom,
                                        new JsonDocumentOptions() 
                                        { 
                                            AllowTrailingCommas = true, 
                                            CommentHandling = JsonCommentHandling.Skip 
                                        }).RootElement,
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