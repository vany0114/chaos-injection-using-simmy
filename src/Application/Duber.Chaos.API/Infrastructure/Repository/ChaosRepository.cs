using System;
using System.Threading.Tasks;
using Duber.Infrastructure.Chaos;
using Microsoft.Extensions.Caching.Distributed;
using Duber.Infrastructure.Cache.Extensions;

namespace Duber.Chaos.API.Infrastructure.Repository
{
    public class ChaosRepository : IChaosRepository
    {
        private readonly IDistributedCache _cache;
        private const string CHAOS_KEY = "CHAOS_KEY";

        public ChaosRepository(IDistributedCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<GeneralChaosSetting> GetChaosSettingsAsync()
        {
            return await _cache.GetAsync<GeneralChaosSetting>(CHAOS_KEY);
        }

        public async Task UpdateChaosSettings(GeneralChaosSetting settings)
        {
            await _cache.SetAsync(
                CHAOS_KEY,
                settings,
                new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) });
        }
    }
}
