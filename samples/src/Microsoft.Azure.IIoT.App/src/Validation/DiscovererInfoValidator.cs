// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Validation
{
    using Microsoft.Azure.IIoT.App.Models;
    using FluentValidation;
    using System;
    using System.Collections.Generic;

    public class DiscovererInfoValidator : AbstractValidator<DiscovererInfoRequested>
    {
        private static readonly ValidationUtils utils = new();

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

            RuleFor(p => p.RequestedDiscoveryUrls)
                .Must(BeAValidDiscoveryUrl)
                .WithMessage("Invalid input value for discovery url. Clear and insert a new value");
        }

        private bool BeValidAddressRanges(string value)
        {
            return utils.ShouldUseDefaultValue(value) /*|| AddressRange.TryParse(value, out _)*/;
        }

        private bool BeValidPortRanges(string value)
        {
            return utils.ShouldUseDefaultValue(value) /*|| PortRange.TryParse(value, out _)*/;
        }

        private bool BeAPositiveInteger(string value)
        {
            return utils.ShouldUseDefaultValue(value) || (int.TryParse(value, out var res) && res > 0);
        }

        private bool BeAValidTimeFormat(string value)
        {
            return utils.ShouldUseDefaultValue(value) || (TimeSpan.TryParse(value, out var res) && res.TotalMilliseconds > 0);
        }

        private bool BeAValidDiscoveryUrl(List<string> value)
        {
            return value == null || !(value.Contains(null) || value.Contains(string.Empty));
        }
    }
}
