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


class EndpointApiModel(Model):
    """Endpoint model.

    :param url: Endpoint url to use to connect with
    :type url: str
    :param alternative_urls: Alternative endpoint urls that can be used for
     accessing and validating the server
    :type alternative_urls: list[str]
    :param user: User Authentication
    :type user: ~azure-iiot-opc-history.models.CredentialApiModel
    :param security_mode: Security Mode to use for communication
     default to best. Possible values include: 'Best', 'Sign',
     'SignAndEncrypt', 'None'. Default value: "Best" .
    :type security_mode: str or ~azure-iiot-opc-history.models.SecurityMode
    :param security_policy: Security policy uri to use for communication
     default to best.
    :type security_policy: str
    :param certificate: Endpoint certificate that was registered.
    :type certificate: bytearray
    """

    _validation = {
        'url': {'required': True},
        'alternative_urls': {'unique': True},
    }

    _attribute_map = {
        'url': {'key': 'url', 'type': 'str'},
        'alternative_urls': {'key': 'alternativeUrls', 'type': '[str]'},
        'user': {'key': 'user', 'type': 'CredentialApiModel'},
        'security_mode': {'key': 'securityMode', 'type': 'SecurityMode'},
        'security_policy': {'key': 'securityPolicy', 'type': 'str'},
        'certificate': {'key': 'certificate', 'type': 'bytearray'},
    }

    def __init__(self, url, alternative_urls=None, user=None, security_mode="Best", security_policy=None, certificate=None):
        super(EndpointApiModel, self).__init__()
        self.url = url
        self.alternative_urls = alternative_urls
        self.user = user
        self.security_mode = security_mode
        self.security_policy = security_policy
        self.certificate = certificate
