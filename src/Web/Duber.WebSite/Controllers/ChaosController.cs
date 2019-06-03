using System.Linq;
using System.Threading.Tasks;
using Duber.Domain.SharedKernel.Chaos;
using Duber.Infrastructure.Chaos;
using Duber.WebSite.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Net;

namespace Duber.WebSite.Controllers
{
    public class ChaosController : Controller
    {
        private readonly ChaosApiHttpClient _httpClient;

        public ChaosController(ChaosApiHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var chaosSettings = await _httpClient.GetGeneralChaosSettings();
            var viewModel = new GeneralChaosSettingViewModel(chaosSettings);

            // set some default values arbitrarily
            if(viewModel.Frequency == default || viewModel.MaxDuration == default)
            {
                viewModel.Frequency = new TimeSpan(23, 59, 0);
                viewModel.MaxDuration = new TimeSpan(0, 15, 0);
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateOperationSettings(string operationKey)
        {
            var chaosSettings = await _httpClient.GetGeneralChaosSettings();
            var operationSettings = chaosSettings.OperationChaosSettings?.SingleOrDefault(x => x.OperationKey == operationKey);

            PopulateLists();
            return View("OperationSettings", operationSettings);
        }

        [HttpGet]
        public IActionResult AddOperationSettings()
        {
            PopulateLists();
            return View("OperationSettings", new OperationChaosSetting());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateGeneralSettings(GeneralChaosSettingViewModel viewModel)
        {
            if(viewModel.MaxDuration > viewModel.Frequency)
            {
                ModelState.AddModelError("MaxDuration", "Duration should be less than Frequency.");
                return View("Index", viewModel);
            }

            var chaosSettings = viewModel as GeneralChaosSetting;
            var originalSettings = await _httpClient.GetGeneralChaosSettings();
            chaosSettings.Id = Guid.NewGuid();
            chaosSettings.OperationChaosSettings = originalSettings.OperationChaosSettings;
            chaosSettings.ExecutionInformation = originalSettings.ExecutionInformation;

            await _httpClient.UpdateGeneralChaosSettings(chaosSettings);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddOrUpdateOperationSettings(OperationChaosSetting operationChaosSetting)
        {
            var chaosSettings = await _httpClient.GetGeneralChaosSettings();
            var index = chaosSettings.OperationChaosSettings.FindIndex(x => x.Id == operationChaosSetting.Id);

            if (index >= 0)
                chaosSettings.OperationChaosSettings[index] = operationChaosSetting;
            else
            {
                if(chaosSettings.OperationChaosSettings.Any(x => x.OperationKey == operationChaosSetting.OperationKey))
                {
                    PopulateLists();
                    ModelState.AddModelError("OperationKey", "There is already a setting using that operation key");
                    return View("OperationSettings", operationChaosSetting);
                }

                operationChaosSetting.Id = Guid.NewGuid();
                chaosSettings.OperationChaosSettings.Add(operationChaosSetting);
            }

            await _httpClient.UpdateGeneralChaosSettings(chaosSettings);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOperationSettings(string operationKey)
        {
            var chaosSettings = await _httpClient.GetGeneralChaosSettings();
            var operationSettings = chaosSettings.OperationChaosSettings?.SingleOrDefault(x => x.OperationKey == operationKey);

            chaosSettings.OperationChaosSettings.Remove(operationSettings);
            await _httpClient.UpdateGeneralChaosSettings(chaosSettings);
            return RedirectToAction("Index");
        }

        private void PopulateLists()
        {
            ViewData["OperationKeys"] = EnumToSelectList<OperationKeys>();
            ViewData["HttpStatusCodes"] = EnumToSelectList<HttpStatusCode>()
                .Where(x => int.Parse(x.Value) > 200)
                .Union(new List<SelectListItem> { new SelectListItem("--", "0") })
                .ToList();
        }

        private static List<SelectListItem> EnumToSelectList<TEnum>() where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum))
                .Cast<OperationKeys>()
                .Select(x => new SelectListItem { Value = x.ToString(), Text = x.ToString() })
                .ToList();
        }
    }
}