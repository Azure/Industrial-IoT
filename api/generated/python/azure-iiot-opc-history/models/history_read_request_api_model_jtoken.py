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


class HistoryReadRequestApiModelJToken(Model):
    """Request node history read.

    :param node_id: Node to read from (mandatory)
    :type node_id: str
    :param browse_path: An optional path from NodeId instance to
     the actual node.
    :type browse_path: list[str]
    :param details: The HistoryReadDetailsType extension object
     encoded in json and containing the tunneled
     Historian reader request.
    :type details: object
    :param index_range: Index range to read, e.g. 1:2,0:1 for 2 slices
     out of a matrix or 0:1 for the first item in
     an array, string or bytestring.
     See 7.22 of part 4: NumericRange.
    :type index_range: str
    :param header: Optional request header
    :type header: ~azure-iiot-opc-history.models.RequestHeaderApiModel
    """

    _attribute_map = {
        'node_id': {'key': 'nodeId', 'type': 'str'},
        'browse_path': {'key': 'browsePath', 'type': '[str]'},
        'details': {'key': 'details', 'type': 'object'},
        'index_range': {'key': 'indexRange', 'type': 'str'},
        'header': {'key': 'header', 'type': 'RequestHeaderApiModel'},
    }

    def __init__(self, node_id=None, browse_path=None, details=None, index_range=None, header=None):
        super(HistoryReadRequestApiModelJToken, self).__init__()
        self.node_id = node_id
        self.browse_path = browse_path
        self.details = details
        self.index_range = index_range
        self.header = header
