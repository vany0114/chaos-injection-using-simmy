using System;
using System.Collections.Generic;
using System.Linq;
using Polly;
using Polly.Registry;

namespace Duber.Infrastructure.Resilience.Abstractions
{
    /// <summary>
    /// Executes the action applying all the policies defined in the wrapper
    /// </summary>
    public class PolicySyncExecutor : IPolicySyncExecutor
    {
        private readonly IEnumerable<ISyncPolicy> _syncPolicies;

        public PolicyRegistry PolicyRegistry { get; set; }

        public PolicySyncExecutor(IEnumerable<ISyncPolicy> policies)
        {
            PolicyRegistry = new PolicyRegistry();
            _syncPolicies = policies ?? throw new ArgumentNullException(nameof(policies));
            (_syncPolicies as List<ISyncPolicy>).ForEach(policy => PolicyRegistry.Add(policy.PolicyKey, policy));
        }

        public T Execute<T>(Func<T> action)
        {
            var policyWrap = Policy.Wrap(_syncPolicies.ToArray());
            return policyWrap.Execute(action);
        }

        public void Execute(Action action)
        {
            var policyWrap = Policy.Wrap(_syncPolicies.ToArray());
            policyWrap.Execute(action);
        }

        public T Execute<T>(Func<Context, T> action, Context context)
        {
            var policyWrap = Policy.Wrap(_syncPolicies.ToArray());
            return policyWrap.Execute(action, context);
        }

        public void Execute(Action<Context> action, Context context)
        {
            var policyWrap = Policy.Wrap(_syncPolicies.ToArray());
            policyWrap.Execute(action, context);
        }
    }
}
