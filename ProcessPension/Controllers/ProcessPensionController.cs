using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessPensionService.Models;
using System.Net.Http;
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
            if (pensionInput == null)
            {
                return Content("Invalid pensioner detail provided, please provide valid detail.");
            }

            var result = await GetPensionerDetail(pensionInput.AaadharNumber);
            var penstionDetail =  CalculatePension(result);
            return Ok(penstionDetail);
        }

        [NonAction]
        private async Task<PentionerDetail> GetPensionerDetail(long aadhar)
        {
            PentionerDetail pentionerDetail = new PentionerDetail();

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:46731/api/pensionerdetail?Aadhar="+aadhar);

            var client = _contextFactory.CreateClient();

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
                pensionDetail.BankServiceCharge = pentionerDetail.BankCategory == "private" ? 550 : 500;
            }

            return pensionDetail;
        }
    }
}
