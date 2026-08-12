// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection
{
    using global::Azure.IIoT.OpcUa.Core.Exceptions;
    using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
    using System;
    using System.Collections.Frozen;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Exception summarization extensions.
    /// </summary>
    public static class ExceptionSummarizationBuilderEx
    {
        /// <summary>
        /// Exception types owned by
        /// <c>Microsoft.Extensions.Diagnostics.ExceptionSummarization.HttpExceptionSummaryProvider</c>,
        /// which <c>AddStandardResilienceHandler</c> registers.
        /// </summary>
        /// <remarks>
        /// ExceptionSummarizer indexes every registered provider's
        /// SupportedExceptionTypes into a single dictionary and throws
        /// ArgumentException on the first duplicate, so two providers may never
        /// claim the same type. The OPC UA client's fluent builder calls
        /// AddStandardResilienceHandler, which happens as soon as <c>--mcp</c>
        /// registers the MCP tool server, and the providers below used to claim
        /// exactly these four types. That aborted module startup with "An item
        /// with the same key has already been added"; because the api key is
        /// established during that startup, every authenticated endpoint then
        /// answered 401, so turning on <c>--mcp</c> took down the whole API
        /// rather than only itself.
        ///
        /// The types are excluded from what the providers claim rather than
        /// removed from their description tables, so <c>Describe</c> still
        /// renders them if it is reached through a base type. When the HTTP
        /// provider is not registered nothing describes them and they fall back
        /// to the default summary, which is the price of never colliding.
        /// </remarks>
        private static readonly ImmutableHashSet<Type> kClaimedByHttpProvider =
        [
            typeof(WebException),
            typeof(SocketException),
            typeof(OperationCanceledException),
            typeof(TaskCanceledException)
        ];

        /// <summary>
        /// Add default exception summary providers.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="httpProviderRegistered">
        /// Whether something else in the container registers
        /// <c>HttpExceptionSummaryProvider</c>, which
        /// <c>AddStandardResilienceHandler</c> does. When it is registered the
        /// four types it claims are ceded to it, because two providers may not
        /// claim the same type; when it is not, they are kept, because
        /// otherwise nothing would describe them at all.
        /// </param>
        /// <returns></returns>
        public static IExceptionSummarizationBuilder AddDefaultProviders(
            this IExceptionSummarizationBuilder builder,
            bool httpProviderRegistered = false)
        {
            if (!httpProviderRegistered)
            {
                builder.AddProvider<HttpExceptionProvider>();
                builder.AddProvider<BuiltInExceptionProvider>();
                return builder;
            }
            //
            // HttpExceptionProvider is not registered at all here: both of the
            // types it describes are ceded, so it would claim nothing and could
            // never be selected.
            //
            builder.AddProvider<CededBuiltInExceptionProvider>();
            return builder;
        }

        /// <summary>
        /// <see cref="BuiltInExceptionProvider"/> with the ceded types removed.
        /// </summary>
        private sealed class CededBuiltInExceptionProvider : IExceptionSummaryProvider
        {
            public IEnumerable<Type> SupportedExceptionTypes { get; } =
                [.. BuiltInExceptionProvider.SupportedTypes
                    .Where(type => !kClaimedByHttpProvider.Contains(type))];

            public IReadOnlyList<string> Descriptions => _inner.Descriptions;

            public int Describe(Exception exception, out string? additionalDetails)
                => _inner.Describe(exception, out additionalDetails);

            private readonly BuiltInExceptionProvider _inner = new();
        }

        private sealed class HttpExceptionProvider : IExceptionSummaryProvider
        {
            public IEnumerable<Type> SupportedExceptionTypes { get; } =
            [
                typeof(WebException),
                typeof(SocketException),
            ];

            public IReadOnlyList<string> Descriptions => kDescriptions;

            public int Describe(Exception exception, out string? additionalDetails)
            {
                ArgumentNullException.ThrowIfNull(exception);
                additionalDetails = null;
                switch (exception)
                {
                    case OperationCanceledException ex:
                        return ex.CancellationToken.IsCancellationRequested ? 0 : 1;
                    case WebException ex when
                        kWebExceptionStatusMap.TryGetValue(ex.Status, out var webIndex):
                        return webIndex;
                    case SocketException ex when
                        kSocketErrorMap.TryGetValue(ex.SocketErrorCode, out var socketIndex):
                        return socketIndex;
                    default:
                        return -1;
                }
            }

            static HttpExceptionProvider()
            {
                var descriptions = new List<string>();
                var socketErrors = new Dictionary<SocketError, int>();
                foreach (var socketError in Enum.GetValues<SocketError>())
                {
                    socketErrors[socketError] = descriptions.Count;
                    descriptions.Add(socketError.ToString());
                }
                var webStatuses = new Dictionary<WebExceptionStatus, int>();
                foreach (var status in Enum.GetValues<WebExceptionStatus>())
                {
                    webStatuses[status] = descriptions.Count;
                    descriptions.Add(status.ToString());
                }
                kDescriptions = [.. descriptions];
                kSocketErrorMap = socketErrors.ToFrozenDictionary();
                kWebExceptionStatusMap = webStatuses.ToFrozenDictionary();
            }

            private static readonly FrozenDictionary<WebExceptionStatus, int> kWebExceptionStatusMap;
            private static readonly FrozenDictionary<SocketError, int> kSocketErrorMap;
            private static readonly ImmutableArray<string> kDescriptions;
        }

        private sealed class BuiltInExceptionProvider : IExceptionSummaryProvider
        {
            /// <summary>
            /// The full set this provider can describe. The ceded variant
            /// filters it; see <see cref="kClaimedByHttpProvider"/>.
            /// </summary>
            internal static IEnumerable<Type> SupportedTypes => kSupported.Keys;

            public IEnumerable<Type> SupportedExceptionTypes => kSupported.Keys;

            public IReadOnlyList<string> Descriptions => kDescriptions;

            public int Describe(Exception exception, out string? additionalDetails)
            {
                ArgumentNullException.ThrowIfNull(exception);
                additionalDetails = exception is OperationCanceledException ?
                    "Reason unknown" : exception.Message;
                if (kSupported.TryGetValue(exception.GetType(), out var index))
                {
                    return index;
                }
                foreach (var supportedType in kSupported)
                {
                    if (supportedType.Key.IsAssignableFrom(exception.GetType()))
                    {
                        return supportedType.Value;
                    }
                }
                return 0;
            }

            static BuiltInExceptionProvider()
            {
                var descriptions = new Dictionary<Type, string>
                {
                    [typeof(Exception)] = "Unknown exception",
                    [typeof(ResourceExhaustionException)] =
                        "Thrown when a resource is exhausted and the system cannot handle the operation",
                    [typeof(ResourceInvalidStateException)] =
                        "A resource is in a state that does not allow the operation to continue.",
                    [typeof(ResourceOutOfDateException)] =
                        "A resource cannot be updated because it is not in the expected state.",
                    [typeof(ResourceNotFoundException)] = "The requested resource could not be found.",
                    [typeof(ExternalDependencyException)] =
                        "An external system is not available or returned an error.",
                    [typeof(BadRequestException)] =
                        "The request contains invalid information or parameters.",
                    [typeof(InvalidConfigurationException)] =
                        "The configuration provided to the system is invalid.",
                    [typeof(MethodCallStatusException)] =
                        "A method call resulted in an error with explicit error detail.",
                    [typeof(MethodCallException)] = "A method call resulted in an error.",
                    [typeof(MessageSizeLimitException)] =
                        "The message is too large for the system to handle.",
                    [typeof(TemporarilyBusyException)] =
                        "The system is temporarily busy, please try again later.",
                    [typeof(SerializerException)] = "Serialization or deserialization failed.",
                    [typeof(StorageException)] = "Accessing persistent storage failed.",
                    [typeof(ResourceConflictException)] =
                        "The specified resource already exists or conflicts with an existing resource.",
                    [typeof(NotSupportedException)] = "The operation is not supported.",
                    [typeof(NotImplementedException)] = "The operation has not yet been implemented.",
                    [typeof(TimeoutException)] = "The operation timed out.",
                    [typeof(OperationCanceledException)] = "The operation was cancelled.",
                    [typeof(TaskCanceledException)] = "The operation was cancelled.",
                    [typeof(ArgumentNullException)] = "A parameter was unexpectedly null.",
                    [typeof(ArgumentException)] = "A parameter was invalid.",
                    [typeof(ArgumentOutOfRangeException)] =
                        "A parameter was outside of the allowed range."
                };

                kDescriptions = [.. descriptions.Values];
                kSupported = descriptions.Keys
                    .Select((value, index) => KeyValuePair.Create(value, index))
                    .Skip(1)
                    .ToImmutableDictionary();
            }

            private static readonly ImmutableArray<string> kDescriptions;
            private static readonly ImmutableDictionary<Type, int> kSupported;
        }
    }
}
