# How to release Helm chart

* [How to release Helm chart](#how-to-release-helm-chart)
  * [Update the Helm Chart](#update-the-helm-chart)
  * [Test the Helm Chart](#test-the-helm-chart)
  * [Release the Helm Chart](#release-the-helm-chart)
  * [Change Links in `README.md`](#change-links-in-readmemd)
  * [Publish the Helm Chart](#publish-the-helm-chart)
    * [Create a Helm Chart Package](#create-a-helm-chart-package)
    * [Publish to `https://azureiiot.blob.core.windows.net/helm` Repository](#publish-to-httpsazureiiotblobcorewindowsnethelm-repository)
    * [Publish to `https://azure.github.io/Industrial-IoT/helm` Repository](#publish-to-httpsazuregithubioindustrial-iothelm-repository)
    * [Publish to `https://microsoft.github.io/charts/repo` Repository](#publish-to-httpsmicrosoftgithubiochartsrepo-repository)

This is a reference guide that lists the steps that should be performed to release a new version of
`azure-industrial-iot` Helm chart. It talks specifically about releasing the chart for `2.8.2` version of
Industrial IoT platform. The steps for other upcoming versions would be similar.

A sample execution of those steps can be traced by following changes that were applied on top of `release/2.7.206`
branch to create `helm/0.3.2` and then `helm/0.3.2_pub` branches.

## Update the Helm Chart

After we have a `2.8.2` release of the platform the following files should be updated:

* `deploy/helm/azure-industrial-iot/values.yaml`:
  * Value of `image:tag` should be updated so that the chart consumes latest release (`2.8.2`).
* `deploy/helm/azure-industrial-iot/Chart.yaml`:
  * Value of `appVersion` should be changed to align with value of `image:tag` of `values.yaml` (`2.8.2`).
  * Value of `version` should be checked to reflect intended version of next release. For `2.8.2` this should be `0.4.2`.
* `deploy/helm/azure-industrial-iot/README.md`: version of components deployed by the chart is references a few times in this file.
  Those should be changes to align with value of `image:tag` of `values.yaml`.
* `docs/deploy/howto-deploy-helm.md`: new entries should be added for the version of that Helm chart that will be released.
  Note that the link to the `README.md` of the chart can be set to the link of the tag that we will create,
  i.e. `https://github.com/Azure/Industrial-IoT/blob/helm_0.4.2/deploy/helm/azure-industrial-iot/README.md` for `helm_0.4.2` tag.
* `docs/deploy/howto-add-aks-to-ps1.md`: update the version number of the chart (`0.4.2`).

## Test the Helm Chart

After the above changes validate the chart by

* [installing](https://helm.sh/docs/helm/helm_install/) the chart
* [upgrading](https://helm.sh/docs/helm/helm_upgrade/) deployment of a previous version
* performing [rollback](https://helm.sh/docs/helm/helm_rollback/)

On top of the above tests, two distinct usage scenarios should be validated:

* deploying the platform while providing all configuration values through `values.yaml`. In this case
  `loadConfFromKeyVault` should be set to `false`.
* deploying the platform while providing only strictly necessary configuration values from `values.yaml` and consuming
  the rest of the configuration values from a Key Vault. In this case `loadConfFromKeyVault` should be set to `true`.

Please check `README.md` of the chart for more details on `loadConfFromKeyVault` and configuration values that will be
required when `loadConfFromKeyVault` is set to `true`.

## Release the Helm Chart

After the chart is checked, the above changes should comprise `helm/0.4.2` branch and release of the new version of the
Helm chart with `helm_0.4.2` tag.

## Change Links in `README.md`

Before we can publish the newly released version of the chart to Helm chart repositories we should change all relative
links in the `README.md` of the chart to absolute ones pointing to `helm_0.4.2` tag. This is required because `README.md`
of the chart will be packaged together with the chart itself and uploaded to the Helm chart repositories. So it will be
hosted outside of `Industrial-IoT` GitHub repo and because of that having relative links in it will result in invalid links.

This change is what usually comprises `helm/<version>_pub` branch, `helm/0.3.2_pub` for example.

## Publish the Helm Chart

Now we are ready to generate Helm chart package and publish it to the Helm chart repositories.

### Create a Helm Chart Package

Now we need to create the package of the Helm chart.

```bash
cd deploy/helm
helm package ./azure-industrial-iot
```

Documentation of `helm package` command can be found [here](https://helm.sh/docs/helm/helm_package/).

This will create `azure-industrial-iot-0.4.2.tgz` package. Please note that the version that is part of the `tgz` file
name comes from the value of `version` key in `Chart.yaml` file. So that part of the file name will be different with
each new release.

### Publish to `https://azureiiot.blob.core.windows.net/helm` Repository

Download `https://azureiiot.blob.core.windows.net/helm/index.yaml` file.

Now we need to create an updated `index.yaml` file with a additional entry for `0.4.2` version:

```bash
helm repo index . --url https://azureiiot.blob.core.windows.net/helm/ --merge '<path-to>/index.yaml'
```

Documentation of `helm repo index` command can be found [here](https://helm.sh/docs/helm/helm_repo_index/).

Now `azure-industrial-iot-0.4.2.tgz` and updated `index.yaml` files can be uploaded to `helm` container of
`azureiiot` storage account.

### Publish to `https://azure.github.io/Industrial-IoT/helm` Repository

This Helm chart repository is hosted on our GitHub Page. So all we need to do is add our new package in `docs/helm`
directory and update `index.yaml` file there to contain an entry for it. Once those changes are in `main` branch,
new chart will be available for consumption.

We need to create an updated `index.yaml` file with a additional entry for `0.4.2` version and replace `index.yaml`
in `docs/helm` directory with it:

```bash
helm repo index . --url https://azure.github.io/Industrial-IoT/helm/ --merge ../../docs/helm/index.yaml
rm ../../docs/helm/index.yaml
cp ./index.yaml ../../docs/helm/
```

Now let's copy new package:

```bash
cp ./azure-industrial-iot-0.4.2.tgz ../../docs/helm/
```

Now `azure-industrial-iot-0.4.2.tgz` and updated `index.yaml` files should be committed and a new PR should be created
to merge those changes into `main` branch. Once the PR is merged, our GitHub Page will be updated and new chart will
be available.

### Publish to `https://microsoft.github.io/charts/repo` Repository

This chart repository is hosted in https://github.com/microsoft/charts GitHub repository. Follow the steps outlined
there for adding a new chart.

[Here](https://github.com/microsoft/charts/pull/24) is the pull request with changes that were required for publishing
`0.3.2` version of `azure-industrial-iot` Helm chart. As you can see in the PR, one needs to put `azure-industrial-iot-0.4.2.tgz`
in `repo/azure-industrial-iot` directory and update `index.yaml` similar to how we have done it above.
