/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 *
 * Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
 * Changes may cause incorrect behavior and will be lost if the code is
 * regenerated.
 */

package com.microsoft.azure.iiot.opc.history.models;

import java.util.List;
import com.fasterxml.jackson.annotation.JsonProperty;

/**
 * The events to delete.
 */
public class DeleteEventsDetailsApiModel {
    /**
     * Events to delete.
     */
    @JsonProperty(value = "eventIds", required = true)
    private List<byte[]> eventIds;

    /**
     * Get events to delete.
     *
     * @return the eventIds value
     */
    public List<byte[]> eventIds() {
        return this.eventIds;
    }

    /**
     * Set events to delete.
     *
     * @param eventIds the eventIds value to set
     * @return the DeleteEventsDetailsApiModel object itself.
     */
    public DeleteEventsDetailsApiModel withEventIds(List<byte[]> eventIds) {
        this.eventIds = eventIds;
        return this;
    }

}
