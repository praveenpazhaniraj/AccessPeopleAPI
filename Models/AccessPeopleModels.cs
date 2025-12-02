using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessPeople.Models
{
    public class AccessPeopleModels
    {
        public class ApiResult
        {
            public bool IsSuccess { get; set; }
            public string Response { get; set; }
            public string ErrorMessage { get; set; }
            public System.Net.HttpStatusCode StatusCode { get; set; }
        }

        #region "Authentication"
        public class AuthenticationResCls
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string error { get; set; }
        }

        public class LoginRequest
        {
            public string client_id { get; set; }
            public string client_secret { get; set; }
        }
        #endregion

        #region "FetchAssessment"

        public class FetchAssessmentResCls
        {
            public string Account_Name { get; set; }
            public string Account_Code { get; set; }
        }

        #endregion

        #region "GenerateAssessmentLink"
        public class GenerateAssessmentLinkReqCls
        {
            public string Accountcode { get; set; }
            public int NoofUsers { get; set; }
        }

        public class GenerateAssessmentLinkResCls
        {
            public List<GenerateAssessmentUser> UserTable { get; set; }
        }

        public class GenerateAssessmentUser
        {
            public string UserCode { get; set; }
            public string Password { get; set; }
            public string AccountCode { get; set; }
        }
        #endregion

        #region "CandidateResult" 
        public class CandidateResultReqCls
        {
            public string UserCode { get; set; }
        }
        public class CandidateResultResCls
        {
            public List<Response> Response { get; set; }
            public List<CandidateScoreRes> CandidateScore { get; set; }
            public List<TotalScore> TotalScore { get; set; }
        }
        public class Response
        {
            public string ResponseCode { get; set; }
        }
        public class CandidateScoreRes
        {
            public string Module { get; set; }
            public string TimeTaken_MaxTime { get; set; }
            public string CandidateScore { get; set; }
            public string MaxScore { get; set; }
            public string Percentage { get; set; }
        }

        public class TotalScore
        {
            public string TotalCandidateScore { get; set; }
            public string TotalMaxScore { get; set; }
            public string TotalPercentage { get; set; }
            public string GrantTotalTime { get; set; }
            public string GrantTotalTimePercentage { get; set; }
        }
        #endregion
         
        #region "WebHook"
        public class WebhookReq
        {
            public string AssessmentPartnerType { get; set; }
            public string AssessmentInviteId { get; set; }
            public string AssessmentStatus { get; set; }
            public int Score { get; set; }
            public int Total { get; set; }
            public string ReportLink { get; set; }
            public string Metadata { get; set; }
        }

        public class WebhookRes
        {
            public string Status { get; set; }
            public string Message { get; set; }
            public string MoreInfo { get; set; }
            public string ErrorCode { get; set; }
        }
        #endregion 

    } 
}
