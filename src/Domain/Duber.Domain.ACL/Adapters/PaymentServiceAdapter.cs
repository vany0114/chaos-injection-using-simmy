using System;
using System.Net.Http;
using System.Threading.Tasks;
using Duber.Domain.ACL.Contracts;
using Duber.Domain.ACL.Translators;
using Duber.Domain.SharedKernel.Chaos;
using Duber.Domain.SharedKernel.Model;
using Duber.Infrastructure.Chaos;
using Duber.Infrastructure.Resilience.Http;
using Polly;

namespace Duber.Domain.ACL.Adapters
{
    public class PaymentServiceAdapter : IPaymentServiceAdapter
    {
        private readonly ResilientHttpClient _httpClient;
        private readonly string _paymentServiceBaseUrl;
        private readonly Lazy<Task<GeneralChaosSetting>> _generalChaosSettingFactory;

        public PaymentServiceAdapter(ResilientHttpClient httpClient, string paymentServiceBaseUrl, Lazy<Task<GeneralChaosSetting>> generalChaosSettingFactory)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _generalChaosSettingFactory = generalChaosSettingFactory ?? throw new ArgumentException(nameof(generalChaosSettingFactory));
            _paymentServiceBaseUrl = !string.IsNullOrWhiteSpace(paymentServiceBaseUrl) ? paymentServiceBaseUrl : throw new ArgumentNullException(nameof(paymentServiceBaseUrl));
        }

        public async Task<PaymentInfo> ProcessPaymentAsync(int userId, string reference)
        {
            var uri = new Uri(
                new Uri(_paymentServiceBaseUrl),
                string.Format(ThirdPartyServices.Payment.PerformPayment(), userId, reference));

            var request = new HttpRequestMessage(HttpMethod.Post, uri);

            var context = new Context(OperationKeys.PaymentApi.ToString()).WithChaosSettings(await _generalChaosSettingFactory.Value);
            var response = await _httpClient.SendAsync(request, context);

            response.EnsureSuccessStatusCode();
            return PaymentInfoTranslator.Translate(await response.Content.ReadAsStringAsync());
        }
    }
}