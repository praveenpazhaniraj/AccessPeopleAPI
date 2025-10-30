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
         
        public class AuthenticationResCls
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
        }
        public class DBmodel
        {
            public string client_id { get; set; }
            public string client_secret { get; set; }
        }

        public class FetchAssessmentResCls
        {
            public string Account_Name { get; set; }
            public string Account_Code { get; set; }
        }
         
        public class GenerateAssessmentLinkReqCls
        {
            public string Accountcode { get; set; }
            public string NoofUsers { get; set; }
        }
         
        public class GenerateAssessmentLinkResCls
        {
            public List<GenerateAssessmentUser> Table1 { get; set; }
        }

        public class GenerateAssessmentUser
        {
            public string UserCode { get; set; }
            public string Password { get; set; }
            public string AccountCode { get; set; }
        } 
        public class WebhookRes
        {
            public string Status { get; set; }
            public string Message { get; set; }
            public string MoreInfo { get; set; }
            public string ErrorCode { get; set; }
        }
    }
}
