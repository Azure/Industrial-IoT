* [Service configuration](#configuration-and-environment-variables)
* [Run and Debug with Visual Studio](#run-and-debug-with-visual-studio)
* [Run and Debug with IntelliJ Rider](#build-and-run-from-the-command-line)
* [Build and Run from the command line](#build-and-run-from-the-command-line)
* [Package the application to a Docker image](#package-the-application-to-a-docker-image)
* [Azure IoT Hub setup](#azure-iot-hub-setup)
* [Development setup](#development-setup)

TODO Configuration and Environment variables
=======================================

The service configuration is stored using ASP.NET Core configuration
adapters, in [appsettings.ini](WebService/appsettings.ini). The INI
format allows to store values in a readable format, with comments.
The application also supports inserting environment variables, such as
credentials and networking details.

The configuration file in the repository references some environment
variables that need to created at least once. Depending on your OS and
the IDE, there are several ways to manage environment variables:

* For Windows users, the [env-vars-setup.cmd](scripts/env-vars-setup.cmd)
  script needs to be prepared and executed just once. When executed, the
  settings will persist across terminal sessions and reboots.
* For Linux and OSX environments, the [env-vars-setup](scripts/env-vars-setup)
  script needs to be executed every time a new console is opened.
  Depending on the OS and terminal, there are ways to persist values
  globally, for more information these pages should help:
  * https://stackoverflow.com/questions/13046624/how-to-permanently-export-a-variable-in-linux
  * https://stackoverflow.com/questions/135688/setting-environment-variables-in-os-x
  * https://help.ubuntu.com/community/EnvironmentVariables
* Visual Studio: env. vars can be set also from Visual Studio, under Project
  Properties, in the left pane select "Configuration Properties" and
  "Environment", to get to a section where you can add multiple variables.
* IntelliJ Rider: env. vars can be set in each Run Configuration, similarly to
  IntelliJ IDEA (https://www.jetbrains.com/help/idea/run-debug-configuration-application.html)

Run and Debug with Visual Studio
================================

Visual Studio lets you quickly open the application without using a command
prompt, without configuring anything outside of the IDE.

Steps using Visual Studio 2017:

1. Open the solution using the `iot-opc-gds-service.sln` file.
1. When the solution is loaded, right click on the `WebService` project,
   select `Properties` and go to the `Debug` section.
1. In the same section define the environment variables required.
1. Press F5, or the Run icon. VisualStudio should open your browser showing
   the service status in JSON format.

Run and Debug with IntelliJ Rider
=================================

1. Open the solution using the `iot-opc-gds-service.sln` file.
1. When the solution is loaded, got to `Run -> Edit Configurations` and
   create a new `.NET Project` configuration.
1. In the configuration select the WebService project
1. Save the settings and run the configuration just created, from the IDE
   toolbar.
1. You should see the service bootstrap messages in IntelliJ Run window,
   with details such as the URL where the web service is running, plus
   the service logs.

Build and Run from the command line
===================================

The [scripts](scripts) folder contains some scripts for frequent tasks:

* `build`: compile all the projects and run the tests.
* `compile`: compile all the projects.
* `run`: compile the projects and run the service. This will prompt for
  elevated privileges in Windows to run the web service.

The scripts check for the environment variables setup. You can set the
environment variables globally in your OS, or use the "env-vars-setup"
script in the scripts folder.

### Sandbox

The scripts assume that you configured your development environment,
with .NET Core and Docker. You can avoid installing .NET Core,
and install only Docker, and use the command line parameter `--in-sandbox`
(or the short form `-s`), for example:

* `build --in-sandbox`: executes the build task inside of a Docker
    container (short form `build -s`).
* `compile --in-sandbox`: executes the compilation task inside of a Docker
    container (short form `compile -s`).
* `run --in-sandbox`: starts the service inside of a Docker container
    (short form `run -s`).

The Docker images used for the sandbox is hosted on Docker Hub
[here](https://hub.docker.com/r/azureiotpcs/code-builder-dotnet).

Package the application to a Docker image
=========================================

The `scripts` folder includes a [docker](scripts/docker) subfolder with the
files required to package the service into a Docker image:

* `Dockerfile`: docker images specifications
* `build`: build a Docker container and store the image in the local registry
* `run`: run the Docker container from the image stored in the local registry
* `content`: a folder with files copied into the image, including the entry
  point script

Azure IoT Hub setup
===================

To use the microservice you will need to setup your Azure IoT Hub,
for development and integration tests.

The project includes some Bash scripts to help you with this setup:

* Create new IoT Hub: `./scripts/iothub/create-hub.sh`
* List existing hubs: `./scripts/iothub/list-hubs.sh`
* Show IoT Hub details (e.g. keys): `./scripts/iothub/show-hub.sh`

and in case you had multiple Azure subscriptions:

* Show subscriptions list: `./scripts/iothub/list-subscriptions.sh`
* Change current subscription: `./scripts/iothub/select-subscription.sh`

Development setup
=================

## .NET setup

The project workflow is managed via [.NET Core](https://dotnet.github.io)
1.x, which you need to install in your environment, so that you can run
all the scripts and ensure that your IDE works as expected.

We also provide a
[Java version](https://github.com/Azure/iot-opc-gds-service-dotnet)
of this project and other Azure IoT PCS components.

## IDE

Here are some of the IDEs that you can use to work on Azure IoT PCS:

* [Visual Studio](https://www.visualstudio.com)
* [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac)
* [IntelliJ Rider](https://www.jetbrains.com/rider)
* [Visual Studio Code](https://code.visualstudio.com)

## Git setup

The project includes a Git hook, to automate some checks before accepting a
code change. You can run the tests manually, or let the CI platform to run
the tests. We use the following Git hook to automatically run all the tests
before sending code changes to GitHub and speed up the development workflow.

If at any point you want to remove the hook, simply delete the file installed
under `.git/hooks`. You can also bypass the pre-commit hook using the
`--no-verify` option.

#### Pre-commit hook with sandbox

To setup the included hooks, open a Windows/Linux/MacOS console and execute:

```
cd PROJECT-FOLDER
cd scripts/git
setup --with-sandbox
```

With this configuration, when checking in files, git will verify that the
application passes all the tests, running the build and the tests inside
a Docker container configured with all the development requirements.

#### Pre-commit hook without sandbox

Note: the hook without sandbox requires [.NET Core](https://dotnet.github.io)
in the system PATH.

To setup the included hooks, open a Windows/Linux/MacOS console and execute:

```
cd PROJECT-FOLDER
cd scripts/git
setup --no-sandbox
```

With this configuration, when checking in files, git will verify that the
application passes all the tests, running the build and the tests in your
workstation, using the tools installed in your OS.

Some quick notes about the project code style:

1. Where reasonable, lines length is limited to 80 chars max, to help code
   reviews and command line editors.
2. Code blocks indentation with 4 spaces. The tab char should be avoided.
3. Text files use Unix end of line format (LF).
4. Dependency Injection is managed with [Autofac](https://autofac.org).
5. Web service APIs fields are CamelCased (except for metadata).
