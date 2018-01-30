Web service
===========

## ASP.NET Web API and Kestrel

The web service is built on ASP.NET Web API and hosted via Kestrel, i.e. IIS is
not strictly required to run the service, although it would be possible if required.
More information can be found here:

* [Building Web APIs](https://docs.microsoft.com/en-us/aspnet/core/mvc/web-api)
* [Routing in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing)
* [Introduction to Kestrel web server implementation in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel)

## Guidelines

The web service is the microservice entry point. There might be other
entry points if the microservice has some background agent, for instance to run
continuous tasks like log aggregation, simulations, watchdogs etc.

The web service takes care of loading the configuration, and injecting it to
underlying dependencies, like the service layer. Most of the business logic
is encapsulated in the service layer, while the web service has the
responsibility of accepting requests and providing responses in the correct
format.

## Conventions

* Web service routing is defined by convention, e.g. the name of the controllers
  defines the supported paths.
* The microservice configuration is defined in the `appsettings.json` file
  stored in the `WebService` project
