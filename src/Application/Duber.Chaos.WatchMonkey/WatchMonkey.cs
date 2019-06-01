using System;
using System.Linq;
using System.Threading.Tasks;
using Duber.Infrastructure.Chaos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Duber.Chaos.WatchMonkey
{
    public class WatchMonkey
    {
        private readonly ChaosApiHttpClient _chaosApiHttpClient;

        public WatchMonkey(ChaosApiHttpClient chaosApiHttpClient)
        {
            _chaosApiHttpClient = chaosApiHttpClient;
        }

        [FunctionName("WatchMonkey")]
        public async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo timer, ILogger log)
        {
            var chaosSeetings = await _chaosApiHttpClient.GetGeneralChaosSettings();

            if (chaosSeetings.AutomaticChaosInjectionEnabled)
            {
                var deltaExecutionTime = DateTime.UtcNow - chaosSeetings.ExecutionInformation.LastTimeExecuted;
                if (deltaExecutionTime >= chaosSeetings.Frequency && chaosSeetings.ExecutionInformation.MonkeysReleased == false)
                {
                    chaosSeetings.ClusterChaosEnabled = true;
                    chaosSeetings.ExecutionInformation.MonkeysReleased = true;
                    chaosSeetings.ExecutionInformation.LastTimeExecuted = new DateTimeOffset(DateTime.UtcNow);
                    chaosSeetings.OperationChaosSettings
                        .ForEach(operation => operation.Enabled = true);

                    await _chaosApiHttpClient.UpdateGeneralChaosSettings(chaosSeetings);
                    log.LogInformation($"Monkeys released at: {chaosSeetings.ExecutionInformation.LastTimeExecuted}");
                }
                else if (chaosSeetings.ExecutionInformation.MonkeysReleased == true)
                {
                    deltaExecutionTime = DateTime.UtcNow - chaosSeetings.ExecutionInformation.LastTimeExecuted;
                    if (deltaExecutionTime >= chaosSeetings.MaxDuration)
                    {
                        chaosSeetings.ClusterChaosEnabled = false;
                        chaosSeetings.ExecutionInformation.MonkeysReleased = false;
                        chaosSeetings.ExecutionInformation.ChaosStoppedAt = new DateTimeOffset(DateTime.UtcNow);
                        chaosSeetings.OperationChaosSettings
                            .ForEach(operation => operation.Enabled = false);

                        await _chaosApiHttpClient.UpdateGeneralChaosSettings(chaosSeetings);
                        log.LogInformation($"Monkeys caged at at: {chaosSeetings.ExecutionInformation.ChaosStoppedAt}");
                    }
                    else
                    {
                        // look for the duration of specific monkeys
                        var monkeysToStop = chaosSeetings
                            .OperationChaosSettings
                            .Where(operation => operation.Duration.TotalMinutes > 0 && deltaExecutionTime >= operation.Duration)
                            .ToList();

                        if(monkeysToStop.Count > 0)
                        {
                            monkeysToStop.ForEach(operation => operation.Enabled = false);
                            await _chaosApiHttpClient.UpdateGeneralChaosSettings(chaosSeetings);
                        }
                    }
                }
            }

            log.LogInformation($"Automatic Chaos Injection Enabled: {chaosSeetings.AutomaticChaosInjectionEnabled}");
            log.LogInformation($"WatchMonkey executed at: {DateTime.Now}");
        }
    }
}
