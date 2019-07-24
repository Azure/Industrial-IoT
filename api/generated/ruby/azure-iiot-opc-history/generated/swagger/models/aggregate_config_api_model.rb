# encoding: utf-8
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for
# license information.
#
# Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
# Changes may cause incorrect behavior and will be lost if the code is
# regenerated.

module azure.iiot.opc.history
  module Models
    #
    # Aggregate configuration
    #
    class AggregateConfigApiModel
      # @return [Boolean] Whether to use the default server caps
      attr_accessor :use_server_capabilities_defaults

      # @return [Boolean] Whether to treat uncertain as bad
      attr_accessor :treat_uncertain_as_bad

      # @return [Integer] Percent of data that is bad
      attr_accessor :percent_data_bad

      # @return [Integer] Percent of data that is good
      attr_accessor :percent_data_good

      # @return [Boolean] Whether to use sloped extrapolation.
      attr_accessor :use_sloped_extrapolation


      #
      # Mapper for AggregateConfigApiModel class as Ruby Hash.
      # This will be used for serialization/deserialization.
      #
      def self.mapper()
        {
          client_side_validation: true,
          required: false,
          serialized_name: 'AggregateConfigApiModel',
          type: {
            name: 'Composite',
            class_name: 'AggregateConfigApiModel',
            model_properties: {
              use_server_capabilities_defaults: {
                client_side_validation: true,
                required: false,
                serialized_name: 'useServerCapabilitiesDefaults',
                type: {
                  name: 'Boolean'
                }
              },
              treat_uncertain_as_bad: {
                client_side_validation: true,
                required: false,
                serialized_name: 'treatUncertainAsBad',
                type: {
                  name: 'Boolean'
                }
              },
              percent_data_bad: {
                client_side_validation: true,
                required: false,
                serialized_name: 'percentDataBad',
                type: {
                  name: 'Number'
                }
              },
              percent_data_good: {
                client_side_validation: true,
                required: false,
                serialized_name: 'percentDataGood',
                type: {
                  name: 'Number'
                }
              },
              use_sloped_extrapolation: {
                client_side_validation: true,
                required: false,
                serialized_name: 'useSlopedExtrapolation',
                type: {
                  name: 'Boolean'
                }
              }
            }
          }
        }
      end
    end
  end
end
