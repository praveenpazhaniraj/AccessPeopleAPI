using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System;
using static AccessPeople.Models.AccessPeopleModels;

namespace YourProject.Services
{
    public class AssessPeopleService
    {  
        public string GetAuthenticationToken()
        {
            string url = "https://www.assesspeople.com/SG/SGTH/authenticate";
            var requestBody = new
            {
                client_id = "01BE4050133921441D1BFAA103333B4CCEC2",
                client_secret = "0x59e2fc4054022e17505688ecd8b740c4db39ed8f5ec287b8af43602bd39ae3"
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);
            var apiResult = ApiCall(url, "POST", "", jsonBody);
            if (!apiResult.IsSuccess)
            {
                Console.WriteLine($"Authentication failed: {apiResult.ErrorMessage}");
                return null;
            }

            var authResponse = JsonConvert.DeserializeObject<AuthenticationResCls>(apiResult.Response);
            return authResponse?.access_token;
        }
        public List<FetchAssessmentResCls> FetchAssessmentTests(string accessToken)
        {
            string url = "https://www.assesspeople.com/SG/SGTH/AssessmentTest?category=SG";
            var apiResult = ApiCall(url, "GET", accessToken);
            if (!apiResult.IsSuccess)
            {
                Console.WriteLine($"Failed to fetch assessment tests: {apiResult.ErrorMessage}");
                return new List<FetchAssessmentResCls>();
            }
            var FetchedAssesment = JsonConvert.DeserializeObject<List<FetchAssessmentResCls>>(apiResult.Response);
            return FetchedAssesment;
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
