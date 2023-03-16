// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Models
{
    /// <summary>
    /// Diagnostics model extensions
    /// </summary>
    public static class DiagnosticsModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiagnosticsModel Clone(this DiagnosticsModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new DiagnosticsModel
            {
                AuditId = model.AuditId,
                Level = model.Level,
                TimeStamp = model.TimeStamp
            };
        }
    }
}
