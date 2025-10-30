using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YourProject.Services;
using static AccessPeople.Models.AccessPeopleModels;

namespace AccessPeople.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AssessPeopleController : ControllerBase
    {
        private readonly AssessPeopleService obj;

        public AssessPeopleController(IConfiguration configuration)
        {
            obj = new AssessPeopleService(configuration);
        }

        [HttpPost("GetToken")]
        public IActionResult GetToken()
        {
            string token = obj.GetAuthenticationToken();
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Failed to retrieve token."); 
            }
            return Ok(new { access_token = token });
        }

        [HttpGet("FetchAssesmentTests")]
        public IActionResult FetchTests([FromHeader(Name = "Authorization")] string authHeader)
        {
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return BadRequest("Missing or invalid Authorization header."); 
            } 
            string token = authHeader.Replace("Bearer ", "").Trim();
            var tests = obj.FetchAssessmentTests(token);
            return Ok(tests);
        }

        [HttpPost("GenerateAssesmentLink")]
        public IActionResult GenerateLink([FromHeader(Name = "Authorization")] string authHeader, [FromBody] GenerateAssessmentLinkReqCls request)
        {
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return BadRequest("Missing or invalid Authorization header."); 
            } 

            string token = authHeader.Replace("Bearer ", "").Trim(); 
            var response = obj.GenerateAssessmentLink(token, request.Accountcode, int.Parse(request.NoofUsers));
            if (response == null)
            {
                return BadRequest("Failed to generate assessment link."); 
            } 
            return Ok(response);
        }

        [HttpPost("Webhook")]
        public IActionResult Webhook()
        {
            var response = obj.WebHook();
            if (string.IsNullOrEmpty(response.ToString()))
            {
                return BadRequest("Failed to retrieve Webhook."); 
            }
            return Ok(response);
        }
    }

}
