using System;
using System.Collections.Generic;
using System.Linq;

namespace Duber.Infrastructure.Chaos
{
    [Serializable]
    public class GeneralChaosSetting
    {
        public bool AutomaticChaosInjectionEnabled { get; set; }

        public TimeSpan Frecuency { get; set; }

        public string SubscriptionId { get; set; }

        public string TenantId { get; set; }

        public string ClientId { get; set; }

        public string ClientKey { get; set; }

        public int PercentageNodesToRestart { get; set; }

        public int PercentageNodesToStop { get; set; }

        public List<OperationChaosSetting> OperationChaosSettings { get; set; }

        public OperationChaosSetting GetSettingsFor(string operationKey) => OperationChaosSettings?.SingleOrDefault(i => i.OperationKey == operationKey);
    }
}
