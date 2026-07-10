// ------------------------------------------------------------
//  Copyright (c) Microsoft.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Servers
{
    using Azure.IIoT.OpcUa.Core.Storage;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Callback
    /// </summary>
    /// <param name="method"></param>
    /// <param name="request"></param>
    /// <param name="headers"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    internal delegate Task<(int status, ReadOnlySequence<byte> response)> Execute(
        Method method, ReadOnlySequence<byte> request, Dictionary<string, string> headers,
        CancellationToken ct);

    /// <summary>
    /// Method
    /// </summary>
    /// <param name="String"></param>
    /// <param name="Uri"></param>
    /// <param name="ProtocolVersion"></param>
    internal sealed record class Method(string String, Uri? Uri = null,
        string? ProtocolVersion = null);

    /// <summary>
    /// Simple .http parser. Does not support multiline, scripting or environment
    /// variables
    /// </summary>
    internal sealed class DotHttpFileParser : IAsyncDisposable
    {
        /// <summary>
        /// Create parser
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="execute"></param>
        /// <param name="logger"></param>
        /// <param name="root"></param>
        /// <param name="provider"></param>
        /// <param name="ct"></param>
        public DotHttpFileParser(Stream request, Stream response, Execute execute,
            ILogger logger, string? root = null, IFileProvider? provider = null,
            CancellationToken ct = default)
        {
            _root = root ?? Directory.GetCurrentDirectory();
            _request = new StreamReader(request, leaveOpen: true);
            _response = new StreamWriter(response, leaveOpen: true);
            _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _directives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _provider = provider;
            _execute = execute;
            _logger = logger;
            _parser = ParseAsync(ct);
        }

        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="execute"></param>
        /// <param name="logger"></param>
        /// <param name="root"></param>
        /// <param name="provider"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task ParseAsync(Stream request, Stream response,
            Execute execute, ILogger logger, string? root = null,
            IFileProvider? provider = null, CancellationToken ct = default)
        {
            await using var parser = new DotHttpFileParser(request,
                response, execute, logger, root, provider, ct: ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="request"></param>
        /// <param name="execute"></param>
        /// <param name="logger"></param>
        /// <param name="root"></param>
        /// <param name="provider"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<string> ParseAsync(string request, Execute execute,
            ILogger logger, string? root = null, IFileProvider? provider = null,
            CancellationToken ct = default)
        {
            var req = new MemoryStream(Encoding.UTF8.GetBytes(request));
            await using (req.ConfigureAwait(false))
            {
                var res = new MemoryStream();
                await using (res.ConfigureAwait(false))
                {
                    await DotHttpFileParser.ParseAsync(req, res, execute,
                        logger, root, provider, ct).ConfigureAwait(false);
                    return Encoding.UTF8.GetString(res.ToArray());
                }
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            try
            {
                await _parser.ConfigureAwait(false);
            }
            finally
            {
                _response.Dispose();
                _request.Dispose();
            }
        }

        /// <summary>
        /// Parse request and response files and execute
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ParseAsync(CancellationToken ct)
        {
            while (true)
            {
                var line = ReadLine();

                // End of file or seperator reached?
                if (line?.StartsWith("###", StringComparison.Ordinal) != false)
                {
                    await ExecuteAsync(ct).ConfigureAwait(false);

                    // EOF?
                    if (line == null)
                    {
                        break;
                    }

                    // Write the ### seperator and possible comments and reset state
                    WriteLine(line);
                    WriteLine();
                    Reset();
                    continue;
                }

                if (ParseComment(line))
                {
                    continue;
                }

                if (line.Length == 0)
                {
                    // Empty line is a seperator between headers and body
                    if (_state == State.Headers)
                    {
                        _state = State.Body;
                    }

                    if (_state != State.Body)
                    {
                        // We skip all empty lines unless we are in
                        // inline body
                        continue;
                    }
                }

                switch (_state)
                {
                    case State.Method:
                        ParseMethod(line);
                        break;
                    case State.Headers:
                        ParseHeaders(line);
                        break;
                    case State.Body:
                        ParseBody(line);
                        break;
                }
            }
        }

        /// <summary>
        /// Parse comment
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool ParseComment(string line)
        {
            var comment = line;
            if (line.StartsWith("//", StringComparison.Ordinal))
            {
                comment = comment.TrimStart('/').Trim();
            }
            else if (line.StartsWith('#'))
            {
                comment = comment.TrimStart('#').Trim();
            }
            else
            {
                return false;
            }

            if (comment.StartsWith('@'))
            {
                ParseDirective(line, comment);
                return true;
            }

            // Parse through comments but skip
            WriteLine(line);
            return true;
        }

        /// <summary>
        /// Parse directive
        /// </summary>
        /// <param name="line"></param>
        /// <param name="comment"></param>
        private void ParseDirective(string line, string comment)
        {
            var value = string.Empty;
            var directive = comment;

            var idx = directive.IndexOf(' ', StringComparison.Ordinal);
            if (idx >= 0)
            {
                directive = comment[..idx].Trim();
                value = comment[idx..].Trim();
            }
            switch (directive)
            {
                case Directive.NoLog:
                case Directive.ContinueOnError:
                case Directive.OnError:
                    if (value.Length == 0)
                    {
                        // Nothing should follow
                        break;
                    }
                    ThrowFormatException(
                        $"Arguments for directive {directive} are not supported", line);
                    break;
                case Directive.Name:
                    if (value.Length != 0)
                    {
                        // A string should follow
                        break;
                    }
                    ThrowFormatException(
                        $"String argument missing in directive {directive}", line);
                    break;
                case Directive.Retries:
                    if (int.TryParse(value, out _))
                    {
                        // An integer should follow
                        break;
                    }
                    ThrowFormatException(
                        $"Argument for directive {directive} must be an integer", line);
                    break;
                case Directive.Delay:
                case Directive.Timeout:
                    if (int.TryParse(value, out _))
                    {
                        break;
                    }
                    ThrowFormatException(
                        $"Argument for directive {directive} must be a duration", line);
                    break;
                case "@connection-timeout":
                case "@no-redirect":
                case "@no-cookie-jar":
                    _logger.SkippingUnsupportedDirective(directive, _lineNumber);
                    return;
                default:
                    _logger.UnknownDirectiveWarning(directive, _lineNumber);
                    return;
            }
            _directives[comment] = value;
        }

        /// <summary>
        /// Parse method line
        /// </summary>
        /// <param name="line"></param>
        private void ParseMethod(string line)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var method = parts[0].ToUpperInvariant();
                // Check if this is a http method
                switch (method)
                {
                    case "POST":
                    case "GET":
                    case "PUT":
                    case "DELETE":
                    case "PATCH":
                    case "OPTIONS":
                    case "TRACE":
                    case "CONNECT":
                    case "HEAD":
                        try
                        {
                            var uri = new Uri(parts[1], UriKind.RelativeOrAbsolute);
                            if (!string.IsNullOrEmpty(uri.PathAndQuery))
                            {
                                _method = new Method(method, uri,
                                    parts.Length >= 3 ? parts[2] : null);
                                break;
                            }
                        }
                        catch { }
                        _method = new Method(parts[1]);
                        break;
                    default:
                        ThrowFormatException("Invalid method format", line);
                        return;
                }
            }
            else
            {
                _method = new Method(line);
            }

            // Capture and write method to output
            WriteLine(line);
            _state = State.Headers;
        }

        /// <summary>
        /// Parse headers
        /// </summary>
        /// <param name="line"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void ParseHeaders(string line)
        {
            var idx = line.IndexOf(':', StringComparison.Ordinal);
            if (idx < 0)
            {
                ThrowFormatException("Invalid header", line);
            }

            var k = line[..idx].Trim();
            var v = line[(idx + 1)..].Trim();
            _headers.Add(k, v);
        }

        /// <summary>
        /// Parse body
        /// </summary>
        /// <param name="line"></param>
        private void ParseBody(string line)
        {
            if (line.StartsWith('<'))
            {
                _input = line[1..].Trim();
            }
            else if (line.StartsWith(">>!", StringComparison.Ordinal))
            {
                _append = true;
                _output = line[3..].Trim();
            }
            else if (line.StartsWith(">>", StringComparison.Ordinal))
            {
                _append = false;
                _output = line[2..].Trim();
            }
            else
            {
                _body += line;
            }
        }

        /// <summary>
        /// Execute the request
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        private async Task ExecuteAsync(CancellationToken ct)
        {
            if (_method == null)
            {
                // Nothing to do
                return;
            }

            if (_executionFailure) // Current execution is in failed state
            {
                // Previously failed, skip unless instructed to run on error
                if (!_directives.ContainsKey(Directive.OnError))
                {
                    WriteLine("// @skipped reason = error");
                    return;
                }
            }
            else if (_directives.ContainsKey(Directive.OnError))
            {
                // There was no error, therefore do not run this request
                WriteLine("// @skipped reason = success");
                return;
            }

            // Get content type
            if (!_headers.TryGetValue("Content-Type", out var contentType))
            {
                contentType = "application/json";
            }

            var jsonPayload = contentType == "application/json";
            var payload = Encoding.UTF8.GetBytes(_body.Trim() ?? string.Empty);
            if (!string.IsNullOrEmpty(_input))
            {
                payload = await ReadFileAsync(_input, ct).ConfigureAwait(false);
            }
            else if (!jsonPayload && payload.Length > 0)
            {
                throw new FormatException(
                    "Only json content type supported inline");
            }

            if (!TryGetCount(Directive.Retries, out var retries))
            {
                retries = 0;
            }

            for (var i = 0; i < retries + 1; i++)
            {
                if (TryGetDuration(Directive.Delay, out var delay))
                {
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                if (TryGetDuration(Directive.Timeout, out var timeout))
                {
                    cts.CancelAfter(timeout);
                }

                var (status, result) = await _execute(_method,
                    new ReadOnlySequence<byte>(payload), _headers,
                    cts.Token).ConfigureAwait(false);

                if (retries != 0)
                {
                    WriteLine("// @retry attempt = " + (i + 1));
                }

                // Write status and result
                WriteLine(status.ToString(CultureInfo.InvariantCulture));
                WriteLine();

                if (!string.IsNullOrEmpty(_output))
                {
                    await WriteFileAsync(_output, _append, result.ToArray(), ct).ConfigureAwait(false);
                }
                else if (jsonPayload && result.Length > 0)
                {
                    WriteLine(Encoding.UTF8.GetString(result.ToArray()));
                    WriteLine();
                }

                _executionFailure = !_directives.ContainsKey(Directive.ContinueOnError)
                    && status >= 400;
                if (!_executionFailure)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Write to file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="append"></param>
        /// <param name="result"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task WriteFileAsync(string file, bool append,
            ReadOnlyMemory<byte> result, CancellationToken ct)
        {
            file = Path.GetFileName(file);
            var stream = _provider?.GetFileInfo(file) is IFileInfoEx fi
                && fi.IsWritable
                ? fi.CreateWriteStream()
                : File.Open(Path.Combine(_root, file), append
                    ? FileMode.Append : FileMode.Create);
            await using (stream.ConfigureAwait(false))
            {
                await stream.WriteAsync(result, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Read from file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<byte[]> ReadFileAsync(string file, CancellationToken ct)
        {
            file = Path.GetFileName(file);
            if (_provider != null)
            {
                var stream = _provider.GetFileInfo(file).CreateReadStream();
                await using (stream.ConfigureAwait(false))
                {
                    var payload = new MemoryStream();
                    await using (payload.ConfigureAwait(false))
                    {
                        await stream.CopyToAsync(payload, ct).ConfigureAwait(false);
                        return payload.ToArray();
                    }
                }
            }
            return await File.ReadAllBytesAsync(Path.Combine(_root, file),
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Write line
        /// </summary>
        /// <param name="line"></param>
        private void WriteLine(string? line = null)
        {
            _logger.LineDebug(_lineNumber, line);

            if (_directives.ContainsKey(Directive.NoLog))
            {
                return;
            }
            _response.WriteLine(line);
        }

        /// <summary>
        /// Read line
        /// </summary>
        /// <returns></returns>
        private string? ReadLine()
        {
            // TODO: Next line with indent is still same line as per spec.

            var line = _request.ReadLine()?.Trim();
            if (line != null)
            {
                _lineNumber++;
            }
            return line;
        }

        /// <summary>
        /// Get duration directive
        /// </summary>
        /// <param name="directive"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool TryGetDuration(string directive, out TimeSpan value)
        {
            if (_directives.TryGetValue(directive, out var s) &&
                int.TryParse(s, out var seconds))
            {
                value = TimeSpan.FromSeconds(seconds);
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Get counter
        /// </summary>
        /// <param name="directive"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool TryGetCount(string directive, out int value)
        {
            if (_directives.TryGetValue(directive, out var s) &&
                int.TryParse(s, out var count))
            {
                value = count;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Throw exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="line"></param>
        /// <exception cref="FormatException"></exception>
        private void ThrowFormatException(string message, string line)
        {
            throw new FormatException($"{message} (line #{_lineNumber}: '{line}')");
        }

        /// <summary>
        /// Reset
        /// </summary>
        private void Reset()
        {
            _body = string.Empty;
            _method = null;
            _input = string.Empty;
            _output = string.Empty;
            _append = false;
            _headers.Clear();
            _state = State.Method;
            _directives.Clear();
        }

        /// <summary>
        /// Parser state
        /// </summary>
        private enum State
        {
            Method,
            Headers,
            Body
        }

        /// <summary>
        /// Request directives
        /// </summary>
        internal static class Directive
        {
            /// <summary>
            /// Disable logging for this request after this directive.
            /// This directive must be applied for every request and
            /// on the first line so that nothing is emitted to the log.
            /// </summary>
            public const string NoLog = "@no-log";

            /// <summary>
            /// Timeout for the request. If the request times out it
            /// will be an error and all further requests are not sent.
            /// </summary>
            public const string Timeout = "@timeout";

            /// <summary>
            /// Retry this number of times in case of an error. An error
            /// is any request that returns with status code >= 400.
            /// </summary>
            public const string Retries = "@retries";

            /// <summary>
            /// Delay before executing a request. If retries are specified
            /// the delay applies before every attempt.
            /// </summary>
            public const string Delay = "@delay";

            /// <summary>
            /// Invoke the request only when the previous request failed.
            /// If the previous request has @continue-on-error directive
            /// this request will not be executed. If the request succeeds
            /// the next request after is run.
            /// </summary>
            public const string OnError = "@on-error";

            /// <summary>
            /// Continue to next request even if the request failed.
            /// The default behavior is to stop execution of requests
            /// except for the next request with @on-error directive.
            /// </summary>
            public const string ContinueOnError = "@continue-on-error";

            /// <summary>
            /// Name of the request for annotation purposes only.
            /// </summary>
            public const string Name = "@name";
        }

        private readonly Dictionary<string, string> _headers;
        private readonly Dictionary<string, string> _directives;
        private readonly StreamReader _request;
        private readonly StreamWriter _response;
        private readonly IFileProvider? _provider;
        private readonly Execute _execute;
        private readonly ILogger _logger;
        private readonly Task _parser;
        private readonly string _root;
        private int _lineNumber;
        private Method? _method;
        private bool _executionFailure;
        private string _body = string.Empty;
        private string _input = string.Empty;
        private bool _append;
        private string _output = string.Empty;
        private State _state = State.Method;
    }

    /// <summary>
    /// Source-generated logging for DotHttpFileParser
    /// </summary>
    internal static partial class DotHttpFileParserLogging
    {
        private const int kEventClass = 0;

        [LoggerMessage(EventId = kEventClass + 0, Level = LogLevel.Debug,
            Message = "Skipping unsupported directive {Directive} at line#{Line}.")]
        public static partial void SkippingUnsupportedDirective(this ILogger logger, string directive, int line);

        [LoggerMessage(EventId = kEventClass + 1, Level = LogLevel.Warning,
            Message = "Skipping unsupported directive {Directive} at line#{Line}.")]
        public static partial void UnknownDirectiveWarning(this ILogger logger, string directive, int line);

        [LoggerMessage(EventId = kEventClass + 2, Level = LogLevel.Debug,
            Message = "line#{LineNumber}: {Line}.")]
        public static partial void LineDebug(this ILogger logger, int lineNumber, string? line);
    }
}
