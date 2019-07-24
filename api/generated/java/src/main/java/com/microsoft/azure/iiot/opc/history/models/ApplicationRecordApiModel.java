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

import com.fasterxml.jackson.annotation.JsonProperty;

/**
 * Application with optional list of endpoints.
 */
public class ApplicationRecordApiModel {
    /**
     * Record id.
     */
    @JsonProperty(value = "recordId", required = true)
    private int recordId;

    /**
     * Application information.
     */
    @JsonProperty(value = "application", required = true)
    private ApplicationInfoApiModel application;

    /**
     * Get record id.
     *
     * @return the recordId value
     */
    public int recordId() {
        return this.recordId;
    }

    /**
     * Set record id.
     *
     * @param recordId the recordId value to set
     * @return the ApplicationRecordApiModel object itself.
     */
    public ApplicationRecordApiModel withRecordId(int recordId) {
        this.recordId = recordId;
        return this;
    }

    /**
     * Get application information.
     *
     * @return the application value
     */
    public ApplicationInfoApiModel application() {
        return this.application;
    }

    /**
     * Set application information.
     *
     * @param application the application value to set
     * @return the ApplicationRecordApiModel object itself.
     */
    public ApplicationRecordApiModel withApplication(ApplicationInfoApiModel application) {
        this.application = application;
        return this;
    }

}
