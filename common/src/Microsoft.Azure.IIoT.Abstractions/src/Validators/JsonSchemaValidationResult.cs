// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Validators {
    /// <summary>
    /// Json schema validation result.
    /// </summary>
    public class JsonSchemaValidationResult {

        /// <summary>
        /// Instantiates json schema validation result.
        /// </summary>
        /// <param name="isValid"></param>
        /// <param name="message"></param>
        /// <param name="schemaLocation"></param>
        /// <param name="instanceLocation"></param>
        public JsonSchemaValidationResult(
            bool isValid,
            string message = null,
            string schemaLocation = null,
            string instanceLocation = null) {
            IsValid = isValid;
            Message = message;
            SchemaLocation = schemaLocation;
            InstanceLocation = instanceLocation;
        }

        /// <summary>
        /// Indicates whether the validation passed or failed.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// The error message, if any.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The schema location that generated this node.
        /// </summary>
        public string SchemaLocation { get; }

        /// <summary>
        /// The instance location that was processed.
        /// </summary>
        public string InstanceLocation { get; }
    }
}
