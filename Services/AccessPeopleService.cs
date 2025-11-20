using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System;
using static AccessPeople.Models.AccessPeopleModels;
using AccessPeople.Data;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Data.SqlClient;
using System.Data;

namespace YourProject.Services
{
    public class AssessPeopleService
    {
        private readonly DBcontext objDB;    
        private readonly HttpClient _client; 
        private static string Token = "";
        private static DateTime TokenExpiry;
        public AssessPeopleService(DBcontext db, HttpClient client)
        {
            objDB = db;
            _client = client;
        }
        public AuthenticationResCls GenerateTokenForClient(string client_id, string client_secret)
        {
            //Validate incoming credentials 
            if (client_id != "01BE4050133921441D1BFAA103333B4CCEC2"||client_secret != "0x59e2fc4054022e17505688ecd8b740c4db39ed8f5ec287b8af43602bd39ae3")
            {
                return new AuthenticationResCls
                {
                    access_token = "",
                    expires_in = 0,
                    error = "Invalid client_id or client_secret"
                };
            }
            // If existing token is valid, return it
            if (!string.IsNullOrEmpty(Token) && DateTime.Now < TokenExpiry)
            {
                return new AuthenticationResCls
                {
                    access_token = Token,
                    expires_in = (int)(TokenExpiry - DateTime.Now).TotalSeconds
                };
            }

            // Generate new token
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                Token = Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "");
            } 
            TokenExpiry = DateTime.Now.AddSeconds(3600); 

            return new AuthenticationResCls
            {
                access_token = Token,
                expires_in = 3600
            }; 
        } 
        public static bool IsValidToken(string token)
        {
            return !string.IsNullOrEmpty(Token) && token == Token && DateTime.Now < TokenExpiry;
        }

        public async Task<List<FetchAssessmentResCls>> FetchAssessmentTestsAsync(string accessToken)
        {
            if (!IsValidToken(accessToken))
            {
                throw new UnauthorizedAccessException("Invalid or expired token."); 
            } 

            // Retrieve from local DB 
            var tests = await objDB.FetchAssessmentTestsFromDbAsync();
            return tests;
        }
          
        public async Task<GenerateAssessmentLinkResCls> GenerateAssessmentLinkAsync(string token, string accountCode, string noOfUsers)
        {
            if (!IsValidToken(token))
                throw new UnauthorizedAccessException("Invalid or expired token.");

            var users = await objDB.GenerateUserIdsWithPasswordsAsync(accountCode, noOfUsers);

            return new GenerateAssessmentLinkResCls
            {
                UserTable = users
            };
        }

        public WebhookRes WebHook()
        { 
            string url = "https://api.turbohire.co/api/assessments/result";
            var requestBody = new
            {
                AssessmentPartnerType = "AssessPeople",
                AssessmentInviteId = "",
                AssessmentStatus = "ongiong",
                Score = 0,
                Total = 0,
                ReportLink = "",
                Metadata = "",
            };
            string jsonBody = JsonConvert.SerializeObject(requestBody);
            var apiResult = ApiCall(url, "POST", "", jsonBody);
            if (!apiResult.IsSuccess)
            {
                Console.WriteLine($"Failed to Send WebHook: {apiResult.ErrorMessage}");
                return null;
            }
            var WebhookRes = JsonConvert.DeserializeObject<WebhookRes>(apiResult.Response);  
            return WebhookRes;
        }

        public ApiResult ApiCall(string url, string method, string accessToken = "", string jsonBody = "")
        {
            ApiResult result = new ApiResult();
            HttpWebResponse response = null;
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                webRequest.Method = method.ToUpper();
                webRequest.Accept = "application/json";
                webRequest.Timeout = 30000; // 30 seconds timeout

                if (!string.IsNullOrEmpty(accessToken))
                {
                    webRequest.Headers["Authorization"] = "Bearer " + accessToken;
                }

                if (method.ToUpper() == "POST")
                {
                    webRequest.ContentType = "application/json";
                    if (!string.IsNullOrEmpty(jsonBody))
                    {
                        using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
                        {
                            streamWriter.Write(jsonBody);
                        }
                    }
                }

                response = (HttpWebResponse)webRequest.GetResponse();
                result.StatusCode = response.StatusCode;
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    result.Response = reader.ReadToEnd();
                }

                result.IsSuccess = (response.StatusCode == HttpStatusCode.OK);
            }
            catch (WebException ex)
            {
                // This catches both network errors and non-200 responses
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    result.StatusCode = errorResponse.StatusCode;
                    using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        result.ErrorMessage = reader.ReadToEnd();
                    }
                }
                else
                {
                    result.ErrorMessage = ex.Message;
                }

                result.IsSuccess = false;
                Console.WriteLine($"[ApiCall] WebException: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                Console.WriteLine($"[ApiCall] General Exception: {ex.Message}");
            }
            finally
            {
                response?.Close();
            }

            return result;
        }
    }
}
