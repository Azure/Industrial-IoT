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

import java.util.Map;
import com.fasterxml.jackson.annotation.JsonProperty;

/**
 * Status response model.
 */
public class StatusResponseApiModel {
    /**
     * Name of this service.
     */
    @JsonProperty(value = "name")
    private String name;

    /**
     * Operational status.
     */
    @JsonProperty(value = "status")
    private String status;

    /**
     * Current time.
     */
    @JsonProperty(value = "currentTime", access = JsonProperty.Access.WRITE_ONLY)
    private String currentTime;

    /**
     * Start time of service.
     */
    @JsonProperty(value = "startTime", access = JsonProperty.Access.WRITE_ONLY)
    private String startTime;

    /**
     * Up time of service.
     */
    @JsonProperty(value = "upTime", access = JsonProperty.Access.WRITE_ONLY)
    private Long upTime;

    /**
     * Value generated at bootstrap by each instance of the service and
     * used to correlate logs coming from the same instance. The value
     * changes every time the service starts.
     */
    @JsonProperty(value = "uid", access = JsonProperty.Access.WRITE_ONLY)
    private String uid;

    /**
     * A property bag with details about the service.
     */
    @JsonProperty(value = "properties", access = JsonProperty.Access.WRITE_ONLY)
    private Map<String, String> properties;

    /**
     * A property bag with details about the internal dependencies.
     */
    @JsonProperty(value = "dependencies", access = JsonProperty.Access.WRITE_ONLY)
    private Map<String, String> dependencies;

    /**
     * Optional meta data.
     */
    @JsonProperty(value = "$metadata", access = JsonProperty.Access.WRITE_ONLY)
    private Map<String, String> metadata;

    /**
     * Get name of this service.
     *
     * @return the name value
     */
    public String name() {
        return this.name;
    }

    /**
     * Set name of this service.
     *
     * @param name the name value to set
     * @return the StatusResponseApiModel object itself.
     */
    public StatusResponseApiModel withName(String name) {
        this.name = name;
        return this;
    }

    /**
     * Get operational status.
     *
     * @return the status value
     */
    public String status() {
        return this.status;
    }

    /**
     * Set operational status.
     *
     * @param status the status value to set
     * @return the StatusResponseApiModel object itself.
     */
    public StatusResponseApiModel withStatus(String status) {
        this.status = status;
        return this;
    }

    /**
     * Get current time.
     *
     * @return the currentTime value
     */
    public String currentTime() {
        return this.currentTime;
    }

    /**
     * Get start time of service.
     *
     * @return the startTime value
     */
    public String startTime() {
        return this.startTime;
    }

    /**
     * Get up time of service.
     *
     * @return the upTime value
     */
    public Long upTime() {
        return this.upTime;
    }

    /**
     * Get value generated at bootstrap by each instance of the service and
     used to correlate logs coming from the same instance. The value
     changes every time the service starts.
     *
     * @return the uid value
     */
    public String uid() {
        return this.uid;
    }

    /**
     * Get a property bag with details about the service.
     *
     * @return the properties value
     */
    public Map<String, String> properties() {
        return this.properties;
    }

    /**
     * Get a property bag with details about the internal dependencies.
     *
     * @return the dependencies value
     */
    public Map<String, String> dependencies() {
        return this.dependencies;
    }

    /**
     * Get optional meta data.
     *
     * @return the metadata value
     */
    public Map<String, String> metadata() {
        return this.metadata;
    }

}
