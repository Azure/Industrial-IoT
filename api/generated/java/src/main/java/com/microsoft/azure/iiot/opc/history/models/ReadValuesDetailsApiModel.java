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

import org.joda.time.DateTime;
import com.fasterxml.jackson.annotation.JsonProperty;

/**
 * Read historic values.
 */
public class ReadValuesDetailsApiModel {
    /**
     * Beginning of period to read. Set to null
     * if no specific start time is specified.
     */
    @JsonProperty(value = "startTime")
    private DateTime startTime;

    /**
     * End of period to read. Set to null if no
     * specific end time is specified.
     */
    @JsonProperty(value = "endTime")
    private DateTime endTime;

    /**
     * The maximum number of values returned for any Node
     * over the time range. If only one time is specified,
     * the time range shall extend to return this number
     * of values. 0 or null indicates that there is no
     * maximum.
     */
    @JsonProperty(value = "numValues")
    private Integer numValues;

    /**
     * Whether to return the bounding values or not.
     */
    @JsonProperty(value = "returnBounds")
    private Boolean returnBounds;

    /**
     * Get beginning of period to read. Set to null
     if no specific start time is specified.
     *
     * @return the startTime value
     */
    public DateTime startTime() {
        return this.startTime;
    }

    /**
     * Set beginning of period to read. Set to null
     if no specific start time is specified.
     *
     * @param startTime the startTime value to set
     * @return the ReadValuesDetailsApiModel object itself.
     */
    public ReadValuesDetailsApiModel withStartTime(DateTime startTime) {
        this.startTime = startTime;
        return this;
    }

    /**
     * Get end of period to read. Set to null if no
     specific end time is specified.
     *
     * @return the endTime value
     */
    public DateTime endTime() {
        return this.endTime;
    }

    /**
     * Set end of period to read. Set to null if no
     specific end time is specified.
     *
     * @param endTime the endTime value to set
     * @return the ReadValuesDetailsApiModel object itself.
     */
    public ReadValuesDetailsApiModel withEndTime(DateTime endTime) {
        this.endTime = endTime;
        return this;
    }

    /**
     * Get the maximum number of values returned for any Node
     over the time range. If only one time is specified,
     the time range shall extend to return this number
     of values. 0 or null indicates that there is no
     maximum.
     *
     * @return the numValues value
     */
    public Integer numValues() {
        return this.numValues;
    }

    /**
     * Set the maximum number of values returned for any Node
     over the time range. If only one time is specified,
     the time range shall extend to return this number
     of values. 0 or null indicates that there is no
     maximum.
     *
     * @param numValues the numValues value to set
     * @return the ReadValuesDetailsApiModel object itself.
     */
    public ReadValuesDetailsApiModel withNumValues(Integer numValues) {
        this.numValues = numValues;
        return this;
    }

    /**
     * Get whether to return the bounding values or not.
     *
     * @return the returnBounds value
     */
    public Boolean returnBounds() {
        return this.returnBounds;
    }

    /**
     * Set whether to return the bounding values or not.
     *
     * @param returnBounds the returnBounds value to set
     * @return the ReadValuesDetailsApiModel object itself.
     */
    public ReadValuesDetailsApiModel withReturnBounds(Boolean returnBounds) {
        this.returnBounds = returnBounds;
        return this;
    }

}
