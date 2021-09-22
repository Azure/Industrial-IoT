// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Validation {

    public class ValidationUtils {

        /// <summary>
        /// Checks if the default value should be used. The default value
        /// should be used when the user did not input any value
        /// </summary>
        /// <param name="value">User input</param>
        /// <returns>True if the input is empty, false otherwise</returns>
        public bool ShouldUseDefaultValue(string value) {
            // User did not input the value, so the default value will be used
            return string.IsNullOrWhiteSpace(value);
        }
    }
}
