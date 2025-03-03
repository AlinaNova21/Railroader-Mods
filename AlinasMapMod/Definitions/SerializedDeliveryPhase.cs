using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Progression;

namespace AlinasMapMod.Definitions
{
    public class SerializedDeliveryPhase
    {

        public IEnumerable<SerializedDelivery> Deliveries { get; set; } = new List<SerializedDelivery>();
        public int Cost { get; set; } = 0;
        public string IndustryComponent { get; set; } = "";

        public SerializedDeliveryPhase()
        {
        }

        public SerializedDeliveryPhase(Section.DeliveryPhase dp)
        {
            Deliveries = dp.deliveries.Select(d => new SerializedDelivery(d));
            Cost = dp.cost;
            IndustryComponent = dp.industryComponent?.Identifier ?? "";
        }

        internal void ApplyTo(Section.DeliveryPhase phase, ObjectCache cache)
        {
            cache.IndustryComponents.TryGetValue(IndustryComponent, out var industryComponent);
            phase.industryComponent = (Model.Ops.ProgressionIndustryComponent)industryComponent;
            phase.cost = Cost;
            phase.deliveries = Deliveries.Select(d => {
                var delivery = new Section.Delivery();
                d.ApplyTo(delivery, cache);
                return delivery;
            }).ToArray();
        }
    }
}
