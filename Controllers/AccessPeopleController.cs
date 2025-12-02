using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

        public AssessPeopleController(AssessPeopleService service)
        {
            obj = service;
        }


        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody] LoginRequest request)
        { 
            var tokenResponse = obj.GenerateTokenForClient(request.client_id, request.client_secret);

            if (!string.IsNullOrEmpty(tokenResponse.error))
            {
                return Unauthorized(tokenResponse); 
            }

            return Ok(tokenResponse);
        }


        [HttpGet("FetchAssessmentTests")]
        public async Task<IActionResult> FetchAssessmentTests()
        { 
            //Read Authorization header
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { error = "Missing or invalid Authorization header" });
            }

            string token = authHeader.Substring("Bearer ".Length).Trim(); 
            try
            {
                //Await the async service method
                var tests = await obj.FetchAssessmentTestsAsync(token);
                return Ok(tests);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpPost("GenerateAssessmentLink")]
        public async Task<IActionResult> GenerateAssessmentLink([FromBody] GenerateAssessmentLinkReqCls request)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { error = "Missing or invalid Authorization header" }); 
            } 
            string token = authHeader.Substring("Bearer ".Length).Trim();

            if (request == null || string.IsNullOrEmpty(request.Accountcode) || request.NoofUsers <= 0)
            {
                return BadRequest(new { message = "Invalid request body" });
            }
            try
            {
                var response = await obj.GenerateAssessmentLinkAsync(token, request.Accountcode, request.NoofUsers.ToString());

                if (response == null || response.UserTable.Count == 0)
                    return BadRequest(new { error = "Failed to generate User Deatils." });

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        //[HttpPost("GetCandidateResult")]
        //public async Task<IActionResult> GetCandidateResult([FromBody] CandidateResultReqCls request)
        //{
        //    // Validate authorization header
        //    var authHeader = Request.Headers["Authorization"].ToString();
        //    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        //    {
        //        return Unauthorized(new { error = "Missing or invalid Authorization header" }); 
        //    }

        //    string token = authHeader.Substring("Bearer ".Length).Trim();

        //    // Validate request body
        //    if (request == null || string.IsNullOrEmpty(request.UserCode))
        //    {
        //        return BadRequest(new { message = "UserCode is required" }); 
        //    }

        //    try
        //    {
        //        // Call service method (your token validation should happen inside GetCandidateResultAsync)
        //        var response = await obj.GetCandidateResultAsync(token, request.UserCode);

        //        if (response == null)
        //            return NotFound(new { error = "No results found for this UserCode" });

        //        // Return proper JSON  
        //        return Ok(response);
        //    }
        //    catch (UnauthorizedAccessException ex)
        //    {
        //        return Unauthorized(new { error = ex.Message });
        //    } 
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { error = ex.Message });
        //    }
        //}



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
