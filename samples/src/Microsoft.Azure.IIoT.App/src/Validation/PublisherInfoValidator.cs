// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Validation {
    using FluentValidation;
    using Microsoft.Azure.IIoT.App.Models;

    public class PublisherInfoValidator : AbstractValidator<PublisherInfoRequested> {

        private static readonly ValidationUtils utils = new ValidationUtils();

        public PublisherInfoValidator() {
            RuleFor(p => p.RequestedMaxWorkers)
                .Must(BePositiveInteger)
                .WithMessage("Max workers value cannot be less than 1.");

            RuleFor(p => p.RequestedHeartbeatInterval)
                .Must(BeAValidIntervalSec)
                .WithMessage("Heartbeat interval cannot be less than 0 seconds.");

            RuleFor(p => p.RequestedJobCheckInterval)
                .Must(BeAValidIntervalSec)
                .WithMessage("Job check interval cannot be less than 0 seconds.");
        }

        private bool BePositiveInteger(string value) {
            if (utils.ShouldUseDefaultValue(value)) {
                return true;
            }

            if (int.TryParse(value, out int result)) {
                return result > 0;
            }

            return false;
        }

        private bool BeAValidIntervalSec(string value) {
            if (utils.ShouldUseDefaultValue(value)) {
                return true;
            }

            if (double.TryParse(value, out double result)) {
                return result >= 0;
            }

            return false;
        }
    }
}
