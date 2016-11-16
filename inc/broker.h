// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** @file        broker.h
*    @brief        Library for configuring and using the gateway's message broker.
*
*    @details    This is the API to create a reference counted and thread safe 
*                gateway message broker. The broker sends messages to other
*                modules. Messages published to the broker have a bag of
*               properties (name, value) and an opaque array of bytes that
*               constitute the message content.
*/

#ifndef BROKER_H
#define BROKER_H

/** @brief Struct representing a message broker. */
typedef struct BROKER_HANDLE_DATA_TAG* BROKER_HANDLE;

#include "azure_c_shared_utility/macro_utils.h"
#include "message.h"
#include "module.h"

#ifdef __cplusplus
#include <cstddef>
extern "C"
{
#else
#include <stddef.h>
#endif

/** @brief    Link Data with #MODULE_HANDLE for source and sink. 
*/
typedef struct BROKER_LINK_DATA_TAG {
    /** @brief    #MODULE_HANDLE representing the module generating/publishing messages. 
    */
    MODULE_HANDLE module_source_handle;
    /** @brief    #MODULE_HANDLE representing the module receiving messages. 
    */
    MODULE_HANDLE module_sink_handle;
} BROKER_LINK_DATA;

#define BROKER_RESULT_VALUES \
    BROKER_OK, \
    BROKER_ERROR, \
    BROKER_ADD_LINK_ERROR, \
    BROKER_REMOVE_LINK_ERROR, \
    BROKER_INVALIDARG

/** @brief    Enumeration describing the result of ::Broker_Publish, 
*            ::Broker_AddModule, ::Broker_AddLink, and ::Broker_RemoveModule.
*/
DEFINE_ENUM(BROKER_RESULT, BROKER_RESULT_VALUES);

/** @brief        Creates a new message broker.
*   
*    @return        A valid #BROKER_HANDLE upon success, or @c NULL upon failure.
*/
extern BROKER_HANDLE Broker_Create(void);

/** @brief        Increments the reference count of a message broker.
*
*    @details    This function will simply increment the internal reference
*                count of the provided #BROKER_HANDLE.
*
*    @param        broker  The #BROKER_HANDLE to be cloned.
*/
extern void Broker_IncRef(BROKER_HANDLE broker);

/** @brief        Decrements the reference count of a message broker.
*
*    @details    This function will simply decrement the internal reference
*                count of the provided #BROKER_HANDLE, destroying the message
*                broker when the reference count reaches 0.
*
*    @param        broker  The #BROKER_HANDLE whose ref count will be decremented.
*/
extern void Broker_DecRef(BROKER_HANDLE broker);

/** @brief        Publishes a message to the message broker.
*
*    @details    For details about threading with regard to the message broker
*                and modules connected to it, see
*                <a href="https://github.com/Azure/azure-iot-gateway-sdk/blob/develop/core/devdoc/broker_hld.md">Broker High Level Design Documentation</a>.
*
*    @param        broker    The #BROKER_HANDLE onto which the message will be
*                        published.
*    @param        source    The #MODULE_HANDLE from which the message will be
*                        published. The broker will not publish the message to this
*                       module. (optional, may be NULL)
*    @param        message    The #MESSAGE_HANDLE representing the message to be
*                        published.
*
*    @return        A #BROKER_RESULT describing the result of the function.
*/
extern BROKER_RESULT Broker_Publish(BROKER_HANDLE broker, MODULE_HANDLE source, MESSAGE_HANDLE message);

/** @brief        Adds a module to the message broker.
*
*    @details    For details about threading with regard to the message broker
*                and modules connected to it, see 
*                <a href="https://github.com/Azure/azure-iot-gateway-sdk/blob/develop/core/devdoc/broker_hld.md">Broker High Level Design Documentation</a>.
*
*    @param        broker          The #BROKER_HANDLE onto which the module will be 
*                                added.
*    @param        module            The #MODULE for the module that will be added 
*                                to this message broker.
*
*    @return        A #BROKER_RESULT describing the result of the function.
*/
extern BROKER_RESULT Broker_AddModule(BROKER_HANDLE broker, const MODULE* module);

/** @brief        Removes a module from the message broker.
*   
*    @param        broker    The #BROKER_HANDLE from which the module will be removed.
*    @param        module    The #MODULE of the module to be removed.
*   
*    @return        A #BROKER_RESULT describing the result of the function.
*/
extern BROKER_RESULT Broker_RemoveModule(BROKER_HANDLE broker, const MODULE* module);

/** @brief        Adds a route to the message broker.
*
*    @details    For details about threading with regard to the message broker
*                and modules connected to it, see
*                <a href="https://github.com/Azure/azure-iot-gateway-sdk/blob/develop/core/devdoc/broker_hld.md">Broker High Level Design Documentation</a>.
*
*    @param        broker          The #BROKER_HANDLE onto which the module will be
*                                added.
*    @param        link            The #BROKER_LINK_DATA for the link that will be added
*                                to this message broker.
*
*    @return        A #BROKER_RESULT describing the result of the function.
*/
extern BROKER_RESULT Broker_AddLink(BROKER_HANDLE broker, const BROKER_LINK_DATA* link);

/** @brief        Removes a route from the message broker.
*
*    @param        broker    The #BROKER_HANDLE from which the link will be removed.
*    @param        link    The #BROKER_LINK_DATA of the link to be removed.
*
*    @return        A #BROKER_RESULT describing the result of the function.
*/
extern BROKER_RESULT Broker_RemoveLink(BROKER_HANDLE broker, const BROKER_LINK_DATA* link);

/** @brief      Disposes of resources allocated by a message broker.
*
*    @param      broker  The #BROKER_HANDLE to be destroyed.
*/
extern void Broker_Destroy(BROKER_HANDLE broker);

#ifdef __cplusplus
}
#endif


#endif /*BROKER_H*/
