using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using static AccessPeople.Models.AccessPeopleModels;
using Newtonsoft.Json;
using System.Linq;

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
                    string query = "SELECT AccountName, AccountCode FROM AssessmentTestsAPI"; 
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        con.Open();  
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (await reader.ReadAsync())  
                            {
                                assessmentTests.Add(new FetchAssessmentResCls
                                {
                                    Account_Name = reader["AccountName"].ToString(),  
                                    Account_Code = reader["AccountCode"].ToString()  
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

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            await Task.Run(() => da.Fill(ds)); 
                        }
                    }
                }

                // ✅ Check data exists
                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return OjbRes;
                }

                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        int totalScore = 0; int totalMaxScore = 0; int totalTimeTaken = 0;

                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            // ✅ Status Handling
                            int statusCode = dr["StatusCode"] != DBNull.Value ? Convert.ToInt32(dr["StatusCode"]) : 0;

                            Response res = new Response
                            {
                                ResponseCode = statusCode.ToString()
                            };
                            OjbRes.Response.Add(res);

                            // ❌ If NOT completed → stop immediately
                            if (statusCode != 4)
                            {
                                return OjbRes;
                            }

                            // ✅ Safe column reads
                            string module = dr.Table.Columns.Contains("ModuleName") ? dr["ModuleName"]?.ToString() : "";
                            int timeGiven = dr.Table.Columns.Contains("TimeGiven") && dr["TimeGiven"] != DBNull.Value ? Convert.ToInt32(dr["TimeGiven"]) : 0;
                            int timeRemain = dr.Table.Columns.Contains("TimeRemain") && dr["TimeRemain"] != DBNull.Value ? Convert.ToInt32(dr["TimeRemain"]) : 0;
                            int candidateScore = dr.Table.Columns.Contains("TotalQtnsCorrectlyAnswered") && dr["TotalQtnsCorrectlyAnswered"] != DBNull.Value ? Convert.ToInt32(dr["TotalQtnsCorrectlyAnswered"]) : 0;
                            int maxScore = dr.Table.Columns.Contains("TotalQtns") && dr["TotalQtns"] != DBNull.Value ? Convert.ToInt32(dr["TotalQtns"]) : 0;
                            string percentage = dr.Table.Columns.Contains("Percentage") && dr["Percentage"] != DBNull.Value ? dr["Percentage"].ToString() : "0";

                            // ✅ Build Score Object
                            CandidateScoreRes Score = new CandidateScoreRes
                            {
                                Module = module,
                                TimeTaken_MaxTime = (timeGiven - timeRemain).ToString(),
                                CandidateScore = candidateScore.ToString(),
                                MaxScore = maxScore.ToString(),
                                Percentage = percentage
                            };

                            OjbRes.CandidateScore.Add(Score);

                            // ✅ Totals calculation
                            totalScore += candidateScore;
                            totalMaxScore += maxScore;
                            totalTimeTaken += (timeGiven - timeRemain);

                        }

                        // ✅ Add ONLY ONE total record (FIXED)
                        TotalScore Tot = new TotalScore
                        {
                            TotalCandidateScore = totalScore.ToString(),
                            TotalMaxScore = totalMaxScore.ToString(),
                            TotalPercentage = totalMaxScore == 0 ? "0" : ((totalScore * 100) / totalMaxScore).ToString(),
                            GrantTotalTime = totalTimeTaken.ToString(),
                            GrantTotalTimePercentage = totalTimeTaken == 0 ? "0" : ((totalTimeTaken * 100) / (totalTimeTaken == 0 ? 1 : totalTimeTaken)).ToString()
                        };

                        OjbRes.TotalScore.Add(Tot);

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