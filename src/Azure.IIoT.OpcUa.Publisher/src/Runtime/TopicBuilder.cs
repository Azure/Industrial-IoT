﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Topic template support
    /// </summary>
    public sealed class TopicBuilder
    {
        /// <summary>
        /// Root topic
        /// </summary>
        public string RootTopic
            => Format(nameof(RootTopic), _templates.Root
                ?? _options.Value.TopicTemplates.Root);

        /// <summary>
        /// Method topic
        /// </summary>
        public string MethodTopic
            => Format(nameof(MethodTopic), _templates.Method
                ?? _options.Value.TopicTemplates.Method);

        /// <summary>
        /// Events topic
        /// </summary>
        public string EventsTopic
            => Format(nameof(EventsTopic), _templates.Events
                ?? _options.Value.TopicTemplates.Events);

        /// <summary>
        /// Diagnostics topic
        /// </summary>
        public string DiagnosticsTopic
            => Format(nameof(DiagnosticsTopic), _templates.Diagnostics
                ?? _options.Value.TopicTemplates.Diagnostics);

        /// <summary>
        /// Telemetry topic
        /// </summary>
        public string TelemetryTopic
            => Format(nameof(TelemetryTopic), _templates.Telemetry
                ?? _options.Value.TopicTemplates.Telemetry);

        /// <summary>
        /// Default metadata topic
        /// </summary>
        public string DataSetMetaDataTopic
            => Format(nameof(DataSetMetaDataTopic), _templates.DataSetMetaData
                ?? _options.Value.TopicTemplates.DataSetMetaData);

        /// <summary>
        /// Create builder
        /// </summary>
        /// <param name="options"></param>
        /// <param name="encoding"></param>
        /// <param name="templates"></param>
        /// <param name="variables"></param>
        /// <param name="dataSetWriterModel"></param>
        public TopicBuilder(IOptions<PublisherOptions> options,
            MessageEncoding? encoding = null, TopicTemplatesOptions? templates = null,
            IReadOnlyDictionary<string, string>? variables = null)
        {
            _options = options;
            _templates = templates ?? options.Value.TopicTemplates;

            _variables = new Dictionary<string, Func<Formatter, string>>
            {
                { nameof(TelemetryTopic),
                    f => f.Format(_templates.Telemetry
                        ?? _options.Value.TopicTemplates.Telemetry) },
                { nameof(RootTopic),
                    f => f.Format(_templates.Root
                        ?? _options.Value.TopicTemplates.Root) },
                { nameof(EventsTopic),
                    f => f.Format(_templates.Events
                        ?? _options.Value.TopicTemplates.Events) },
                { "Encoding",
                    _ => (encoding
                        ?? MessageEncoding.Json).ToString() },
                { nameof(options.Value.SiteId),
                    _ => options.Value.SiteId
                        ?? Constants.DefaultSiteId },
                { nameof(options.Value.PublisherId),
                    _ => options.Value.PublisherId
                        ?? options.Value.SiteId
                        ?? Constants.DefaultPublisherId }
            };
            if (variables != null)
            {
                foreach (var kv in variables)
                {
                    _variables.Add(kv.Key, _ => kv.Value);
                }
            }
        }

        /// <summary>
        /// Format template
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        internal string Format(string topicName, string? template)
        {
            return new Formatter(topicName, _variables).Format(template);
        }

        private sealed class Formatter
        {
            /// <summary>
            /// Unused variables
            /// </summary>
            public Dictionary<string, Func<Formatter, string>> Variables { get; }

            public Formatter(string topicName,
                Dictionary<string, Func<Formatter, string>> variables)
            {
                Variables = new Dictionary<string, Func<Formatter, string>>(variables,
                    StringComparer.OrdinalIgnoreCase);
                // Remove topic name from formatter resolver to not recurse into itself
                Variables.Remove(topicName);
            }

            /// <summary>
            /// Format topic
            /// </summary>
            /// <param name="template"></param>
            /// <returns></returns>
            public string Format(string? template)
            {
                if (template == null)
                {
                    return string.Empty;
                }
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
                return Regex.Replace(template, "{([^}]+)}", m =>
                {
                    if (Variables.TryGetValue(m.Groups[1].Value, out var v))
                    {
                        Variables.Remove(m.Groups[1].Value);
                        return v.Invoke(this);
                    }
                    return m.Groups[1].Value;
                });
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
            }
        }

        private readonly IOptions<PublisherOptions> _options;
        private readonly TopicTemplatesOptions _templates;
        private readonly Dictionary<string, Func<Formatter, string>> _variables;
    }
}
