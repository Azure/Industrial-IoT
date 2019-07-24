/*
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 *
 * Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
 * Changes may cause incorrect behavior and will be lost if the code is
 * regenerated.
 */

'use strict';

/**
 * Request node history update
 *
 */
class HistoryUpdateRequestApiModelDeleteValuesAtTimesDetailsApiModel {
  /**
   * Create a HistoryUpdateRequestApiModelDeleteValuesAtTimesDetailsApiModel.
   * @property {string} [nodeId] Node to update
   * @property {array} [browsePath] An optional path from NodeId instance to
   * the actual node.
   * @property {object} details The HistoryUpdateDetailsType extension object
   * encoded as json Variant and containing the tunneled
   * update request for the Historian server. The value
   * is updated at edge using above node address.
   * @property {array} [details.reqTimes] The timestamps to delete
   * @property {object} [header] Optional request header
   * @property {object} [header.elevation] Optional User elevation
   * @property {string} [header.elevation.type] Type of credential. Possible
   * values include: 'None', 'UserName', 'X509Certificate', 'JwtToken'
   * @property {object} [header.elevation.value] Value to pass to server
   * @property {array} [header.locales] Optional list of locales in preference
   * order.
   * @property {object} [header.diagnostics] Optional diagnostics configuration
   * @property {string} [header.diagnostics.level] Requested level of response
   * diagnostics.
   * (default: Status). Possible values include: 'None', 'Status',
   * 'Operations', 'Diagnostics', 'Verbose'
   * @property {string} [header.diagnostics.auditId] Client audit log entry.
   * (default: client generated)
   * @property {date} [header.diagnostics.timeStamp] Timestamp of request.
   * (default: client generated)
   */
  constructor() {
  }

  /**
   * Defines the metadata of HistoryUpdateRequestApiModelDeleteValuesAtTimesDetailsApiModel
   *
   * @returns {object} metadata of HistoryUpdateRequestApiModelDeleteValuesAtTimesDetailsApiModel
   *
   */
  mapper() {
    return {
      required: false,
      serializedName: 'HistoryUpdateRequestApiModel_DeleteValuesAtTimesDetailsApiModel_',
      type: {
        name: 'Composite',
        className: 'HistoryUpdateRequestApiModelDeleteValuesAtTimesDetailsApiModel',
        modelProperties: {
          nodeId: {
            required: false,
            serializedName: 'nodeId',
            type: {
              name: 'String'
            }
          },
          browsePath: {
            required: false,
            serializedName: 'browsePath',
            type: {
              name: 'Sequence',
              element: {
                  required: false,
                  serializedName: 'StringElementType',
                  type: {
                    name: 'String'
                  }
              }
            }
          },
          details: {
            required: true,
            serializedName: 'details',
            type: {
              name: 'Composite',
              className: 'DeleteValuesAtTimesDetailsApiModel'
            }
          },
          header: {
            required: false,
            serializedName: 'header',
            type: {
              name: 'Composite',
              className: 'RequestHeaderApiModel'
            }
          }
        }
      }
    };
  }
}

module.exports = HistoryUpdateRequestApiModelDeleteValuesAtTimesDetailsApiModel;
