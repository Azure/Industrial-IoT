// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Text.Json;

    /// <summary>
    /// Values accepted by a command-line option.
    /// </summary>
    internal enum CommandLineOptionValueType
    {
        None,
        Optional,
        Required
    }

    /// <summary>
    /// A deterministic command-line option descriptor.
    /// </summary>
    internal sealed class CommandLineOptionDescriptor
    {
        /// <summary>
        /// Creates a descriptor.
        /// </summary>
        /// <param name="prototype">The aliases and value requirement.</param>
        /// <param name="description">The help text.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="hidden">Whether the option is hidden from help.</param>
        public CommandLineOptionDescriptor(string prototype, string description,
            Action<string?> action, bool hidden)
        {
            Prototype = prototype;
            Description = description;
            Action = action;
            Hidden = hidden;

            var names = prototype.Split('|');
            var optionValueType = CommandLineOptionValueType.None;
            foreach (var name in names)
            {
                if (name.EndsWith('='))
                {
                    optionValueType = CommandLineOptionValueType.Required;
                }
                else if (name.EndsWith(':') &&
                    optionValueType != CommandLineOptionValueType.Required)
                {
                    optionValueType = CommandLineOptionValueType.Optional;
                }
            }
            OptionValueType = optionValueType;
            Names = names.Select(static name => name.TrimEnd('=', ':')).ToArray();
        }

        /// <summary>
        /// Option prototype.
        /// </summary>
        public string Prototype { get; }

        /// <summary>
        /// Option aliases.
        /// </summary>
        public IReadOnlyList<string> Names { get; }

        /// <summary>
        /// Option help text.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Option value requirement.
        /// </summary>
        public CommandLineOptionValueType OptionValueType { get; }

        /// <summary>
        /// Whether to omit this option from help.
        /// </summary>
        public bool Hidden { get; }

        /// <summary>
        /// Gets all option aliases.
        /// </summary>
        /// <returns>The aliases in declaration order.</returns>
        public IEnumerable<string> GetNames()
        {
            return Names;
        }

        internal Action<string?> Action { get; }
    }

    /// <summary>
    /// AOT-safe, deterministic parser for the publisher's static option descriptors.
    /// </summary>
    internal sealed class CommandLineOptionParser : IEnumerable<CommandLineOptionDescriptor>
    {
        /// <summary>
        /// Add a help heading.
        /// </summary>
        /// <param name="text">The heading text.</param>
        public void Add(string text)
        {
            _helpEntries.Add(new HelpEntry(text));
        }

        /// <summary>
        /// Add a string option.
        /// </summary>
        /// <param name="prototype">The aliases and value requirement.</param>
        /// <param name="description">The help text.</param>
        /// <param name="action">The value callback.</param>
        public void Add(string prototype, string description, Action<string> action)
        {
            Add(prototype, description, action, false);
        }

        /// <summary>
        /// Add a string option.
        /// </summary>
        /// <param name="prototype">The aliases and value requirement.</param>
        /// <param name="description">The help text.</param>
        /// <param name="action">The value callback.</param>
        /// <param name="hidden">Whether to omit the option from help.</param>
        public void Add(string prototype, string description, Action<string> action, bool hidden)
        {
            AddOption(prototype, description, value => action(value!), hidden);
        }

        /// <summary>
        /// Add an option whose value is converted to the declared type.
        /// </summary>
        /// <typeparam name="T">The callback value type.</typeparam>
        /// <param name="prototype">The aliases and value requirement.</param>
        /// <param name="description">The help text.</param>
        /// <param name="action">The value callback.</param>
        public void Add<T>(string prototype, string description, Action<T> action)
        {
            Add(prototype, description, action, false);
        }

        /// <summary>
        /// Add an option whose value is converted to the declared type.
        /// </summary>
        /// <typeparam name="T">The callback value type.</typeparam>
        /// <param name="prototype">The aliases and value requirement.</param>
        /// <param name="description">The help text.</param>
        /// <param name="action">The value callback.</param>
        /// <param name="hidden">Whether to omit the option from help.</param>
        public void Add<T>(string prototype, string description, Action<T> action, bool hidden)
        {
            AddOption(prototype, description,
                value => action(ParseValue<T>(value, prototype)), hidden);
        }

        /// <summary>
        /// Parse the supplied arguments.
        /// </summary>
        /// <param name="arguments">The command-line arguments.</param>
        /// <returns>Arguments that are not options supported by the publisher.</returns>
        /// <exception cref="CommandLineOptionException">Thrown for malformed options.</exception>
        public List<string> Parse(IEnumerable<string> arguments)
        {
            var unsupported = new List<string>();
            var values = arguments.ToList();
            for (var index = 0; index < values.Count; index++)
            {
                var argument = values[index];
                if (argument == "--")
                {
                    unsupported.AddRange(values.Skip(index + 1));
                    break;
                }

                if (TryFindOption(argument, out var option, out var optionName,
                    out var value, out var hasValue))
                {
                    ProcessOption(option, argument, optionName, value, hasValue,
                        values, ref index);
                    continue;
                }

                if (TryProcessShortOptionBundle(argument, values, ref index))
                {
                    continue;
                }

                unsupported.Add(argument);
            }
            return unsupported;
        }

        /// <summary>
        /// Write the command-line help.
        /// </summary>
        /// <param name="writer">The output writer.</param>
        public void WriteOptionDescriptions(TextWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            foreach (var entry in _helpEntries)
            {
                if (entry.Heading != null)
                {
                    writer.WriteLine(entry.Heading);
                }
                else if (entry.Option is { Hidden: false } option)
                {
                    WriteOptionDescription(writer, option);
                }
            }
        }

        /// <summary>
        /// Write the environment-variable help as deterministic JSON.
        /// </summary>
        /// <param name="writer">The output writer.</param>
        /// <param name="options">The environment-backed descriptors.</param>
        public static void WriteEnvironmentVariableHelp(TextWriter writer,
            IEnumerable<CommandLineOptionDescriptor> options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            using var stream = new MemoryStream();
            using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }))
            {
                json.WriteStartArray();
                foreach (var descriptor in options)
                {
                    json.WriteStartObject();
                    json.WriteString("key", descriptor.Names[^1]);
                    json.WriteString("description", descriptor.Description);
                    json.WriteEndObject();
                }
                json.WriteEndArray();
            }
            writer.WriteLine(Encoding.UTF8.GetString(stream.GetBuffer(), 0,
                checked((int)stream.Length)));
        }

        /// <inheritdoc/>
        public IEnumerator<CommandLineOptionDescriptor> GetEnumerator()
        {
            return _options.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void AddOption(string prototype, string description,
            Action<string?> action, bool hidden)
        {
            var option = new CommandLineOptionDescriptor(prototype, description,
                action, hidden);
            _options.Add(option);
            _helpEntries.Add(new HelpEntry(option));
            foreach (var name in option.Names)
            {
                _byName.Add(name, option);
            }
        }

        private bool TryFindOption(string argument,
            out CommandLineOptionDescriptor option, out string optionName,
            out string? value, out bool hasValue)
        {
            option = null!;
            optionName = string.Empty;
            value = null;
            hasValue = false;
            if (!IsOption(argument))
            {
                return false;
            }

            var optionPart = argument[(argument[1] == '-' ? 2 : 1)..];
            var separator = optionPart.IndexOfAny(['=', ':']);
            if (separator >= 0)
            {
                value = optionPart[(separator + 1)..];
                optionPart = optionPart[..separator];
                hasValue = true;
            }
            if (_byName.TryGetValue(optionPart, out var found))
            {
                option = found;
                optionName = optionPart;
                return true;
            }
            return false;
        }

        private void ProcessOption(CommandLineOptionDescriptor option, string argument,
            string optionName, string? value, bool hasValue, List<string> values,
            ref int index)
        {
            switch (option.OptionValueType)
            {
                case CommandLineOptionValueType.Required:
                    if (!hasValue)
                    {
                        if (++index == values.Count)
                        {
                            throw new CommandLineOptionException(
                                $"Missing required value for option '{argument}'.");
                        }
                        value = values[index];
                    }
                    option.Action(value);
                    break;
                case CommandLineOptionValueType.Optional:
                    option.Action(hasValue ? value : null);
                    break;
                case CommandLineOptionValueType.None:
                    option.Action(optionName);
                    break;
            }
        }

        private bool TryProcessShortOptionBundle(string argument, List<string> values,
            ref int index)
        {
            if (argument.Length <= 2 || argument[0] != '-' || argument[1] == '-')
            {
                return false;
            }

            var bundle = argument[1..];
            for (var position = 0; position < bundle.Length; position++)
            {
                var optionName = bundle[position].ToString();
                if (!_byName.TryGetValue(optionName, out var option))
                {
                    if (position == 0)
                    {
                        return false;
                    }
                    throw new CommandLineOptionException(
                        $"Cannot use unregistered option '{optionName}' in bundle '{argument}'.");
                }

                var remaining = bundle[(position + 1)..];
                switch (option.OptionValueType)
                {
                    case CommandLineOptionValueType.None:
                        option.Action(bundle);
                        continue;
                    case CommandLineOptionValueType.Optional:
                        option.Action(remaining.Length == 0 ? null : remaining);
                        return true;
                    case CommandLineOptionValueType.Required:
                        if (remaining.Length == 0)
                        {
                            if (++index == values.Count)
                            {
                                throw new CommandLineOptionException(
                                    $"Missing required value for option '-{optionName}'.");
                            }
                            remaining = values[index];
                        }
                        option.Action(remaining);
                        return true;
                }
            }
            return true;
        }

        private static bool IsOption(string value)
        {
            return value.Length > 1 && value[0] == '-' && value != "--";
        }

        /// <summary>
        /// Convert an option's argument to its target type. Command line
        /// arguments are machine facing configuration, so every conversion is
        /// invariant: the host locale must not decide whether "1.5" means one
        /// and a half or fifteen.
        /// </summary>
        private static T ParseValue<T>(string? value, string prototype)
        {
            try
            {
                var type = typeof(T);
                if (type == typeof(string))
                {
                    return value is null ? default! : (T)(object)value;
                }
                if (type == typeof(bool))
                {
                    return (T)(object)bool.Parse(value!);
                }
                if (type == typeof(bool?))
                {
                    return value is null ? default! : (T)(object)bool.Parse(value);
                }
                if (type == typeof(uint))
                {
                    return (T)(object)uint.Parse(value!, CultureInfo.InvariantCulture);
                }
                if (type == typeof(ushort))
                {
                    return (T)(object)ushort.Parse(value!, CultureInfo.InvariantCulture);
                }
                if (type == typeof(ushort?))
                {
                    return value is null ? default! :
                        (T)(object)ushort.Parse(value, CultureInfo.InvariantCulture);
                }
                if (type == typeof(TimeSpan))
                {
                    return (T)(object)TimeSpan.Parse(value!, CultureInfo.InvariantCulture);
                }
                if (type.IsEnum)
                {
                    return (T)Enum.Parse(type, value!, true);
                }
                var result = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
                return result is null ? default! : (T)result;
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException or
                InvalidCastException or OverflowException)
            {
                throw new CommandLineOptionException(
                    $"Could not convert value '{value}' for option '{prototype}'.", ex);
            }
        }

        private static void WriteOptionDescription(TextWriter writer,
            CommandLineOptionDescriptor option)
        {
            const int kDescriptionColumn = 29;
            const int kContinuationColumn = 31;
            var names = option.Names.Select(static name =>
                name.Length == 1 ? $"-{name}" : $"--{name}").ToArray();
            var suffix = option.OptionValueType switch
            {
                CommandLineOptionValueType.Required => "=VALUE",
                CommandLineOptionValueType.Optional => "[=VALUE]",
                _ => string.Empty
            };
            names[^1] += suffix;
            var prototype = "  " + (option.Names[0].Length == 1 ? string.Empty : "    ") +
                string.Join(", ", names);
            if (prototype.Length < kDescriptionColumn)
            {
                writer.Write(prototype.PadRight(kDescriptionColumn));
            }
            else
            {
                writer.WriteLine(prototype);
                writer.Write(new string(' ', kDescriptionColumn));
            }

            var initialWidth = 80 - kDescriptionColumn;
            var continuationWidth = 80 - kContinuationColumn;
            var firstLine = true;
            foreach (var line in Wrap(option.Description, initialWidth, continuationWidth))
            {
                if (!firstLine)
                {
                    writer.Write(new string(' ', kContinuationColumn));
                }
                writer.WriteLine(line);
                firstLine = false;
            }
        }

        private static IEnumerable<string> Wrap(string value, int initialWidth,
            int continuationWidth)
        {
            var width = initialWidth;
            var line = new StringBuilder();
            foreach (var paragraph in value.Split('\n'))
            {
                foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.Length > 0 && line.Length + 1 + word.Length > width)
                    {
                        yield return line.ToString();
                        line.Clear();
                        width = continuationWidth;
                    }
                    if (line.Length > 0)
                    {
                        line.Append(' ');
                    }
                    line.Append(word);
                }
                if (line.Length > 0)
                {
                    yield return line.ToString();
                    line.Clear();
                    width = continuationWidth;
                }
            }
        }

        private sealed class HelpEntry
        {
            public HelpEntry(string heading)
            {
                Heading = heading;
            }

            public HelpEntry(CommandLineOptionDescriptor option)
            {
                Option = option;
            }

            public string? Heading { get; }
            public CommandLineOptionDescriptor? Option { get; }
        }

        private readonly List<CommandLineOptionDescriptor> _options = [];
        private readonly Dictionary<string, CommandLineOptionDescriptor> _byName =
            new(StringComparer.Ordinal);
        private readonly List<HelpEntry> _helpEntries = [];
    }

    /// <summary>
    /// Represents a malformed command-line option.
    /// </summary>
    internal sealed class CommandLineOptionException : Exception
    {
        /// <summary>
        /// Creates an exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        public CommandLineOptionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates an exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The conversion exception.</param>
        public CommandLineOptionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="optionName">The malformed option name.</param>
        public CommandLineOptionException(string message, string optionName)
            : base(message)
        {
            OptionName = optionName;
        }

        /// <summary>
        /// Gets the malformed option name.
        /// </summary>
        public string OptionName { get; } = string.Empty;
    }
}
