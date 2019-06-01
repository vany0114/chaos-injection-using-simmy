using Polly;

namespace Duber.Infrastructure.Chaos
{
    public static class SimmyContextExtensions
    {
        public const string ChaosSettings = "ChaosSettings";

        public static Context WithChaosSettings(this Context context, GeneralChaosSetting options)
        {
            context[ChaosSettings] = options;
            return context;
        }

        public static GeneralChaosSetting GetChaosSettings(this Context context) => context.GetSetting<GeneralChaosSetting>(ChaosSettings);

        private static T GetSetting<T>(this Context context, string key)
        {
            if (context.TryGetValue(key, out object setting))
            {
                if (setting is T)
                {
                    return (T)setting;
                }
            }
            return default;
        }
    }
}
