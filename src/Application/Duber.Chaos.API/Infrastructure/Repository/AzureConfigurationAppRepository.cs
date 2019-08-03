using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Azure.ApplicationModel.Configuration;
using Duber.Infrastructure.Chaos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Duber.Chaos.API.Infrastructure.Repository
{
    public class AzureConfigurationAppRepository : IChaosRepository
    {
        private readonly IConfiguration _configuration;
        private Dictionary<string, object> _settingsToUpdate;
        private readonly GeneralChaosSetting _chaosSettings;
        private readonly MethodInfo _methodInfo = typeof(AzureConfigurationAppRepository).GetMethod(nameof(GetSettingsToUpdate), BindingFlags.NonPublic | BindingFlags.Instance);

        public AzureConfigurationAppRepository(IOptionsSnapshot<GeneralChaosSetting> chaosSettings, IConfiguration configuration)
        {
            _chaosSettings = chaosSettings.Value;
            _configuration = configuration;
        }

        public Task<GeneralChaosSetting> GetChaosSettingsAsync()
        {
            return Task.FromResult(_chaosSettings);
        }

        // this nasty code is due to Azure App Configuration doesn't support a batch or generic update, it's necessary to update one setting at a time.
        public async Task UpdateChaosSettings(GeneralChaosSetting settings)
        {
            _settingsToUpdate = new Dictionary<string, object>();
            GetSettingsToUpdate(settings);
            var client = new ConfigurationClient(_configuration.GetConnectionString("AppConfig"));

            // TODO: update just the ones that really changed.
            foreach (var settingToUpdate in _settingsToUpdate)
            {
                var setting = new ConfigurationSetting(settingToUpdate.Key, settingToUpdate.Value.ToNullString());
                await client.SetAsync(setting);
            }

            // find which settings are going to be deleted.
            var currentSettings = await GetChaosSettingsAsync();
            var seetingsToDelete = currentSettings?.OperationChaosSettings?
                .Where(settingToDelete => !settings.OperationChaosSettings.Select(x => x.Id).Contains(settingToDelete.Id));

            if (seetingsToDelete != null)
                foreach (var settingToDelete in seetingsToDelete)
                {
                    _settingsToUpdate = new Dictionary<string, object>();
                    GetSettingsToUpdate(settingToDelete, "OperationChaosSettings", currentSettings.OperationChaosSettings.IndexOf(settingToDelete));
                    
                    foreach (var settingToUpdate in _settingsToUpdate)
                    {
                        await client.DeleteAsync(settingToUpdate.Key);
                    }
                }

            await client.SetAsync(new ConfigurationSetting("GeneralChaosSetting:Sentinel", "True"));
        }

        // this nasty code is due to Azure App Configuration doesn't support a batch or generic update, it's necessary to update one setting at a time.
        private void GetSettingsToUpdate<T>(T value, string parent = null, int? index = null)
        {
            foreach (var property in typeof(T).GetProperties())
            {
                if (typeof(IList).IsAssignableFrom(property.PropertyType))
                {
                    var list = (IList)property.GetValue(value);
                    var listType = property.PropertyType.GenericTypeArguments[0];

                    for (var i = 0; i < list?.Count; i++)
                    {
                        var listItem = list[i];
                        var generic = _methodInfo.MakeGenericMethod(listType);
                        generic.Invoke(this, new[] { listItem, property.Name, i });
                    }
                }
                else if (!property.PropertyType.IsPrimitive())
                {
                    var generic = _methodInfo.MakeGenericMethod(property.PropertyType);
                    generic.Invoke(this, new[] { property.GetValue(value), property.Name, null });
                }

                if (property.PropertyType.IsPrimitive())
                {
                    var settingName = $"{nameof(GeneralChaosSetting)}:{GetParent(parent, index)}{property.Name}";
                    var settingValue = property.GetValue(value);
                    _settingsToUpdate.Add(settingName, settingValue);
                }
            }
        }

        private static string GetParent(string parent, int? index)
        {
            if (parent == null)
                return string.Empty;

            return index.HasValue ? $"{parent}:{index}:" : $"{parent}:";
        }
    }

    internal static class Extensions
    {
        internal static bool IsPrimitive(this Type t)
        {
            return t.IsPrimitive ||
                   t == typeof(decimal) ||
                   t == typeof(string) ||
                   t == typeof(DateTime) ||
                   t == typeof(DateTimeOffset) ||
                   t == typeof(Guid) ||
                   t == typeof(TimeSpan) ||
                   t == typeof(Enum);
        }

        internal static string ToNullString(this object obj)
        {
            return obj == null ? string.Empty : obj.ToString();
        }
    }
}
