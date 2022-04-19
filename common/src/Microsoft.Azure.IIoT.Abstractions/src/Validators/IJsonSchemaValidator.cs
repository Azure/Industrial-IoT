// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Validators {
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Json schema validator
    /// </summary>
    public interface IJsonSchemaValidator {
        /// <summary>
        /// Validates Json against the provided Json schema.
        /// </summary>
        /// <param name="jsonBuffer"></param>
        /// <param name="schemaReader"></param>
        /// <returns></returns>
        public IList<JsonSchemaValidationResult> Validate(byte[] jsonBuffer, TextReader schemaReader);
    }
}
