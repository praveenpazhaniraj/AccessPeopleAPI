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
            AuthenticationResCls objRes = new AuthenticationResCls();

            //Validate incoming credentials 
            if (client_id != "01BE4050133921441D1BFAA103333B4CCEC2"||client_secret != "0x59e2fc4054022e17505688ecd8b740c4db39ed8f5ec287b8af43602bd39ae3")
            {
                objRes.access_token = "";
                objRes.expires_in = 0;
                objRes.error = "Invalid client_id or client_secret";
                return objRes;
            }

            // If existing token is valid, return it
            if (!string.IsNullOrEmpty(Token) && DateTime.Now < TokenExpiry)
            {
                objRes.access_token = Token;
                objRes.expires_in = (int)(TokenExpiry - DateTime.Now).TotalSeconds;
                return objRes;
            }

            // Generate new token
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                Token = Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "");
            } 
            TokenExpiry = DateTime.Now.AddSeconds(3600);

            objRes.access_token = Token;
            objRes.expires_in = 3600;

            return objRes;
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
            {
                throw new UnauthorizedAccessException("Invalid or expired token."); 
            }

            var users = await objDB.GenerateUserIdsWithPasswordsAsync(accountCode, noOfUsers); 

            return new GenerateAssessmentLinkResCls
            {
                UserTable = users
            };
        }
         
        // To Get Candidate Results here
        public async Task<CandidateResultResCls> GetCandidateResultAsync(string userCode)
        {
            var result = await objDB.GetCandidateResultFromDBAsync(userCode);
            return result; 
        }

        //public async Task<WebhookRes> WebHook(string userCode)
        //{
        //    //Here We getting candidate result from DB
        //    var candidateResult = await objDB.GetCandidateResultFromDBAsync(userCode);

        //    // If test not completed → DO NOT send webhook
        //    if (candidateResult.Response == null ||candidateResult.Response.Count == 0 ||candidateResult.Response[0].ResponseCode != "1")
        //    {
        //        Console.WriteLine("Webhook not triggered - Test not yet started or not completed");
        //    } 

        //    WebhookReq ObjReq = new WebhookReq(); 
        //    ObjReq.Metadata = JsonConvert.SerializeObject(candidateResult); //Pass DB result into Metadata
        //    foreach (var score in candidateResult.CandidateScore)
        //    { 
        //        ObjReq.Score = Convert.ToInt32(score.CandidateScore);
        //        ObjReq.Total = Convert.ToInt32(score.MaxScore);
        //    }
        //    ObjReq.ReportLink = "http://WWW.ASSESSPEOPLE.COM/assessmentv6/admin/SAINTGOBAINREPORTS.ASP?UserCode=SGFN004073&DCode=Current";
        //    ObjReq.AssessmentPartnerType = "AccessPeople";
        //    ObjReq.AssessmentStatus = candidateResult.Response[0].ResponseCode;
        //    ObjReq.AssessmentInviteId = userCode; 

        //    string postData = JsonConvert.SerializeObject(ObjReq);
        //    string url = "https://api.turbohire.co/api/assessments/result";
        //    var apiResult = ApiCall(url, "POST", postData);
        //    if (apiResult != null)
        //    {
        //        Console.WriteLine("Failed to Send WebHook");
        //    }
        //    var WebhookRes = JsonConvert.DeserializeObject<WebhookRes>(apiResult);
        //    return WebhookRes;
        //}

        public async Task<WebhookRes> WebHook(string userCode)
        {
            WebhookRes Res = new WebhookRes();

            try
            {
                var candidateResult = await objDB.GetCandidateResultFromDBAsync(userCode);

                if (candidateResult.Response == null ||candidateResult.Response.Count == 0 ||candidateResult.Response[0].ResponseCode != "4")
                {
                    Res.Status = "Failed";
                    Res.Message = "Test not completed";
                    Res.MoreInfo = "Webhook not triggered because candidate test is not finished.";
                    Res.ErrorCode = "TEST_NOT_COMPLETED";
                    return Res;
                }

                WebhookReq ObjReq = new WebhookReq();

                Metadata meta = new Metadata();
                meta.testModule = new TestModule();
                meta.testModule.scores = new List<Dictionary<string, object>>();

                int index = 1;
                int totalScore = 0;
                int totalMax = 0;

                foreach (var score in candidateResult.CandidateScore)
                {
                    var scoreDict = new Dictionary<string, object>();

                    scoreDict.Add($"Score{index}Value", Convert.ToInt32(score.CandidateScore));
                    scoreDict.Add($"Score{index}Label", score.Module);

                    meta.testModule.scores.Add(scoreDict);

                    totalScore += Convert.ToInt32(score.CandidateScore);
                    totalMax += Convert.ToInt32(score.MaxScore);

                    index++;
                }

                meta.testModule.count = meta.testModule.scores.Count;
                 
                ObjReq.Metadata = JsonConvert.SerializeObject(meta);

                ObjReq.Score = totalScore;
                ObjReq.Total = totalMax;
                ObjReq.ReportLink = "http://WWW.ASSESSPEOPLE.COM/assessmentv6/admin/SAINTGOBAINREPORTS.ASP?UserCode=" + userCode + "&DCode=Current";
                ObjReq.AssessmentPartnerType = "Assesspeople";
                ObjReq.AssessmentStatus = candidateResult.Response[0].ResponseCode;
                ObjReq.AssessmentInviteId = userCode;

                string postData = JsonConvert.SerializeObject(ObjReq);

                string url = "https://api.turbohire.co/api/assessments/result";

                var apiResult = ApiCall(url, "POST", postData);

                Res.Status = "Success";
                Res.Message = "Webhook triggered successfully";
                Res.MoreInfo = apiResult;
                Res.ErrorCode = "0";

                return Res;
            }
            catch (Exception ex)
            {
                Res.Status = "Error";
                Res.Message = "Webhook failed";
                Res.MoreInfo = ex.Message;
                Res.ErrorCode = "500";

                return Res;
            }
        }
        private string ApiCall(string url, string method, string postData)
        {
            string responseFromServer = "";

            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol =SecurityProtocolType.Tls |SecurityProtocolType.Tls11 |SecurityProtocolType.Tls12;

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                webRequest.Method = method.ToUpper();
                webRequest.Accept = "application/json";
                webRequest.ContentType = "application/json";

                if (!string.IsNullOrEmpty(postData))
                {
                    using (StreamWriter requestWriter = new StreamWriter(webRequest.GetRequestStream()))
                    {
                        requestWriter.Write(postData);
                    }
                }
                using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
                {
                    using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
                    {
                        responseFromServer = responseReader.ReadToEnd();
                    }
                }
            }
            catch (WebException e)
            {
                var response = (HttpWebResponse)e.Response;
                if (response != null)
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        responseFromServer = rd.ReadToEnd();
                    }
                }
                if (responseFromServer == "")
                {
                    responseFromServer = e.Message;
                }
            }
            return responseFromServer;
        }

         

    }
}
