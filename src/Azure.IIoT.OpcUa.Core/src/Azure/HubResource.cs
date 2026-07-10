// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.AzureSdk
{
    using System;

    /// <summary>
    /// IoT Hub resource target formatter.
    /// </summary>
    public static class HubResource
    {
        /// <summary>
        /// Format hub/device/module target.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        public static string Format(string? hub, string deviceId, string? moduleId)
        {
            var target = string.IsNullOrEmpty(hub) ? string.Empty : $"{hub}/";
            target += $"devices/{deviceId}";
            if (!string.IsNullOrEmpty(moduleId))
            {
                target += $"/modules/{moduleId}";
            }
            return target;
        }

        /// <summary>
        /// Parse hub/device/module target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="hub"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool Parse(string target, out string? hub, out string deviceId,
            out string? moduleId, out string? error)
        {
            hub = null;
            deviceId = string.Empty;
            moduleId = null;
            error = null;
            var parts = target.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var index = 0;
            if (parts.Length > 0 && parts[0] != "devices")
            {
                hub = parts[index++];
            }
            if (parts.Length - index < 2 || parts[index] != "devices")
            {
                error = "Target must contain devices/{deviceId}.";
                return false;
            }
            deviceId = parts[index + 1];
            index += 2;
            if (parts.Length > index)
            {
                if (parts.Length - index < 2 || parts[index] != "modules")
                {
                    error = "Target module segment must be modules/{moduleId}.";
                    return false;
                }
                moduleId = parts[index + 1];
            }
            return true;
        }
    }
}
