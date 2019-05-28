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
        private readonly GeneralChaosSetting _generalChaosSetting;

        public PaymentServiceAdapter(ResilientHttpClient httpClient, string paymentServiceBaseUrl, GeneralChaosSetting generalChaosSetting)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _generalChaosSetting = generalChaosSetting ?? throw new ArgumentException(nameof(generalChaosSetting));
            _paymentServiceBaseUrl = !string.IsNullOrWhiteSpace(paymentServiceBaseUrl) ? paymentServiceBaseUrl : throw new ArgumentNullException(nameof(paymentServiceBaseUrl));
        }

        public async Task<PaymentInfo> ProcessPaymentAsync(int userId, string reference)
        {
            var uri = new Uri(
                new Uri(_paymentServiceBaseUrl),
                string.Format(ThirdPartyServices.Payment.PerformPayment(), userId, reference));

            var request = new HttpRequestMessage(HttpMethod.Post, uri);

            var context = new Context(OperationKeys.PaymentApi.ToString()).WithChaosSettings(_generalChaosSetting);
            var response = await _httpClient.SendAsync(request, context);

            response.EnsureSuccessStatusCode();
            return PaymentInfoTranslator.Translate(await response.Content.ReadAsStringAsync());
        }
    }
}