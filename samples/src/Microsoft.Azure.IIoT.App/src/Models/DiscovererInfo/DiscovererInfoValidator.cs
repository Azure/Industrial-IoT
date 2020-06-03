// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Models
{
    using System;
    using FluentValidation;
    using Microsoft.Azure.IIoT.Net.Models;

    public class DiscovererInfoValidator : AbstractValidator<DiscovererInfoRequested>
    {
        public DiscovererInfoValidator()
        {
            RuleFor(p => p.RequestedAddressRangesToScan)
                .Must(BeValidAddressRanges)
                .WithMessage("Invalid input value for address ranges.");

            RuleFor(p => p.RequestedPortRangesToScan)
                .Must(BeValidPortRanges)
                .WithMessage("Invalid input value for port ranges.");

            RuleFor(p => p.RequestedMaxNetworkProbes)
                .Must(BeAPositiveInteger)
                .WithMessage("Max network probes must be a positive integer.");

            RuleFor(p => p.RequestedMaxPortProbes)
                .Must(BeAPositiveInteger)
                .WithMessage("Max port probes must be a positive integer.");

            RuleFor(p => p.RequestedNetworkProbeTimeout)
                .Must(BeAValidTimeFormat)
                .WithMessage("Invalid input value for network probe timeout.");

            RuleFor(p => p.RequestedPortProbeTimeout)
                .Must(BeAValidTimeFormat)
                .WithMessage("Invalid input value for port probe timeout.");

            RuleFor(p => p.RequestedIdleTimeBetweenScans)
                .Must(BeAValidTimeFormat)
                .WithMessage("Invalid input value for idle time between scans.");
        }

        /// <summary>
        /// Checks if the default value should be used. The default value 
        /// should be used when the user did not input any value
        /// </summary>
        /// <param name="value">User input</param>
        /// <returns>True if the input is empty, false otherwise</returns>
        private bool ShouldUseDefaultValue(string value)
        {
            // User did not input the value, so the default value will be used
            return string.IsNullOrWhiteSpace(value);
        }

        private bool BeValidAddressRanges(string value)
        {
            if (ShouldUseDefaultValue(value))
            {
                return true;
            }

            return AddressRange.TryParse(value, out _);
        }

        private bool BeValidPortRanges(string value)
        {
            if (ShouldUseDefaultValue(value))
            {
                return true;
            }

            return PortRange.TryParse(value, out _);
        }

        private bool BeAPositiveInteger(string value)
        {
            if (ShouldUseDefaultValue(value))
            {
                return true;
            }

            if (int.TryParse(value, out int res))
            {
                return res > 0;
            }

            return false;
        }

        private bool BeAValidTimeFormat(string value)
        {
            if (ShouldUseDefaultValue(value))
            {
                return true;
            }

            if (TimeSpan.TryParse(value, out TimeSpan res)){
                return res.TotalMilliseconds > 0;
            }

            return false;
        }
    }
}
