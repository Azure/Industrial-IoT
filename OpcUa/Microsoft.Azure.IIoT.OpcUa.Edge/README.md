Edge service
===========

## IoT Edge Hub

The edge service is built using the IoT Hub Edge Device sdk.
More information can be found here:

<todo>

## Guidelines

The edge service is the edge side service entry point. The edge service uses
the publisher module to publish streams received from a server.

Like the web service layer, the edge service takes care of loading the 
configuration, and injecting it to underlying dependencies, like the 
service layer. 

