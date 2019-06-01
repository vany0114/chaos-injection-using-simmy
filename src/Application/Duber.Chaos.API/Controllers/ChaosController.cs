using System;
using System.Net;
using System.Threading.Tasks;
using Duber.Chaos.API.Infrastructure.Repository;
using Duber.Infrastructure.Chaos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Duber.Chaos.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChaosController : ControllerBase
    {
        private readonly IChaosRepository _chaosRepository;
        private readonly IOptions<GeneralChaosSetting> _azureSettings;

        public ChaosController(IChaosRepository chaosRepository, IOptions<GeneralChaosSetting> azureSettings)
        {
            _chaosRepository = chaosRepository ?? throw new ArgumentNullException(nameof(chaosRepository));
            _azureSettings = azureSettings ?? throw new ArgumentNullException(nameof(azureSettings));
        }

        /// <summary>
        /// Returns the chaos configuration.
        /// </summary>
        /// <returns>Returns general and all operations configuration.</returns>
        /// <response code="200">Returns a list of Invoice object.</response>
        [Route("get")]
        [HttpGet]
        [ProducesResponseType(typeof(GeneralChaosSetting), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Get()
        {
            var chaosSettings = await _chaosRepository.GetChaosSettingsAsync();

            if (chaosSettings == null)
                return NotFound();

            if (chaosSettings.ExecutionInformation == null)
                chaosSettings.ExecutionInformation = new ExecutionInformation();

            // TODO: consider getting these values from Azure KeyVault.
            chaosSettings.SubscriptionId = _azureSettings.Value.SubscriptionId;
            chaosSettings.ClientId = _azureSettings.Value.ClientId;
            chaosSettings.ClientKey = _azureSettings.Value.ClientKey;
            chaosSettings.TenantId = _azureSettings.Value.TenantId;

            return Ok(chaosSettings);
        }

        /// <summary>
        /// Save the provided chaos settings.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>No content.</returns>
        /// <response code="204">No content.</response>
        [Route("update")]
        [HttpPost]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Post([FromBody] GeneralChaosSetting settings)
        {
            if (settings.MaxDuration > settings.Frequency)
            {
                return BadRequest("Duration should be less than Frequency.");
            }

            await _chaosRepository.UpdateChaosSettings(settings);
            return Ok();
        }
    }
}
