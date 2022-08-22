using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessPensionService.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ProcessPensionService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProcessPensionController : ControllerBase
    {
        private readonly IHttpClientFactory _contextFactory;
        public ProcessPensionController(IHttpClientFactory clientFactory)
        {
           _contextFactory = clientFactory;
        }

        [HttpPost]
        public async Task<IActionResult> GetProcessPensionAsync([FromBody]ProcessPensionInput pensionInput)
        {
            if (pensionInput.AaadharNumber.ToString().Length != 12)
            {
                return Content("Invalid pensioner detail provided, please provide valid detail.");
            }

            var result = await GetPensionerDetail(pensionInput.AaadharNumber);


            if (result.PensionType == null && result.SalaryEarned == 0 && result.BankCategory == null && result.Allowences == 0)
            {
                return NotFound("Invalid Aadhar number.");
            }

            var penstionDetail = CalculatePension(result);
            return Ok(penstionDetail);
        }

        [NonAction]
        private async Task<PentionerDetail> GetPensionerDetail(long aadhar)
        {
            PentionerDetail pentionerDetail = new PentionerDetail();
            var token = await HttpContext.GetTokenAsync("access_token");

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string loc = env == "dev" ? "localhost:46731" : "20.204.190.194";

            var request = new HttpRequestMessage(HttpMethod.Get, $"http://{loc}/api/pensionerdetail?Aadhar=" + aadhar);
           

            var client = _contextFactory.CreateClient();

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",HttpContext.Session.GetString("token"));

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                pentionerDetail = await response.Content.ReadFromJsonAsync<PentionerDetail>();

                return pentionerDetail;
            }
            else
            {
                return pentionerDetail;
            }
        }

        [NonAction]
        private PensionDetail CalculatePension(PentionerDetail pentionerDetail)
        {
            PensionDetail pensionDetail = new PensionDetail();

            if (pentionerDetail.PensionType == "self")
            {
                pensionDetail.PensionAmount = 0.8 * pentionerDetail.SalaryEarned + pentionerDetail.Allowences;
                pensionDetail.BankServiceCharge = pentionerDetail.BankCategory == "private" ? 550 : 500;
            }
            else if (pentionerDetail.PensionType == "family")
            {
                pensionDetail.PensionAmount = 0.5 * pentionerDetail.SalaryEarned + pentionerDetail.Allowences;
                pensionDetail.BankServiceCharge = pentionerDetail.BankCategory == "private" ? 600 : 500;
            }

            return pensionDetail;
        }
    }
}
