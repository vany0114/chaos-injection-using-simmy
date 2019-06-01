using Duber.Infrastructure.Chaos;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Duber.WebSite.Models
{
    public class GeneralChaosSettingViewModel : GeneralChaosSetting
    {
        public GeneralChaosSettingViewModel()
        {
        }

        public GeneralChaosSettingViewModel(GeneralChaosSetting setting)
        {
            AutomaticChaosInjectionEnabled = setting.Frecuency.TotalMilliseconds > 0 || setting.MaxDuration.TotalMilliseconds > 0;
            MaxDuration = setting.MaxDuration;
            Frecuency = setting.Frecuency;
            PercentageNodesToRestart = setting.PercentageNodesToRestart;
            PercentageNodesToStop = setting.PercentageNodesToStop;
            OperationChaosSettings = setting.OperationChaosSettings;
            ClusterChaosEnabled = setting.PercentageNodesToStop > 0 || setting.PercentageNodesToRestart > 0;
        }

        public bool AutomaticChaosInjectionEnabled { get; set; }

        public bool ClusterChaosEnabled { get; set; }

        public List<SelectListItem> OperationKeys { get; set; }

        public List<SelectListItem> HttpStatusCodes { get; set; }
    }
}
