namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Model {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using System;
    using System.Collections.Generic;

    internal static class MonitoredItemModelTestHelper {
        public static bool IsSameAs(DataMonitoredItemModel model1, DataMonitoredItemModel model2) {
            if (!IsSameAs(model1 as BaseMonitoredItemModel, model2 as BaseMonitoredItemModel)) {
                return false;
            }
            if (!IsSameAs(model1.DataChangeFilter, model2.DataChangeFilter)) {
                return false;
            }
            if (!IsSameAs(model1.AggregateFilter, model2.AggregateFilter)) {
                return false;
            }
            if (model1.HeartbeatInterval != model2.HeartbeatInterval) {
                return false;
            }
            return true;
        }

        public static bool IsSameAs(EventMonitoredItemModel model1, EventMonitoredItemModel model2) {
            if (!IsSameAs(model1 as BaseMonitoredItemModel, model2 as BaseMonitoredItemModel)) {
                return false;
            }
            if (!IsSameAs(model1.EventFilter, model2.EventFilter)) {
                return false;
            }
            return true;
        }

        public static bool IsSameAs(BaseMonitoredItemModel model1, BaseMonitoredItemModel model2) {
            if (model1 == null && model2 == null) {
                return true;
            }
            if (model1 == null || model2 == null) {
                return false;
            }
            if (model1.StartNodeId != model2.StartNodeId) {
                return false;
            }
            if (model1.SamplingInterval != model2.SamplingInterval) {
                return false;
            }
            if (model1.QueueSize != model2.QueueSize) {
                return false;
            }
            if (model1.AttributeId != model2.AttributeId) {
                return false;
            }
            if (model1.DiscardNew != model2.DiscardNew) {
                return false;
            }
            if (model1.DisplayName != model2.DisplayName) {
                return false;
            }
            if (model1.Id != model2.Id) {
                return false;
            }
            if (model1.IndexRange != model2.IndexRange) {
                return false;
            }
            if (model1.MonitoringMode != model2.MonitoringMode) {
                return false;
            }
            if (!IsSameAs(model1.RelativePath, model2.RelativePath)) {
                return false;
            }
            if (model1.TriggerId != model2.TriggerId) {
                return false;
            }
            return true;
        }

        public static bool IsSameAs<T>(T[] array1, T[] array2) where T : IComparable<T> {
            if ((array1 != null && array2 == null) ||
                (array1 == null && array2 != null)) {
                return false;
            }
            if (array1 != null && array2 != null) {
                if (array1.Length != array2.Length) {
                    return false;
                }
                for (var i = 0; i < array1.Length; i++) {
                    if (array1[i].CompareTo(array2[i]) != 0) {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool IsSameAs(DataChangeFilterModel model1, DataChangeFilterModel model2) {
            if (model1 == null && model2 == null) {
                return true;
            }
            if (model1 == null || model2 == null) {
                return false;
            }
            if (model1.DataChangeTrigger != model2.DataChangeTrigger) {
                return false;
            }
            if (model1.DeadBandType != model2.DeadBandType) {
                return false;
            }
            if (model1.DeadBandValue != model2.DeadBandValue) {
                return false;
            }
            return true;
        }

        public static bool IsSameAs(AggregateFilterModel model1, AggregateFilterModel model2) {
            if (model1 == null && model2 == null) {
                return true;
            }
            if (model1 == null || model2 == null) {
                return false;
            }
            if (model1.StartTime != model2.StartTime) {
                return false;
            }
            if (model1.AggregateTypeId != model2.AggregateTypeId) {
                return false;
            }
            if (model1.ProcessingInterval != model2.ProcessingInterval) {
                return false;
            }
            if (!IsSameAs(model1.AggregateConfiguration, model2.AggregateConfiguration)) {
                return false;
            }
            return true;
        }

        public static bool IsSameAs(AggregateConfigurationModel model1, AggregateConfigurationModel model2) {
            if (model1 == null && model2 == null) {
                return true;
            }
            if (model1 == null || model2 == null) {
                return false;
            }
            if (model1.UseServerCapabilitiesDefaults != model2.UseServerCapabilitiesDefaults) {
                return false;
            }
            if (model1.TreatUncertainAsBad != model2.TreatUncertainAsBad) {
                return false;
            }
            if (model1.PercentDataBad != model2.PercentDataBad) {
                return false;
            }
            if (model1.PercentDataGood != model2.PercentDataGood) {
                return false;
            }
            if (model1.UseSlopedExtrapolation != model2.UseSlopedExtrapolation) {
                return false;
            }
            return true;
        }

        public static bool IsSameAs(EventFilterModel model1, EventFilterModel model2) {
            if (model1 == null && model2 == null) {
                return true;
            }
            if (model1 == null || model2 == null) {
                return false;
            }
            if (!IsSameAs(model1.SelectClauses, model2.SelectClauses)) {
                return false;
            }
            if (!IsSameAs(model1.WhereClause, model2.WhereClause)) {
                return false;
            }
            return true;
        }

        public static bool IsSameAs(List<SimpleAttributeOperandModel> models1, List<SimpleAttributeOperandModel> models2) {
            if (models1 == null && models2 == null) {
                return true;
            }
            if (models1 == null || models2 == null) {
                return false;
            }
            if (models1.Count != models2.Count) {
                return false;
            }
            for (var i = 0; i < models1.Count; i++) {
                if (!IsSameAs(models1[i], models2[i])) {
                    return false;
                }
            }
            return true;
        }

        public static bool IsSameAs(SimpleAttributeOperandModel model1, SimpleAttributeOperandModel model2) {
            if (model1 == null && model2 == null) {
                return true;
            }
            if (model1 == null || model2 == null) {
                return false;
            }
            if (model1.TypeDefinitionId != model2.TypeDefinitionId) {
                return false;
            }
            if (!IsSameAs(model1.BrowsePath, model2.BrowsePath)) {
                return false;
            }
            if (model1.AttributeId != model2.AttributeId) {
                return false;
            }
            if (model1.IndexRange != model2.IndexRange) {
                return false;
            }
            return true;
        }

        public static bool IsSameAs(ContentFilterModel model1, ContentFilterModel model2) {
            if (model1 == null && model2 == null) {
                return true;
            }
            if (model1 == null || model2 == null) {
                return false;
            }
            if (!IsSameAs(model1.Elements, model2.Elements)) {
                return false;
            }
            return true;
        }

        public static bool IsSameAs(List<ContentFilterElementModel> models1, List<ContentFilterElementModel> models2) {
            if (models1 == null && models2 == null) {
                return true;
            }
            if (models1 == null || models2 == null) {
                return false;
            }
            if (models1.Count != models2.Count) {
                return false;
            }
            for (var i = 0; i < models1.Count; i++) {
                if (!IsSameAs(models1[i], models2[i])) {
                    return false;
                }
            }
            return true;
        }

        public static bool IsSameAs(ContentFilterElementModel model1, ContentFilterElementModel model2) {
            if (model1 == null && model2 == null) {
                return true;
            }
            if (model1 == null || model2 == null) {
                return false;
            }
            if (model1.FilterOperator != model2.FilterOperator) {
                return false;
            }
            if (!IsSameAs(model1.FilterOperands, model2.FilterOperands)) {
                return false;
            }
            return true;
        }

        public static bool IsSameAs(List<FilterOperandModel> models1, List<FilterOperandModel> models2) {
            if (models1 == null && models2 == null) {
                return true;
            }
            if (models1 == null || models2 == null) {
                return false;
            }
            if (models1.Count != models2.Count) {
                return false;
            }
            for (var i = 0; i < models1.Count; i++) {
                if (!IsSameAs(models1[i], models2[i])) {
                    return false;
                }
            }
            return true;
        }

        public static bool IsSameAs(FilterOperandModel model1, FilterOperandModel model2) {
            if (model1 == null && model2 == null) {
                return true;
            }
            if (model1 == null || model2 == null) {
                return false;
            }
            if (model1.NodeId != model2.NodeId) {
                return false;
            }
            if (!IsSameAs(model1.BrowsePath, model2.BrowsePath)) {
                return false;
            }
            if (model1.AttributeId != model2.AttributeId) {
                return false;
            }
            if (model1.IndexRange != model2.IndexRange) {
                return false;
            }
            if (model1.Value != model2.Value) {
                return false;
            }
            if (model1.Index != model2.Index) {
                return false;
            }
            if (model1.Alias != model2.Alias) {
                return false;
            }
            return true;
        }
    }
}
