using System;
using System.Collections.Generic;
using Masterloop.Core.Types.Base;
using Masterloop.Core.Types.Observations;
using Newtonsoft.Json;

namespace Masterloop.Plugin.Application
{
    internal class MasterloopObservationHelper
    {
        public static Observation DeserializeObservation(string json, DataType dataType)
        {
            if (!string.IsNullOrEmpty(json))
                switch (dataType)
                {
                    case DataType.Boolean:
                        return JsonConvert.DeserializeObject<BooleanObservation>(json);
                    case DataType.Double:
                        return JsonConvert.DeserializeObject<DoubleObservation>(json);
                    case DataType.Integer:
                        return JsonConvert.DeserializeObject<IntegerObservation>(json);
                    case DataType.Position:
                        return JsonConvert.DeserializeObject<PositionObservation>(json);
                    case DataType.String:
                        return JsonConvert.DeserializeObject<StringObservation>(json);
                    case DataType.Statistics:
                        return JsonConvert.DeserializeObject<StatisticsObservation>(json);
                    default:
                        throw new NotSupportedException($"Unsupported data type: {dataType.ToString()}");
                }

            return null;
        }

        public static IdentifiedObservation[] DeserializeIdentifiedObservations(string json)
        {
            if (!string.IsNullOrEmpty(json) && json.Length > 0)
            {
                var expandedObservationValues = JsonConvert.DeserializeObject<ExpandedObservationValue[]>(json);
                if (expandedObservationValues != null && expandedObservationValues.Length > 0)
                {
                    var identifiedObservations = new List<IdentifiedObservation>();
                    foreach (var expandedObservationValue in expandedObservationValues)
                    {
                        var identifiedObservation = new IdentifiedObservation
                        {
                            ObservationId = expandedObservationValue.Id,
                            Observation = ObservationStringConverter.StringToObservation(
                                expandedObservationValue.Timestamp, expandedObservationValue.Value,
                                expandedObservationValue.DataType)
                        };
                        identifiedObservations.Add(identifiedObservation);
                    }

                    return identifiedObservations.ToArray();
                }
            }

            return null;
        }

        public static Observation[] DeserializeObservations(string json, DataType dataType)
        {
            if (!string.IsNullOrEmpty(json))
                switch (dataType)
                {
                    case DataType.Boolean:
                        return JsonConvert.DeserializeObject<BooleanObservation[]>(json);
                    case DataType.Double:
                        return JsonConvert.DeserializeObject<DoubleObservation[]>(json);
                    case DataType.Integer:
                        return JsonConvert.DeserializeObject<IntegerObservation[]>(json);
                    case DataType.Position:
                        return JsonConvert.DeserializeObject<PositionObservation[]>(json);
                    case DataType.String:
                        return JsonConvert.DeserializeObject<StringObservation[]>(json);
                    case DataType.Statistics:
                        return JsonConvert.DeserializeObject<StatisticsObservation[]>(json);
                    default:
                        throw new NotSupportedException($"Unsupported data type: {dataType.ToString()}");
                }

            return null;
        }
    }
}