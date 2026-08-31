// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Opc.Ua;
    using System;

    internal enum ManagedItemRetryKind
    {
        None,
        Subscription,
        Invalid,
        Bad
    }

    internal static class ManagedSubscriptionRetryPolicy
    {
        public static ManagedItemRetryKind Classify(StatusCode status,
            bool localFailure = false)
        {
            if (StatusCode.IsGood(status) ||
                status == StatusCodes.BadTooManyMonitoredItems)
            {
                return ManagedItemRetryKind.None;
            }
            if (localFailure)
            {
                return ManagedItemRetryKind.Bad;
            }
            if (status == StatusCodes.BadCommunicationError ||
                status == StatusCodes.BadNotConnected ||
                status == StatusCodes.BadSecureChannelClosed ||
                status == StatusCodes.BadSessionClosed ||
                status == StatusCodes.BadSubscriptionIdInvalid)
            {
                return ManagedItemRetryKind.Subscription;
            }
            // Classic routes every server-side monitored-item apply failure
            // through InvalidMonitoredItemRetryDelay. The Bad delay is for
            // locally invalid desired items that never reach the server.
            return ManagedItemRetryKind.Invalid;
        }

        public static TimeSpan GetDelay(ManagedItemRetryKind kind,
            OpcUaSubscriptionOptions options, int attempt)
        {
            ArgumentNullException.ThrowIfNull(options);
            return kind switch
            {
                ManagedItemRetryKind.Subscription => GetDelay(
                    options.SubscriptionErrorRetryDelay,
                    TimeSpan.FromSeconds(2), null, attempt),
                ManagedItemRetryKind.Invalid => GetDelay(
                    options.InvalidMonitoredItemRetryDelayDuration,
                    TimeSpan.FromMinutes(5),
                    options.InvalidMonitoredItemRetryDelayDurationMax,
                    attempt),
                ManagedItemRetryKind.Bad => GetDelay(
                    options.BadMonitoredItemRetryDelayDuration,
                    TimeSpan.FromMinutes(30),
                    // Preserve the classic option pairing until an explicit
                    // compatibility decision changes it.
                    options.InvalidMonitoredItemRetryDelayDurationMax,
                    attempt),
                _ => TimeSpan.MaxValue
            };
        }

        private static TimeSpan GetDelay(TimeSpan? delay,
            TimeSpan defaultDelay, TimeSpan? maxDelay, int attempt)
        {
            if (!delay.HasValue)
            {
                return defaultDelay;
            }
            if (delay.Value == TimeSpan.Zero)
            {
                return TimeSpan.MaxValue;
            }

            var minimum = delay.Value.Duration();
            if (delay.Value > TimeSpan.Zero &&
                !maxDelay.HasValue &&
                minimum > TimeSpan.FromSeconds(10))
            {
                return minimum;
            }

            var maximum = maxDelay?.Duration() ?? defaultDelay;
            if (maximum <= minimum)
            {
                return minimum;
            }
            if (maximum < TimeSpan.FromSeconds(10))
            {
                maximum = TimeSpan.FromSeconds(10);
            }

            var exponent = Math.Min(Math.Max(attempt, 1), 10);
            var multiplier = 1L << exponent;
            var ticks = minimum.Ticks > long.MaxValue / multiplier
                ? long.MaxValue
                : minimum.Ticks * multiplier;
            var calculated = TimeSpan.FromTicks(ticks);
            return calculated > maximum ? maximum : calculated;
        }
    }
}
