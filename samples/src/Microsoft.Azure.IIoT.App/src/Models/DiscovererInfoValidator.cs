using System;
using FluentValidation;

namespace Microsoft.Azure.IIoT.App.Models
{
    public class DiscovererInfoValidator : AbstractValidator<DiscovererInfoRequested>
    {
        public DiscovererInfoValidator()
        {
            RuleFor(p => p.RequestedNetworkProbeTimeout)
                .Must(BeAValidTimeFormat)
                .WithMessage("Invalid input value for network probe timeout.");

            RuleFor(p => p.RequestedPortProbeTimeout)
                .Must(BeAValidTimeFormat)
                .WithMessage("Invalid input value for port probe timeout.");

            RuleFor(p => p.RequestedIdleTimeBetweenScans)
                .Must(BeAValidTimeFormat)
                .WithMessage("Invalid input value for idle time between scans.");

            RuleFor(p => p.RequestedMaxNetworkProbes)
                .Must(BeAPositiveNumber)
                .WithMessage("Requested max network probes must be a positive integer.");

            RuleFor(p => p.RequestedMaxPortProbes)
                .Must(BeAPositiveNumber)
                .WithMessage("Requested max port probes must be a positive integer.");

            RuleFor(p => p.RequestedAddressRangesToScan)
                .Must(BeAPositiveNumber)
                .WithMessage("Requested address ranges to scan must be a positive integer.");

            RuleFor(p => p.RequestedPortRangesToScan)
                .Must(BeAPositiveNumber)
                .WithMessage("Requested port ranges to schan must be a positive integer.");
        }

        private bool BeAValidTimeFormat(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                // User did not input the value, so the default value will be used
                return true;
            }
            return TimeSpan.TryParse(value, out TimeSpan res);
        }

        private bool BeAPositiveNumber(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                // User did not input the value, so the default value will be used
                return true;
            }

            if (int.TryParse(value, out int res))
            {
                return res > 0;
            }

            return false;
        }
    }
}
