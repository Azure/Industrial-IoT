// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** @file       gateway.h
 *   @brief     Library that allows a user to create and configure a gateway.
 *
 *   @details   The Gateway library provides a mechanism for creating and
 *              destroying a gateway, as well as adding and removing modules.
 */

#ifndef GATEWAY_H
#define GATEWAY_H

#include "azure_c_shared_utility/macro_utils.h"
#include "azure_c_shared_utility/vector.h"
#include "module.h"
#include "module_loader.h"

#ifdef __cplusplus
extern "C"
{
#endif

#define GATEWAY_ADD_LINK_RESULT_VALUES \
    GATEWAY_ADD_LINK_SUCCESS, \
    GATEWAY_ADD_LINK_ERROR, \
    GATEWAY_ADD_LINK_INVALID_ARG

/** @brief      Enumeration describing the result of ::Gateway_AddLink.
 */
DEFINE_ENUM(GATEWAY_ADD_LINK_RESULT, GATEWAY_ADD_LINK_RESULT_VALUES);

#define GATEWAY_START_RESULT_VALUES \
    GATEWAY_START_SUCCESS, \
    GATEWAY_START_INVALID_ARGS

/** @brief      Enumeration describing the result of ::Gateway_Start.
 */
DEFINE_ENUM(GATEWAY_START_RESULT, GATEWAY_START_RESULT_VALUES);

/** @brief      Struct representing a single link for a gateway. */
typedef struct GATEWAY_LINK_ENTRY_TAG
{
    /** @brief  The name of the module which is going to send messages. */
    const char* module_source;

    /** @brief  The name of the module which is going to receive messages. */
    const char* module_sink;
} GATEWAY_LINK_ENTRY;

/** @brief      Struct representing a particular gateway. */
typedef struct GATEWAY_HANDLE_DATA_TAG* GATEWAY_HANDLE;

/** @brief      Struct representing the loader and entrypoint
 *              to be used for a specific module.
 */
typedef struct GATEWAY_MODULE_LOADER_INFO_TAG
{
    /** @brief  Pointer to the module loader to be used for
     *          loading this module.
     */
    const MODULE_LOADER* loader;

    /** @brief  Pointer to the entrypoint for this module.
     */
    void* entrypoint;
} GATEWAY_MODULE_LOADER_INFO;

/** @brief      Struct representing a single entry of the #GATEWAY_PROPERTIES.
 */
typedef struct GATEWAY_MODULES_ENTRY_TAG
{
    /** @brief  The (possibly @c NULL) name of the module */
    const char* module_name;

    /** @brief  The module loader information for this module */
    GATEWAY_MODULE_LOADER_INFO module_loader_info;

    /** @brief  The user-defined configuration object for the module */
    const void* module_configuration;
} GATEWAY_MODULES_ENTRY;

/** @brief      Struct representing the properties that should be used when
 *              creating a module; each entry of the @c VECTOR_HANDLE being a
 *              #GATEWAY_MODULES_ENTRY.
 */
typedef struct GATEWAY_PROPERTIES_DATA_TAG
{
    /** @brief  Vector of #GATEWAY_MODULES_ENTRY objects. */
    VECTOR_HANDLE gateway_modules;

    /** @brief  Vector of #GATEWAY_LINK_ENTRY objects. */
    VECTOR_HANDLE gateway_links;
} GATEWAY_PROPERTIES;

/** @brief      Creates a gateway using a JSON configuration file as input
 *              which describes each module. Each module described in the
 *              configuration must support Module_CreateFromJson.
 *
 * @param       file_path   Path to the JSON configuration file for this
 *                          gateway.
 *
 *              Sample JSON configuration file:
 *
 *              {
 *                  "modules" :
 *                  [
 *                      {
                            "name": "sensor",
                            "loader": {
                                "name": "dotnet",
                                "entrypoint": {
                                    "class.name": "Microsoft.Azure.Gateway.SensorModule",
                                    "assembly.path": "./bin/Microsoft.Azure.Gateway.Modules.dll"
                                }
                            },
                            "args" : {
                                "power.level": 5
                            }
                        },
                        {
                            "name": "logger",
                            "loader": {
                                "name": "native",
                                "entrypoint": {
                                    "module.path": "./bin/liblogger.so"
                                }
                            },
                            "args": {
                                "filename": "/var/logs/gateway-log.json"
                            }
                        }
 *                  ],
 *                  "links":
 *                  [
 *                      {
 *                          "source": "sensor",
 *                          "sink": "logger"
 *                      }
 *                  ]
 *              }
 *
 * @return      A non-NULL #GATEWAY_HANDLE that can be used to manage the
 *              gateway or @c NULL on failure.
 */
extern GATEWAY_HANDLE Gateway_CreateFromJson(const char* file_path);

/** @brief      Creates a new gateway using the provided #GATEWAY_PROPERTIES.
 *
 *  @param      properties      #GATEWAY_PROPERTIES structure containing
 *                              specific module properties and information.
 *
 *  @return     A non-NULL #GATEWAY_HANDLE that can be used to manage the
 *              gateway or @c NULL on failure.
 */
extern GATEWAY_HANDLE Gateway_Create(const GATEWAY_PROPERTIES* properties);

/** @brief      Tell the Gateway it's ready to start.
 *
 *  @param      gw      #GATEWAY_HANDLE to be destroyed.
 *
 *  @return     A #GATEWAY_START_RESULT to report the result of the start
 */
extern GATEWAY_START_RESULT Gateway_Start(GATEWAY_HANDLE gw);

/** @brief      Destroys the gateway and disposes of all associated data.
 *
 *  @param      gw      #GATEWAY_HANDLE to be destroyed.
 */
extern void Gateway_Destroy(GATEWAY_HANDLE gw);

/** @brief      Creates a new module based on the GATEWAY_MODULES_ENTRY*.
 *
 *  @param      gw      Pointer to a #GATEWAY_HANDLE to add the Module onto.
 *  @param      entry   Pointer to a #GATEWAY_MODULES_ENTRY structure
 *                      describing the module.
 *
 *  @return     A non-NULL #MODULE_HANDLE to the newly created and added
 *              Module, or @c NULL on failure.
 */
extern MODULE_HANDLE Gateway_AddModule(GATEWAY_HANDLE gw, const GATEWAY_MODULES_ENTRY* entry);

/** @brief      Tells a module that the gateway is ready for it to start.
 *
 *  @param      gw      Pointer to a #GATEWAY_HANDLE from which to remove the
 *                      Module.
 *  @param      module  Pointer to a #MODULE_HANDLE that needs to be removed.
 */
extern void Gateway_StartModule(GATEWAY_HANDLE gw, MODULE_HANDLE module);


/** @brief      Removes the provided module from the gateway and all links that
 *              involves this module.
 *
 *  @param      gw      Pointer to a #GATEWAY_HANDLE from which to remove the
 *                      Module.
 *  @param      module  Pointer to a #MODULE_HANDLE that needs to be removed.
 */
extern void Gateway_RemoveModule(GATEWAY_HANDLE gw, MODULE_HANDLE module);

/** @brief      Removes module by its unique name
 *
 *  @param      gw          Pointer to a #GATEWAY_HANDLE from which to remove
 *                          the module.
 *  @param      module_name A C string representing the name of module to be
 *                          removed
 *
 *  @return     0 on success and a non-zero value when an error occurs.
 */
extern int Gateway_RemoveModuleByName(GATEWAY_HANDLE gw, const char *module_name);

/** @brief      Adds a link to a gateway message broker.
 *
 *  @param      gw          Pointer to a #GATEWAY_HANDLE from which link is
 *                          going to be added.
 *
 *  @param      entryLink   Pointer to a #GATEWAY_LINK_ENTRY to be added.
 *
 *  @return     A GATEWAY_ADD_LINK_RESULT with the operation result.
 */
extern GATEWAY_ADD_LINK_RESULT Gateway_AddLink(GATEWAY_HANDLE gw, const GATEWAY_LINK_ENTRY* entryLink);

/** @brief      Remove a link from a gateway message broker.
 *
 *  @param      gw          Pointer to a #GATEWAY_HANDLE from which link is
 *                          going to be removed.
 *
 *  @param      entryLink   Pointer to a #GATEWAY_LINK_ENTRY to be removed.
 */
extern void Gateway_RemoveLink(GATEWAY_HANDLE gw, const GATEWAY_LINK_ENTRY* entryLink);

#ifdef __cplusplus
}
#endif

#endif // GATEWAY_H
