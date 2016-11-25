// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** @file       module_loader.h
 *  @brief      Definition of the functions and structures that must be
 *              implemented by a gateway module loader.
 *
 *  @details    A gateway module loader library must make available to the
 *              gateway application (e.g. via a function) an instance of the
 *              #MODULE_LOADER_API structure. This structure must contain valid
 *              pointers to functions that can be invoked by the gateway to load
 *              and unload a gateway module, and get its interface.
 */

#ifndef MODULE_LOADER_H
#define MODULE_LOADER_H

#include "azure_c_shared_utility/macro_utils.h"
#include "azure_c_shared_utility/umock_c_prod.h"

#include "module.h"
#include "parson.h"

#ifdef __cplusplus
extern "C"
{
#endif

/** @brief handle for a module library */
typedef void* MODULE_LIBRARY_HANDLE;

struct MODULE_LOADER_TAG;

/**
 * Configuration that is common to all module loaders goes here. The expectation
 * is that a given module loader will define its own configuration struct but
 * always have an instance of this struct as the first field.
 */
typedef struct MODULE_LOADER_BASE_CONFIGURATION_TAG
{
    /** @brief  The path to the native binding module. 
     */
    STRING_HANDLE binding_path;
} MODULE_LOADER_BASE_CONFIGURATION;

typedef MODULE_LIBRARY_HANDLE(*pfModuleLoader_Load)(const struct MODULE_LOADER_TAG* loader, const void* entrypoint);
typedef void(*pfModuleLoader_Unload)(const struct MODULE_LOADER_TAG* loader, MODULE_LIBRARY_HANDLE handle);
typedef const MODULE_API*(*pfModuleLoader_GetApi)(const struct MODULE_LOADER_TAG* loader, MODULE_LIBRARY_HANDLE handle);

typedef void*(*pfModuleLoader_ParseEntrypointFromJson)(const struct MODULE_LOADER_TAG* loader, const JSON_Value* json);
typedef void(*pfModuleLoader_FreeEntrypoint)(const struct MODULE_LOADER_TAG* loader, void* entrypoint);

typedef MODULE_LOADER_BASE_CONFIGURATION*(*pfModuleLoader_ParseConfigurationFromJson)(const struct MODULE_LOADER_TAG* loader, const JSON_Value* json);
typedef void(*pfModuleLoader_FreeConfiguration)(const struct MODULE_LOADER_TAG* loader, MODULE_LOADER_BASE_CONFIGURATION* configuration);

typedef void*(*pfModuleLoader_BuildModuleConfiguration)(const struct MODULE_LOADER_TAG* loader, const void* entrypoint, const void* module_configuration);
typedef void(*pfModuleLoader_FreeModuleConfiguration)(const struct MODULE_LOADER_TAG* loader, const void* module_configuration);

/** @brief  Function table for loading modules into a gateway */
typedef struct MODULE_LOADER_API_TAG
{
    /** @brief  Load function, loads module for gateway, returns a valid handle
     *          on success */
    pfModuleLoader_Load Load;

    /** @brief Unload function, unloads the library from the gateway */
    pfModuleLoader_Unload Unload;

    /** @brief GetApi function, gets the MODULE_API for the loaded module */
    pfModuleLoader_GetApi GetApi;

    /** @brief ParseEntrypointFromJson function, parses entrypoint information
    provided in JSON form and returns a pointer to a struct that the module will
    be able to use */
    pfModuleLoader_ParseEntrypointFromJson ParseEntrypointFromJson;
    /** @brief FreeEntrypoint function, frees the memory returned by
    ParseEntryPoint */
    pfModuleLoader_FreeEntrypoint FreeEntrypoint;

    /** @brief ParseConfigurationFromJson function, parses module loader
    configuration information provided in JSON form and returns a pointer to a
    struct that the module loader will be able to use */
    pfModuleLoader_ParseConfigurationFromJson ParseConfigurationFromJson;
    /** @brief FreeEntrypoint function, frees the memory returned by
    ParseConfigurationFromJson */
    pfModuleLoader_FreeConfiguration FreeConfiguration;

    /** @brief ParseConfigurationFromJson function, responsible for merging
    entrypoint data and data from module configuration into a struct that
    language binding modules expect*/
    pfModuleLoader_BuildModuleConfiguration BuildModuleConfiguration;
    /** @brief FreeEntrypoint function, frees the memory returned by
    BuildModuleConfiguration */
    pfModuleLoader_FreeModuleConfiguration FreeModuleConfiguration;
} MODULE_LOADER_API;

/**
 * @brief Module loader type.
 */

#define MODULE_LOADER_TYPE_VALUES \
    UNKNOWN,    \
    NATIVE,     \
    JAVA,       \
    DOTNET,     \
    NODEJS     

/**
 * @brief Enumeration listing all supported module loaders
 */
DEFINE_ENUM(MODULE_LOADER_TYPE, MODULE_LOADER_TYPE_VALUES);


/**
 * The Module Loader.
 */
typedef struct MODULE_LOADER_TAG
{
    /** @brief The module loader type from the MODULE_LOADER_TYPE enumeration. */
    MODULE_LOADER_TYPE                  type;

    /** @brief The module loader's name used to reference this loader when
     *         using JSON to configure a gateway. */
    const char*                         name;

    /** @brief The module loader's' configuration. For example the Java
     *         language binding loader might store JVM runtime options here. */
    MODULE_LOADER_BASE_CONFIGURATION*   configuration;

    /** @brief The module loader's' API implementation. */
    MODULE_LOADER_API*                  api;
} MODULE_LOADER;

#define MODULE_LOADER_RESULT_VALUES \
    MODULE_LOADER_SUCCESS, \
    MODULE_LOADER_ERROR

/**
 * @brief   Enumeration describing the result of module loader APIs
 */
DEFINE_ENUM(MODULE_LOADER_RESULT, MODULE_LOADER_RESULT_VALUES);

/**
 * @brief Utility function for parsing a JSON object that has a property called
 *       "binding.path" into a MODULE_LOADER_BASE_CONFIGURATION instance.
 */
MOCKABLE_FUNCTION(, MODULE_LOADER_RESULT, ModuleLoader_ParseBaseConfigurationFromJson,
    MODULE_LOADER_BASE_CONFIGURATION*, configuration,
    const JSON_Value*, json
);

/**
 * @brief Utility function for freeing a MODULE_LOADER_BASE_CONFIGURATION*
 *        instance from a previous call to
 *        ModuleLoader_ParseBaseConfigurationFromJson.
 */
MOCKABLE_FUNCTION(, void, ModuleLoader_FreeBaseConfiguration, MODULE_LOADER_BASE_CONFIGURATION*, configuration);

/**
 * @brief This function creates the default set of module loaders that the
 *        gateway supports.
 */
MOCKABLE_FUNCTION(, MODULE_LOADER_RESULT, ModuleLoader_Initialize);

/**
 * @brief This function frees resources allocated for tracking module loaders.
 */
MOCKABLE_FUNCTION(, void, ModuleLoader_Destroy);

/**
 * @brief Adds a new module loader to the gateway's collection of module loaders.
 */
MOCKABLE_FUNCTION(, MODULE_LOADER_RESULT, ModuleLoader_Add, const MODULE_LOADER*, loader);

/**
 * @brief Replaces the module loader configuration for the given loader in a
 *        thread-safe manner.
 */
MOCKABLE_FUNCTION(, MODULE_LOADER_RESULT, ModuleLoader_UpdateConfiguration,
    MODULE_LOADER*, loader,
    MODULE_LOADER_BASE_CONFIGURATION*, configuration
);

/**
 * @brief Searches the module loader collection given the loader's name.
 */
MOCKABLE_FUNCTION(, MODULE_LOADER*, ModuleLoader_FindByName, const char*, name);

/**
 * @brief Given a module loader type, returns the default loader.
 */
MOCKABLE_FUNCTION(, MODULE_LOADER*, ModuleLoader_GetDefaultLoaderForType, MODULE_LOADER_TYPE, type);

/**
 * @brief Given a string representation of a module loader type, returns the
 * corresponding enum value.
 */
MOCKABLE_FUNCTION(, MODULE_LOADER_TYPE, ModuleLoader_ParseType, const char*, type);

/**
 * @brief Given a module name, determines if it is a default module loader or a
 *        custom one.
 */
MOCKABLE_FUNCTION(, bool, ModuleLoader_IsDefaultLoader, const char*, name);

/**
 * @brief Updates the global loaders array from a JSON that looks like this:
 *
 *   "loaders": [
 *       {
 *           "type": "node",
 *           "name": "node",
 *           "configuration": {
 *               "binding.path": "./bin/libnode_binding.so"
 *           }
 *       },
 *       {
 *           "type": "java",
 *           "name": "java",
 *           "configuration": {
 *               "jvm.options": {
 *                   "memory": 1073741824
 *               },
 *               "gateway.class.path": "./bin/gateway.jar",
 *               "binding.path": "./bin/libjava_binding.so"
 *           }
 *       },
 *       {
 *           "type": "dotnet",
 *           "name": "dotnet",
 *           "configuration": {
 *               "binding.path": "./bin/libdotnet_binding.so"
 *           }
 *       }
 *   ]
 */
MOCKABLE_FUNCTION(, MODULE_LOADER_RESULT, ModuleLoader_InitializeFromJson, const JSON_Value*, loaders);

#ifdef __cplusplus
}
#endif

#endif // MODULE_LOADER_H
