// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Validation
{
    using Microsoft.Azure.IIoT.App.Models;
    using FluentValidation;

    public class ListNodeValidator : AbstractValidator<ListNodeRequested>
    {
        private static readonly ValidationUtils utils = new();

        public ListNodeValidator()
        {
            RuleFor(p => p.RequestedPublishingInterval)
                .Must(BeAValidIntervalMs)
                .WithMessage("Publishing interval cannot be less than 0 ms.");

            RuleFor(p => p.RequestedSamplingInterval)
                .Must(BeAValidIntervalMs)
                .WithMessage("Sampling interval cannot be less than 0 ms.");

            RuleFor(p => p.RequestedHeartbeatInterval)
                .Must(BeAValidIntervalSec)
                .WithMessage("Heartbeat interval cannot be less than 0 second.");
        }

        private bool BeAValidIntervalMs(string value)
        {
            return utils.ShouldUseDefaultValue(value) || (double.TryParse(value, out var result) && result >= 0);
        }

        private bool BeAValidIntervalSec(string value)
        {
            return utils.ShouldUseDefaultValue(value) || (double.TryParse(value, out var result) && result >= 0);
        }
    }
}
