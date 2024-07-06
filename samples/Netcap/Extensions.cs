// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;
using Microsoft.Azure.Devices.Shared;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;

internal static partial class Extensions
{
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? GetProperty(this Twin twin, string name, string? defaultValue = null)
    {
        if (!twin.Properties.Desired.Contains(name))
        {
            return defaultValue;
        }
        var value = twin.Properties.Desired[name];
        var result = (string?)value?.ToString();
        if (string.IsNullOrEmpty(result))
        {
            return defaultValue;
        }
        return result;
    }

    public static bool TryGetBytes(this JsonElement elem, [NotNullWhen(true)] out byte[]? value)
    {
        if (elem.ValueKind == JsonValueKind.Array)
        {
            value = elem.EnumerateArray().Select(d => d.GetByte()).ToArray();
            return true;
        }
        if (elem.ValueKind == JsonValueKind.String)
        {
            return elem.TryGetBytesFromBase64(out value);
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Replace invalid chars in container name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string? FixContainerName(string name)
    {
        // Remove any invalid characters
        var containerName = InvalidCharMatch().Replace(name, "");
        containerName = containerName.Trim('-');

        // Check length
        if (containerName.Length < 3)
        {
            containerName = containerName.PadRight(3 - containerName.Length, 'x');
        }
        else if (containerName.Length > 63)
        {
            containerName = containerName.Substring(0, 63);
        }
        return containerName.ToLowerInvariant();
    }

    [GeneratedRegex("[^a-zA-Z0-9-]")]
    private static partial Regex InvalidCharMatch();
}
