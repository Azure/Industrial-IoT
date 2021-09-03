// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Validation {
    using System;
    using FluentValidation;
    using System.Collections.Generic;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.Net.Models;

    public class DiscovererInfoValidator : AbstractValidator<DiscovererInfoRequested> {
        private static readonly ValidationUtils utils = new ValidationUtils();

        public DiscovererInfoValidator() {
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

        private bool BeValidAddressRanges(string value) {
            if (utils.ShouldUseDefaultValue(value))
            {
                return true;
            }

            return AddressRange.TryParse(value, out _);
        }

        private bool BeValidPortRanges(string value) {
            if (utils.ShouldUseDefaultValue(value))
            {
                return true;
            }

            return PortRange.TryParse(value, out _);
        }

        private bool BeAPositiveInteger(string value) {
            if (utils.ShouldUseDefaultValue(value))
            {
                return true;
            }

            if (int.TryParse(value, out int res))
            {
                return res > 0;
            }

            return false;
        }

        private bool BeAValidTimeFormat(string value) {
            if (utils.ShouldUseDefaultValue(value))
            {
                return true;
            }

            if (TimeSpan.TryParse(value, out TimeSpan res)){
                return res.TotalMilliseconds > 0;
            }

            return false;
        }

        private bool BeAValidDiscoveryUrl(List<string> value) {
            if (value != null) {
                return !(value.Contains(null) || value.Contains(string.Empty));
            }

            return true;
        }
    }
}
