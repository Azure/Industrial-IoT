# coding=utf-8
# --------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for
# license information.
#
# Code generated by Microsoft (R) AutoRest Code Generator 2.3.33.0
# Changes may cause incorrect behavior and will be lost if the code is
# regenerated.
# --------------------------------------------------------------------------

from msrest.serialization import Model


class InsertValuesDetailsApiModel(Model):
    """Insert historic data.

    :param values: Values to insert
    :type values: list[~azure-iiot-opc-history.models.HistoricValueApiModel]
    """

    _validation = {
        'values': {'required': True},
    }

    _attribute_map = {
        'values': {'key': 'values', 'type': '[HistoricValueApiModel]'},
    }

    def __init__(self, values):
        super(InsertValuesDetailsApiModel, self).__init__()
        self.values = values
