::  Prepare the environment variables used by the application.
::
::  For more information about finding IoT Hub settings, more information here:
::
::  * https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal#endpoints
::  * https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted
::

:: see: Shared access policies => key name => Connection string
SETX PCS_IOTHUB_CONNSTRING "..."

SETX PCS_IOTHUBMANAGER_WEBSERVICE_URL "..."

:: The OpenId tokens issuer URL, e.g. https://sts.windows.net/12000000-3400-5600-0000-780000000000/
SETX PCS_AUTH_ISSUER "{enter the token issuer URL here}"

:: The intended audience of the tokens, e.g. your Client Id
SETX PCS_AUTH_AUDIENCE "{enter the tokens audience here}"
