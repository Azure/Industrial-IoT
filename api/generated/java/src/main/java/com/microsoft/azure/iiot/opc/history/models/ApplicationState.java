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

import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonValue;

/**
 * Defines values for ApplicationState.
 */
public enum ApplicationState {
    /** Enum value New. */
    NEW("New"),

    /** Enum value Approved. */
    APPROVED("Approved"),

    /** Enum value Rejected. */
    REJECTED("Rejected");

    /** The actual serialized value for a ApplicationState instance. */
    private String value;

    ApplicationState(String value) {
        this.value = value;
    }

    /**
     * Parses a serialized value to a ApplicationState instance.
     *
     * @param value the serialized value to parse.
     * @return the parsed ApplicationState object, or null if unable to parse.
     */
    @JsonCreator
    public static ApplicationState fromString(String value) {
        ApplicationState[] items = ApplicationState.values();
        for (ApplicationState item : items) {
            if (item.toString().equalsIgnoreCase(value)) {
                return item;
            }
        }
        return null;
    }

    @JsonValue
    @Override
    public String toString() {
        return this.value;
    }
}
