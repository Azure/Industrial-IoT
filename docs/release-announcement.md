# Release announcement

## Azure Industrial IoT Platform Release 2.8.2

We are pleased to announce the release of version 2.8.2 of our Industrial IoT Platform components as a second patch update of the 2.8 Long-Term Support (LTS) release. This release contains important backward compatibility fixes with version 2.5.x, performance optimizations as well as security updates and bugfixes.

> IMPORTANT
> We suggest to update from  version 2.5 or later to ensure secure operations of your deployment. OPC Publisher 2.8.2 addresses backwards compatibilities issues with version 2.5.x.

### Fundamentals related fixes

- [OPC Publisher] Implemented the backwards compatible [Direct Methods API](docs/modules/publisher-directmethods.md) of 2.5.x publisher. The migration path is documented [here](docs/modules/publisher-migrationpath.md)
- [OPC Publisher] Optimizations in opc subscriptions/monitored items management in case of configuration changes. Only incremental changes are applied to a subscription.
- [OPC Publisher] Added support for setting QueueSize on monitored items for publisher in standalone mode.
- [OPC Publisher] Hardened the retry mechanism for activating monitored items.

## Azure Industrial IoT Platform Release 2.8.1

We are pleased to announce the release of version 2.8.1 of our Industrial IoT Platform components as the first patch update of the 2.8 Long-Term Support (LTS) release. This release contains important security updates, bugfixes and performance optimizations.

> IMPORTANT
> Please note that OPC Publisher 2.8.1 is not backwards compatible with version 2.5.x.

## Azure Industrial IoT Platform Release 2.8

We are pleased to announce the release of version 2.8 of our Industrial IoT Platform as well as the declaration of Long-Term Support (LTS) for this version.
While we continue to develop and release updates to our ongoing projects on GitHub, we now also offer a branch that will only get critical bug fixes and security updates starting in July 2021. Customers can rely upon a longer-term support lifecycle for these LTS builds, providing stability and assurance for the planning on longer time horizons our customers require. The LTS branch offers customers a guarantee that they will benefit from any necessary security or critical bug fixes with minimal impact to their deployments and module interactions. At the same time, customers can access the latest updates in the main branch to keep pace with the latest developments and fastest cycle time for product updates.

> IMPORTANT
> We suggest to update from the version 2.6 or later to ensure secure operations of your deployment. 2.8.0 is not backwards compatible with version 2.5.x

Version 2.8.0 includes an updated version of the IoT Edge Runtime, a new Linux base image for all Linux deployments, and several bug fixes. The detailed changes can be found [here](https://github.com/Azure/Industrial-IoT/releases/tag/2.8.0).
