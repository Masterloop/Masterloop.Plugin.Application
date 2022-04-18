using System;
using System.Collections.Generic;
using Masterloop.Core.Types.Base;
using Masterloop.Core.Types.Observations;
using Newtonsoft.Json;

namespace Masterloop.Plugin.Application
{
    internal class MasterloopObservationHelper
    {
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
    }
}