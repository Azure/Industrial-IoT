// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using System;
    using System.IO;

    /// <summary>
    /// Builds absolute paths that are actually absolute on the host operating
    /// system.
    /// </summary>
    /// <remarks>
    /// The configuration tests exercise how the publisher composes and
    /// preserves paths, not Windows path syntax. Written with a literal
    /// <c>C:\publisher\...</c> they only pass on Windows: on Linux that string
    /// is not rooted, so <see cref="Path.IsPathRooted(string)"/> is false and
    /// the production code takes its relative-path branch instead, producing a
    /// path under the working directory and failing the assertion. Building the
    /// fixture from the platform root keeps the test about the behaviour it
    /// means to cover on every operating system CI runs.
    /// </remarks>
    internal static class TestPaths
    {
        /// <summary>
        /// Combine the segments onto the platform's root, yielding
        /// <c>C:\a\b</c> on Windows and <c>/a/b</c> elsewhere.
        /// </summary>
        public static string Rooted(params string[] segments)
        {
            return Path.Combine(OperatingSystem.IsWindows() ? @"C:\" : "/",
                Path.Combine(segments));
        }
    }
}
