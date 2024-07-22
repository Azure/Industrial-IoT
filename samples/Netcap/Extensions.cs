﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using Microsoft.Azure.Devices.Shared;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Various extensions
/// </summary>
internal static partial class Extensions
{
    /// <summary>
    /// Get property
    /// </summary>
    /// <param name="twin"></param>
    /// <param name="name"></param>
    /// <param name="defaultValue"></param>
    /// <param name="desired"></param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? GetProperty(this Twin twin, string name,
        string? defaultValue = null, bool desired = true)
    {
        var bag = desired ? twin.Properties.Desired : twin.Properties.Reported;
        if (!bag.Contains(name))
        {
            return defaultValue;
        }
        var value = bag[name];
        var result = (string?)value?.ToString();
        if (string.IsNullOrEmpty(result))
        {
            return defaultValue;
        }
        return result;
    }

    /// <summary>
    /// Get tag
    /// </summary>
    /// <param name="twin"></param>
    /// <param name="name"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? GetTag(this Twin twin, string name,
        string? defaultValue = null)
    {
        if (!twin.Tags.Contains(name))
        {
            return defaultValue;
        }
        var value = twin.Tags[name];
        var result = (string?)value?.ToString();
        if (string.IsNullOrEmpty(result))
        {
            return defaultValue;
        }
        return result;
    }

    /// <summary>
    /// Get bytes
    /// </summary>
    /// <param name="elem"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool TryGetBytes(this JsonElement elem,
        [NotNullWhen(true)] out byte[]? value)
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
    /// Fix a unique name for a resource
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string FixUpResourceName(string name)
    {
        name = AlphaNumOnly().Replace(name, "");
        if (name.Length > 24)
        {
            name = name.Substring(0, 24);
        }
        return name;
    }

    /// <summary>
    /// Get assembly version
    /// </summary>
    /// <param name="assembly"></param>
    public static string GetVersion(this Assembly assembly)
    {
        var ver = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        if (ver == null || !Version.TryParse(ver, out var assemblyVersion))
        {
            assemblyVersion = new Version();
        }
        return assemblyVersion.ToString();
    }

    /// <summary>
    /// Replace invalid chars in a storage entity name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string FixUpStorageName(string name)
    {
        // Remove any invalid characters
        var containerName = AlphaNumAndDashOnly().Replace(name, "");
        containerName = containerName.Trim('-');

        // Check length
        if (containerName.Length < 3)
        {
            containerName = containerName.PadRight(3, 'x');
        }
        else if (containerName.Length > 63)
        {
            containerName = containerName.Substring(0, 63);
        }
#pragma warning disable CA1308 // Normalize strings to uppercase
        return containerName.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
    }

    /// <summary>
    /// Returns true if running in container.
    /// </summary>
    /// <returns></returns>
    public static bool IsRunningInContainer()
    {
        return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != null;
    }

    [GeneratedRegex("[^a-zA-Z0-9-]")]
    private static partial Regex AlphaNumAndDashOnly();
    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex AlphaNumOnly();
}
