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

namespace YourProject.Services
{
    public class AssessPeopleService
     {
        private readonly DBcontext objDB;
        private readonly HttpClient Client;
        private static string Token;
        private static DateTime TokenExpiry;

        public AssessPeopleService(IConfiguration configuration)
        { 
             objDB = new DBcontext(configuration);
             Client = new HttpClient(); 
        }

        public async Task<AuthenticationResCls> GetToken()
        {
            ////Get Token form Client Server via API Call.  
            //DBmodel creds = objDB.DBretrieve();
            //if (creds == null || string.IsNullOrEmpty(creds.client_id))
            //{
            //    throw new Exception("Credentails not found in the database.");
            //}

            //string url = "https://www.assesspeople.com/SG/SGTH/authenticate";
            //var requestBody = new
            //{
            //    client_id = creds.client_id,
            //    client_secret = creds.client_secret
            //};

            //string jsonBody = JsonConvert.SerializeObject(requestBody);
            //var apiResult = ApiCall(url, "POST", "", jsonBody);
            //if (!apiResult.IsSuccess)
            //{
            //    Console.WriteLine($"Authentication failed: {apiResult.ErrorMessage}");
            //    return null;
            //}

            //var authResponse = JsonConvert.DeserializeObject<AuthenticationResCls>(apiResult.Response);
            //return authResponse?.access_token;

            //Generating Token ourself for validations.
            string ClientID = "01BE4050133921441D1BFAA103333B4CCEC2";
            string ClientSecret = "0x59e2fc4054022e17505688ecd8b740c4db39ed8f5ec287b8af43602bd39ae3";
            string url = "https://www.assesspeople.com/SG/SGTH/authenticate";

            int tokenValiditySeconds = 3600; // 1 hour 
            // If token is still valid, return existing one
            if (!string.IsNullOrEmpty(Token) && DateTime.Now < TokenExpiry)
            {
                return new AuthenticationResCls
                {
                    access_token = Token,
                    expires_in = (int)(TokenExpiry - DateTime.Now).TotalSeconds
                };
            }

            using (HttpClient client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; 
                byte[] client_byte = Encoding.UTF8.GetBytes(ClientID);
                var clientid_mod = Convert.ToBase64String(client_byte);
                byte[] clientsecret_byte = Encoding.UTF8.GetBytes(ClientSecret);
                var clientsecret_mod = Convert.ToBase64String(clientsecret_byte);
                string credentials = clientid_mod + ":" + clientsecret_mod;
                string authorization = Convert.ToBase64String(Encoding.Default.GetBytes(credentials)); 
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + authorization);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")); 
                var postData = new StringContent($"grant_type=client_credentials&client_id={ClientID}&client_secret={ClientSecret}",Encoding.UTF8,"application/x-www-form-urlencoded");
                try
                {
                    await client.PostAsync(url, postData);

                    //Token Generation.  
                    Token = GenerateGuidToken();
                    TokenExpiry = DateTime.Now.AddSeconds(tokenValiditySeconds); 
                    string GenerateGuidToken()
                    {
                        return Guid.NewGuid().ToString("N");
                    }

                    return new AuthenticationResCls
                    {
                        access_token = Token,
                        expires_in = tokenValiditySeconds
                    };
                }
                catch (Exception ex)
                {
                    return new AuthenticationResCls
                    {
                        access_token = $"Error: {ex.Message}",
                        expires_in = 0
                    };
                }
            }
        } 
          
        public List<FetchAssessmentResCls> FetchAssessmentTests(string accessToken)
        {

            //Retrieving FetchedAssessmentTest form Local DB
            //DBcontext objDB = new DBcontext();
            var fetchedAssessment = objDB.FetchAssessmentTestsFromDb();   
            return fetchedAssessment;

            ////Retrieving FetchedAssessmentTest form Client Server via API Call. 
            //string url = "https://www.assesspeople.com/SG/SGTH/AssessmentTest?category=SG";
            //var apiResult = ApiCall(url, "GET", accessToken);
            //if (!apiResult.IsSuccess)
            //{
            //    Console.WriteLine("Failed to fetch assessment tests: {apiResult.ErrorMessage}");
            //    return new List<FetchAssessmentResCls>();
            //}
            //var FetchedAssesment = JsonConvert.DeserializeObject<List<FetchAssessmentResCls>>(apiResult.Response);
            //return FetchedAssesment;
        }

        public GenerateAssessmentLinkResCls GenerateAssessmentLink(string accessToken, string accountCode, int noOfUsers)
        {
            string url = "https://www.assesspeople.com/SG/SGTH/GenerateIds";
            var requestBody = new
            {
                Accountcode = accountCode,
                NoofUsers = noOfUsers.ToString()
            };
            string jsonBody = JsonConvert.SerializeObject(requestBody);
            var apiResult = ApiCall(url, "POST", accessToken, jsonBody);

            if (!apiResult.IsSuccess)
            {
                Console.WriteLine($"Failed to generate assessment link: {apiResult.ErrorMessage}");
                return null;
            }
            var AssessmentLink = JsonConvert.DeserializeObject<GenerateAssessmentLinkResCls>(apiResult.Response);
            return AssessmentLink;
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
