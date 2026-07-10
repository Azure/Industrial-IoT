// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Configuration
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Env file configuration
    /// </summary>
    public class DotEnvFileSource : IConfigurationSource
    {
        /// <summary>
        /// Adds .env file environment variables from an .env file that
        /// is in current folder or below up to root.
        /// </summary>
        public DotEnvFileSource()
        {
            _source = new MemoryConfigurationSource();
            try
            {
                // Find .env file
                var curDir = Path.GetFullPath(Environment.CurrentDirectory);
                while (!string.IsNullOrEmpty(curDir) &&
                    !File.Exists(Path.Combine(curDir, ".env")))
                {
                    curDir = Path.GetDirectoryName(curDir);
                }
                if (!string.IsNullOrEmpty(curDir))
                {
                    TryAddToSource(_source, Path.Combine(curDir, ".env"));
                }
            }
            catch (IOException)
            {
            }
        }

        /// <summary>
        /// Create configuration source from file
        /// </summary>
        /// <param name="filePath"></param>
        public DotEnvFileSource(string filePath)
        {
            _source = new MemoryConfigurationSource();
            try
            {
                TryAddToSource(_source, filePath);
            }
            catch (IOException)
            {
            }
        }

        /// <summary>
        /// Adds .env file environment variables
        /// </summary>
        /// <param name="source"></param>
        /// <param name="filePath"></param>
        private static void TryAddToSource(MemoryConfigurationSource source, string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    var lines = File.ReadAllLines(filePath);
                    var values = new Dictionary<string, string?>();
                    foreach (var line in lines)
                    {
                        var offset = line.IndexOf('=', StringComparison.Ordinal);
                        if (offset == -1)
                        {
                            continue;
                        }
                        var key = line[..offset].Trim();
                        if (key.StartsWith('#'))
                        {
                            continue;
                        }
                        key = key.Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
                        values[key] = line[(offset + 1)..]
                            .Replace("\\n", "\n", StringComparison.Ordinal)
                            .Replace("\\r", "\r", StringComparison.Ordinal);
                    }
                    source.InitialData = values;
                }
                catch (IOException) { }
            }
        }

        /// <inheritdoc/>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return _source.Build(builder);
        }

        private readonly MemoryConfigurationSource _source;
    }
}
