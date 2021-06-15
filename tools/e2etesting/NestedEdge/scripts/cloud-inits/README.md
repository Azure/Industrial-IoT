# Configuration of VMs via Cloud-Init

[Cloud-init](https://cloudinit.readthedocs.io/en/latest/) is a popular way to initializes Virtual Machines (VMs). This sample leverages Cloud-Init to initialize various types of VMs used such as the jumpbox, the proxies, the simulated IoT Edge devices and the simulated IIOT assets.

The cloud-init configuration of each of these VM type is stored in this folder. These configurations are however passed to the VMs via an [Azure Resource Manager](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/overview) (ARM) template via the `CustomData` field. To update a cloud-init script in an ARM template, follow these steps:

- Format the `cloud-init` script to be used in the ARM template:

    - From a terminal, go to the `cloud-inits` directory:

    ```bash
    cd  ./scripts/cloud-inits/
    ```

    -  Run the following python code targeting the `cloud-init` of the type of VM that you want to update:

    ```bash
    python python ./cloudInitToCustomData/cloudInitToCustomData.py ./jumpbox/cloud-init.txt
    ```

    - Copy the output of the python script

- Update the ARM template with the new cloud-init script:

    - Open the ARM template of the targeted VM in the `ARM-templates` folder
    - Find the the `CustomData` field and replace it with the key and value provided by the Python script above

