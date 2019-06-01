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
            AutomaticChaosInjectionEnabled = setting.AutomaticChaosInjectionEnabled;
            MaxDuration = setting.MaxDuration;
            Frequency = setting.Frequency;
            PercentageNodesToRestart = setting.PercentageNodesToRestart;
            PercentageNodesToStop = setting.PercentageNodesToStop;
            OperationChaosSettings = setting.OperationChaosSettings;
            ClusterChaosEnabled = setting.ClusterChaosEnabled;
        }

        public List<SelectListItem> OperationKeys { get; set; }

        public List<SelectListItem> HttpStatusCodes { get; set; }
    }
}
