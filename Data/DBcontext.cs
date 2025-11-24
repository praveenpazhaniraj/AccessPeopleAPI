using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using static AccessPeople.Models.AccessPeopleModels;

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
            var assessmentTests = new List<FetchAssessmentResCls>();
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT Account_Name, Account_Code FROM AssessmentTestsAPI";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
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

            var users = new List<GenerateAssessmentUser>();
            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("GenerateUserIdsWithPasswords", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = accountCode;
            cmd.Parameters.Add("@UnitsToBeGenerated", SqlDbType.Int).Value = unitsToGenerate;

            try
            {
                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    users.Add(new GenerateAssessmentUser
                    {
                        AccountCode = reader["AccountCode"]?.ToString(),
                        UserCode = reader["UserCode"]?.ToString(),
                        Password = reader["Password"]?.ToString(), 
                    });
                }
            }
            catch (SqlException ex)
            {
                // LOG IT HERE
                throw new Exception("Database error occurred while generating user IDs.", ex);
            }

            return users;
             
        }  

    } 
}
