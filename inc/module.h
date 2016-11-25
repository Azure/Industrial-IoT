// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** @file       module.h
 *  @brief      Interface for modules which communicate with other modules via
 *              a message broker.
 *
 *  @details    Every module associated with the message broker must implement
 *              this interface.
 *              A module can only belong to one message broker; a message broker
 *              may have many modules associated with it.
 *
 *              Every module library exports a function (Module_GetApi) that
 *              returns a pointer to the #MODULE_API structure.
 */

#ifndef MODULE_H
#define MODULE_H

/** @brief  Encapsulates a module's handle and a cached copy of its interface.
 */
typedef struct MODULE_TAG MODULE;

/** @brief  Represents a module instance. */
typedef void* MODULE_HANDLE;

/** @brief  Provides access to a module's interface. */
typedef struct MODULE_API_TAG MODULE_API;

#include "azure_c_shared_utility/macro_utils.h"
#include "broker.h"
#include "message.h"


#ifdef __cplusplus
extern "C"
{
#endif

    /** @brief  Structure used to represent/abstract the idea of a module.  May
     *          contain Handle/FxnPtrs or an interface ptr or some unforseen
     *          representation.
     */
    struct MODULE_TAG
    {
        /** @brief  Struct containing function pointers */
        const MODULE_API* module_apis;
        /** @brief  HANDLE for module. */
        MODULE_HANDLE module_handle;
    };

    /** @brief      Translates module configuration from a JSON string to a
     *              module specific data structure.
     *
     *  @details    This function must be implemented by modules that support
     *              configuration options.
     *
     *  @param      configuration   A JSON string which describes any needed
     *                              configuration to create this module.
     *
     *  @return     A void pointer containing a parsed representation of the
     *              module's configuration.
     */
    typedef void*(*pfModule_ParseConfigurationFromJson)(const char* configuration);

    /** @brief      Frees the configuration object returned by the
     *              ParseConfigurationFromJson function.
     *
     *  @details    This function must be implemented by modules that support
     *              configuration options.
     *
     *  @param      configuration   A void pointer containing a parsed representation
     *                              of the module's configuration.
     */
    typedef void(*pfModule_FreeConfiguration)(void* configuration);

    /** @brief      Creates an instance of a module.
     *
     *  @details    This function must be implemented.
     *
     *  @param      broker          The #BROKER_HANDLE to which this module
     *                              will publish messages.
     *  @param      configuration   A pointer to the user-defined configuration 
     *                              structure for this module.
     *
     *  @return     A non-NULL #MODULE_HANDLE upon success, otherwise @c NULL.
     */
    typedef MODULE_HANDLE(*pfModule_Create)(BROKER_HANDLE broker, const void* configuration);

    /** @brief      Disposes of the resources managed by this module.
     *
     *  @details    This function must be implemented.
     *
     *  @param      moduleHandle    The #MODULE_HANDLE of the module to be
     *                              destroyed.
     */
    typedef void(*pfModule_Destroy)(MODULE_HANDLE moduleHandle);

    /** @brief      Receives a message from the broker.
     *
     *  @details    This function must be implemented.
     *
     *  @param      moduleHandle    The #MODULE_HANDLE of the module receiving
     *                              the message.
     *  @param      messageHandle   The #MESSAGE_HANDLE of the message being
     *                              sent to the module.
     */
    typedef void(*pfModule_Receive)(MODULE_HANDLE moduleHandle, MESSAGE_HANDLE messageHandle);

    /** @brief      Signals to the module that the broker is ready to send and
     *              receive messages.
     *
     *  @details    This function is optional, but recommended. Messaging in
     *              the gateway is more predictable when modules delay
     *              publishing until after this function is called.
     *
     *  @param      moduleHandle    The target module's #MODULE_HANDLE.
     */
    typedef void(*pfModule_Start)(MODULE_HANDLE moduleHandle);

    /** @brief  Module API version. */
    typedef enum MODULE_API_VERSION_TAG
    {
        MODULE_API_VERSION_1
    } MODULE_API_VERSION;

    /** @brief  Current gateway module API version */
    static const MODULE_API_VERSION Module_ApiGatewayVersion = MODULE_API_VERSION_1;

    /** @brief  Structure returned by ::Module_GetApi containing the API
     *          version. By convention, the module returns a compound structure 
     *          containing this structure and a function table.  The version 
     *          determines the function table attached to the structure.
     */
    struct MODULE_API_TAG
    {
        /** @brief  The version of the MODULES_API, shall always be the first
         *          element of this structure
         */
        MODULE_API_VERSION version;
    };

    /** @brief  The module interface, version 1. An instance of this struct
     *          contains function pointers to module-specific implementations of
     *          the interface.
     */
    typedef struct MODULE_API_1_TAG
    {
        /** @brief  Always the first element on a Module's API*/
        MODULE_API base;

        /** @brief  Function pointer to the #Module_ParseConfigurationFromJson
         *          function. */
        pfModule_ParseConfigurationFromJson Module_ParseConfigurationFromJson;

        /** @brief  Function pointer to the #Module_FreeConfiguration
         *          function. */
        pfModule_FreeConfiguration Module_FreeConfiguration;

        /** @brief  Function pointer to the #Module_Create function. */
        pfModule_Create Module_Create;

        /** @brief  Function pointer to the #Module_Destroy function. */
        pfModule_Destroy Module_Destroy;

        /** @brief  Function pointer to the #Module_Receive function. */
        pfModule_Receive Module_Receive;

        /** @brief  Function pointer to the #Module_Start function (optional).
         */
        pfModule_Start Module_Start;
    } MODULE_API_1;

    /** @brief  This is the only function exported by a module. Using the
     *          exported function, the caller learns the functions for the 
     *          particular module.
     *  @param  gateway_api_version     The current API version of the gateway.
     *
     *  @return NULL in failure, MODULE_API* pointer on success.
     */
    typedef const MODULE_API* (*pfModule_GetApi)(MODULE_API_VERSION gateway_api_version);

    /** @brief  Returns the module APIS name.*/
#define MODULE_GETAPI_NAME ("Module_GetApi")

#ifdef _WIN32
#define MODULE_EXPORT __declspec(dllexport)
#else
#define MODULE_EXPORT
#endif // _WIN32

/** @brief      Returns a unique name for pfModule_GetApi that is used when
 *              the module is statically linked.
 */
#define MODULE_STATIC_GETAPI(MODULE_NAME) C2(Module_GetApi_, MODULE_NAME)

/** @brief      This is the only function exported by a module under a "by
 *              convention" name. Using the exported function, the caller learns
 *              the functions for the particular module.
 */
MODULE_EXPORT const MODULE_API* Module_GetApi(MODULE_API_VERSION gateway_api_version);

#ifdef __cplusplus
}
#endif

#endif // MODULE_H
