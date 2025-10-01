// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    /// Always validates true
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class SkipValidationAttribute : ValidationAttribute
    {
        /// <inheritdoc/>
        public override bool IsValid(object? value)
        {
            return true;
        }
    }
}
