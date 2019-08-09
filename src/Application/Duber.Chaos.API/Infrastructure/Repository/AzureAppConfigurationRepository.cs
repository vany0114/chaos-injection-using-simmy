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
    /// <summary>
    /// // All the nasty code related to Generics is due to Azure App Configuration doesn't support a batch or generic update, it's necessary to update one setting at a time.
    /// </summary>
    public class AzureAppConfigurationRepository : IChaosRepository
    {
        private readonly IConfiguration _configuration;
        private Dictionary<string, object> _settingsToUpdate;
        private readonly GeneralChaosSetting _chaosSettings;
        private readonly MethodInfo _getSettingsToUpdateMethodInfo = typeof(AzureAppConfigurationRepository).GetMethod(nameof(GetSettingsToUpdate), BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly MethodInfo _settingHasChangedMethodInfo = typeof(AzureAppConfigurationRepository).GetMethod(nameof(SettingHasChanged), BindingFlags.NonPublic | BindingFlags.Instance);

        public AzureAppConfigurationRepository(IOptionsSnapshot<GeneralChaosSetting> chaosSettings, IConfiguration configuration)
        {
            _chaosSettings = chaosSettings.Value;
            _configuration = configuration;
        }

        public Task<GeneralChaosSetting> GetChaosSettingsAsync()
        {
            return Task.FromResult(_chaosSettings);
        }

        public async Task UpdateChaosSettings(GeneralChaosSetting settings)
        {
            _settingsToUpdate = new Dictionary<string, object>();
            var currentSettings = await GetChaosSettingsAsync();
            GetSettingsToUpdate(settings);

            var client = new ConfigurationClient(_configuration.GetConnectionString("AppConfig"));
            foreach (var settingToUpdate in _settingsToUpdate)
            {
                if (!SettingHasChanged(settingToUpdate.Key, settingToUpdate.Value.ToNullString(), currentSettings)) continue;
                var setting = new ConfigurationSetting(settingToUpdate.Key, settingToUpdate.Value.ToNullString());
                await client.SetAsync(setting);
            }

            // find which settings are going to be deleted.
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

            // We always update our sentinel in order to all the settings will be refreshed.
            await client.SetAsync(new ConfigurationSetting("GeneralChaosSetting:Sentinel", "True"));
        }

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
                        var generic = _getSettingsToUpdateMethodInfo.MakeGenericMethod(listType);
                        generic.Invoke(this, new[] { listItem, property.Name, i });
                    }
                }
                else if (!property.PropertyType.IsPrimitive())
                {
                    var generic = _getSettingsToUpdateMethodInfo.MakeGenericMethod(property.PropertyType);
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

        private bool SettingHasChanged<T>(string key, string value, T currentSettings)
        {
            var properties = key.Split(":");
            var propertiesToExclude = new[]
            {
                nameof(_chaosSettings.Sentinel),
                nameof(_chaosSettings.Id),
                nameof(_chaosSettings.SubscriptionId),
                nameof(_chaosSettings.TenantId),
                nameof(_chaosSettings.ClientId),
                nameof(_chaosSettings.ClientKey)
            };
            if (propertiesToExclude.Contains(properties.Last())) return false;

            foreach (var property in properties)
            {
                var propertyInfo = typeof(T).GetProperty(property);
                if (propertyInfo == null) continue;

                if (propertyInfo.PropertyType.IsPrimitive())
                {
                    return !propertyInfo.GetValue(currentSettings).ToNullString().Equals(value);
                }
                else if (typeof(IList).IsAssignableFrom(propertyInfo.PropertyType))
                {
                    var list = (IList)propertyInfo.GetValue(currentSettings);
                    var listType = propertyInfo.PropertyType.GenericTypeArguments[0];

                    var index = int.Parse(properties[2]);
                    if (index > list.Count - 1)
                        return true; // it means it's a new operation setting.

                    var listItem = list[index];
                    var generic = _settingHasChangedMethodInfo.MakeGenericMethod(listType);
                    return (bool)generic.Invoke(this, new[] { properties.Last(), value, listItem });
                }
                else if (!propertyInfo.PropertyType.IsPrimitive())
                {
                    var generic = _settingHasChangedMethodInfo.MakeGenericMethod(propertyInfo.PropertyType);
                    return (bool)generic.Invoke(this, new[] { properties.Last(), value, propertyInfo.GetValue(currentSettings) });
                }
            }

            return false;
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
