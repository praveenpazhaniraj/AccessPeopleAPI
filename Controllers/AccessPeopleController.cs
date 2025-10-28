using Microsoft.AspNetCore.Mvc;
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
        private readonly AssessPeopleService _service;

        public AssessPeopleController()
        {
            _service = new AssessPeopleService();
        }

        [HttpPost("GetToken")]
        public IActionResult GetToken()
        {
            string token = _service.GetAuthenticationToken();
            if (string.IsNullOrEmpty(token))
                return BadRequest("Failed to retrieve token.");
            return Ok(new { access_token = token });
        }

        [HttpGet("FetchAssesmentTests")]
        public IActionResult FetchTests([FromHeader(Name = "Authorization")] string authHeader)
        {
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return BadRequest("Missing or invalid Authorization header.");

            string token = authHeader.Replace("Bearer ", "").Trim();
            var tests = _service.FetchAssessmentTests(token);
            return Ok(tests);
        }

        [HttpPost("GenerateAssesmentLink")]
        public IActionResult GenerateLink([FromHeader(Name = "Authorization")] string authHeader, [FromBody] GenerateAssessmentLinkReqCls request)
        {
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return BadRequest("Missing or invalid Authorization header.");

            string token = authHeader.Replace("Bearer ", "").Trim();

            var response = _service.GenerateAssessmentLink(token, request.Accountcode, int.Parse(request.NoofUsers));
            if (response == null)
                return BadRequest("Failed to generate assessment link.");

            return Ok(response);
        }
    }

}
