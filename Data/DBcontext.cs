using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using static AccessPeople.Models.AccessPeopleModels;
using Newtonsoft.Json;
namespace AccessPeople.Data
{
    public class DBcontext
    {
        private readonly string connectionString;

        public DBcontext(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }
         
        public async Task<List<FetchAssessmentResCls>> FetchAssessmentTestsFromDbAsync()
        {  
            List<FetchAssessmentResCls> assessmentTests = new List<FetchAssessmentResCls>(); 
            try
            {
                DataSet ds = new DataSet();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "SELECT Account_Name, Account_Code FROM AssessmentTestsAPI"; 
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(ds);
                    }
                }

                string strjson = JsonConvert.SerializeObject(ds.Tables[0]);
                assessmentTests = JsonConvert.DeserializeObject<List<FetchAssessmentResCls>>(strjson);
              
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB Error: {ex.Message}");
                throw;
            }

            return assessmentTests;
        }

        public async Task<List<GenerateAssessmentUser>> GenerateUserIdsWithPasswordsAsync(string accountCode, string noOfUsers)
        {
            if (!int.TryParse(noOfUsers, out int unitsToGenerate) || unitsToGenerate <= 0)
            {
                throw new ArgumentException("noOfUsers must be a valid positive integer.");
            } 

            List<GenerateAssessmentUser> users = new List<GenerateAssessmentUser>();
            try
            {
                DataSet ds = new DataSet();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("GenerateUserIdsWithPasswords", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = accountCode;
                        cmd.Parameters.Add("@UnitsToBeGenerated", SqlDbType.Int).Value = unitsToGenerate;
                        SqlDataAdapter da = new SqlDataAdapter();
                        da.SelectCommand = cmd;
                        da.Fill(ds);
                    }
                }

                string strjson = JsonConvert.SerializeObject(ds.Tables[0]);
                users = JsonConvert.DeserializeObject<List<GenerateAssessmentUser>>(strjson); 
            }
            catch (SqlException ex)
            {
                // LOG IT HERE
                throw new Exception("Database error occurred while generating user IDs.", ex);
            }

            return users;
             
        }

        public async Task<CandidateResultResCls> GetCandidateResultFromDBAsync(string userCode)
        {
            CandidateResultResCls OjbRes = new CandidateResultResCls();
            OjbRes.CandidateScore = new List<CandidateScoreRes>();
            OjbRes.Response = new List<Response>();
            OjbRes.TotalScore = new List<TotalScore>();

            try
            {
                DataSet ds = new DataSet();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("GetCandidateResultSaintGobain", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@UserCode", SqlDbType.NVarChar, 50).Value = userCode;
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(ds);
                    }
                }

                OjbRes.Response.Add(new Response { ResponseCode = "200" });

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    int totalCandidateScore = 0; int totalMaxScore = 0; int totalTimeGiven = 0; int totalTimeRemain = 0;

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        //Canditate Score
                        CandidateScoreRes objScr = new CandidateScoreRes();
                        objScr.Module = row["ModuleName"].ToString();
                        objScr.TimeTaken_MaxTime = $"{row["TimeGiven"]}/{row["TimeRemain"]}"; // adjust if needed
                        objScr.CandidateScore = row["TotalQtnsCorrectlyAnswered"].ToString();
                        objScr.MaxScore = row["TotalQtns"].ToString();
                        objScr.Percentage = row["Percentage"].ToString();
                        OjbRes.CandidateScore.Add(objScr);

                        // Aggregate totals
                        totalCandidateScore += Convert.ToInt32(row["TotalQtnsCorrectlyAnswered"]);
                        totalMaxScore += Convert.ToInt32(row["TotalQtns"]);
                        totalTimeGiven += Convert.ToInt32(row["TimeGiven"]);
                        totalTimeRemain += Convert.ToInt32(row["TimeRemain"]);

                        TotalScore objTotal = new TotalScore
                        {
                            TotalCandidateScore = totalCandidateScore.ToString(),
                            TotalMaxScore = totalMaxScore.ToString(),
                            TotalPercentage = totalMaxScore > 0 ? ((totalCandidateScore * 100) / totalMaxScore).ToString() : "0",
                            GrantTotalTime = $"{totalTimeGiven}/{totalTimeRemain} min",
                            GrantTotalTimePercentage = totalTimeGiven + totalTimeRemain > 0 ? ((totalTimeGiven * 100) / (totalTimeGiven + totalTimeRemain)).ToString() : "0"
                        };

                        OjbRes.TotalScore.Add(objTotal);
                    }
                }
                else
                {
                    // Optionally, handle "no results" scenario
                }

            }
            catch (SqlException ex)
            {
                throw new Exception("Database error while fetching candidate result.", ex);
            }

            return OjbRes;
        } 
    }
}