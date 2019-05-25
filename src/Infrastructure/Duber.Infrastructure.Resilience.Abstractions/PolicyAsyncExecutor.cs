using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;

namespace Duber.Infrastructure.Resilience.Abstractions
{
    /// <summary>
    /// Executes the action applying all the policies defined in the wrapper
    /// </summary>
    public class PolicyAsyncExecutor : IPolicyAsyncExecutor
    {
        private readonly IEnumerable<IAsyncPolicy> _asyncPolicies;

        public PolicyRegistry PolicyRegistry { get; set; }

        public PolicyAsyncExecutor(IEnumerable<IAsyncPolicy> policies)
        {
            PolicyRegistry = new PolicyRegistry();
            _asyncPolicies = policies ?? throw new ArgumentNullException(nameof(policies));
            (_asyncPolicies as List<IAsyncPolicy>).ForEach(policy => PolicyRegistry.Add(policy.PolicyKey, policy));
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            var policyWrap = Policy.WrapAsync(_asyncPolicies.ToArray());
            return await policyWrap.ExecuteAsync(action);
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            var policyWrap = Policy.WrapAsync(_asyncPolicies.ToArray());
            await policyWrap.ExecuteAsync(action);
        }

        public async Task<T> ExecuteAsync<T>(Func<Context, Task<T>> action, Context context)
        {
            var policyWrap = Policy.WrapAsync(_asyncPolicies.ToArray());
            return await policyWrap.ExecuteAsync(action, context);
        }

        public async  Task ExecuteAsync(Func<Context, Task> action, Context context)
        {
            var policyWrap = Policy.WrapAsync(_asyncPolicies.ToArray());
            await policyWrap.ExecuteAsync(action, context);
        }
    }
}
