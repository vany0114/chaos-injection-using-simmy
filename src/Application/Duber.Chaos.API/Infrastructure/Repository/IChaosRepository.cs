using Duber.Infrastructure.Chaos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Duber.Chaos.API.Infrastructure.Repository
{
    public interface IChaosRepository
    {
        Task<GeneralChaosSetting> GetChaosSettingsAsync();

        Task UpdateChaosSettings(GeneralChaosSetting settings);
    }
}
