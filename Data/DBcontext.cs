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
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "SELECT Account_Name, Account_Code FROM AssessmentTestsAPI"; 
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        con.Open();  
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (await reader.ReadAsync())  
                            {
                                assessmentTests.Add(new FetchAssessmentResCls
                                {
                                    Account_Name = reader["Account_Name"].ToString(),  
                                    Account_Code = reader["Account_Code"].ToString()  
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB Error:" + ex.Message);
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
            string StaticTestURL = "https://www.assesspeople.com/assessmentv6/Test";

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

                foreach (var user in users)
                {
                    user.AccountCode = accountCode;
                    user.TestURL = StaticTestURL;
                }
            }
            catch (SqlException ex)
            { 
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
                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    { 
                        int totalScore = 0;int totalMaxScore = 0;int totalTimeGiven = 0;int totalTimeRemain = 0; int totalTimeTaken = 0; int timeGiven = 0; int timeRemain = 0; int candidateScore = 0;int maxScore = 0;
                         
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            int statusCode = Convert.ToInt32(dr["StatusCode"]);
                            string message = dr["Message"].ToString();

                            Response res = new Response();
                            res.ResponseCode = statusCode.ToString();
                            OjbRes.Response.Add(res);

                            if (statusCode == 0)
                            { 
                                Console.WriteLine(message);
                            }

                            CandidateScoreRes Score = new CandidateScoreRes();
                            Score.Module = dr["ModuleName"].ToString();
                            timeGiven = Convert.ToInt32(dr["TimeGiven"]);
                            timeRemain = Convert.ToInt32(dr["TimeRemain"]);
                            Score.TimeTaken_MaxTime = (timeGiven - timeRemain).ToString();

                            candidateScore = dr["TotalQtnsCorrectlyAnswered"] == DBNull.Value? 0 : Convert.ToInt32(dr["TotalQtnsCorrectlyAnswered"]);
                            maxScore = dr["TotalQtns"] == DBNull.Value? 0 : Convert.ToInt32(dr["TotalQtns"]);

                            Score.CandidateScore = candidateScore.ToString();
                            Score.MaxScore = maxScore.ToString();
                            Score.Percentage = dr["Percentage"] == DBNull.Value? "0": dr["Percentage"].ToString();
                            OjbRes.CandidateScore.Add(Score);

                            totalScore += candidateScore;
                            totalMaxScore += maxScore;
                            totalTimeGiven += timeGiven;
                            totalTimeRemain += timeRemain;

                            TotalScore Tot = new TotalScore();
                            Tot.TotalCandidateScore = totalScore.ToString();
                            Tot.TotalMaxScore = totalMaxScore.ToString();
                            Tot.TotalPercentage = dr["Percentage"].ToString(); //((totalScore * 100) / totalMaxScore).ToString();
                            totalTimeTaken = totalTimeGiven - totalTimeRemain;
                            Tot.GrantTotalTime = totalTimeTaken.ToString();
                            Tot.GrantTotalTimePercentage = ((totalTimeTaken * 100) / totalTimeGiven).ToString();
                            OjbRes.TotalScore.Add(Tot); 
                        } 
                    }
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