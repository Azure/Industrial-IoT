// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Topic templating support
    /// </summary>
    public sealed class TopicBuilder
    {
        /// <summary>
        /// Root topic
        /// </summary>
        public string RootTopic
            => Format(nameof(RootTopic), _options.Value.RootTopicTemplate);

        /// <summary>
        /// Method topic
        /// </summary>
        public string MethodTopic
            => Format(nameof(MethodTopic), _options.Value.MethodTopicTemplate);

        /// <summary>
        /// Events topic
        /// </summary>
        public string EventsTopic
            => Format(nameof(EventsTopic), _options.Value.EventsTopicTemplate);

        /// <summary>
        /// Diagnostics topic
        /// </summary>
        public string DiagnosticsTopic
            => Format(nameof(DiagnosticsTopic), _options.Value.DiagnosticsTopicTemplate);

        /// <summary>
        /// Telemetry topic
        /// </summary>
        public string TelemetryTopic
            => Format(nameof(TelemetryTopic), _options.Value.TelemetryTopicTemplate);

        /// <summary>
        /// Default metadata topic
        /// </summary>
        public string DataSetMetaDataTopic
            => Format(nameof(DataSetMetaDataTopic), _options.Value.DataSetMetaDataTopicTemplate);

        /// <summary>
        /// Create builder
        /// </summary>
        /// <param name="options"></param>
        /// <param name="variables"></param>
        public TopicBuilder(IOptions<PublisherOptions> options,
            IReadOnlyDictionary<string, string>? variables = null)
        {
            _options = options;
            _variables = new Dictionary<string, Func<Formatter, string>>
            {
                { nameof(TelemetryTopic),
                    f => f.Format(_options.Value.TelemetryTopicTemplate) },
                { nameof(RootTopic),
                    f => f.Format(_options.Value.RootTopicTemplate) },
                { nameof(EventsTopic),
                    f => f.Format(_options.Value.EventsTopicTemplate) },

                { nameof(options.Value.Site),
                    _ => options.Value.Site ?? Constants.DefaultSite },
                { nameof(options.Value.PublisherId),
                    _ => options.Value.PublisherId ?? Constants.DefaultPublisherId }
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
        private readonly Dictionary<string, Func<Formatter, string>> _variables;
    }
}
