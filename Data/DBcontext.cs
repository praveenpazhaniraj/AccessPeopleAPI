using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

        public DBmodel DBretrieve()
        {
            DBmodel objdb = new DBmodel();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "SELECT TOP 1 client_id, client_secret FROM ClientApiDetails";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            objdb = new DBmodel
                            {
                                client_id = reader["client_id"]?.ToString(),
                                client_secret = reader["client_secret"]?.ToString()
                            };
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error: {ex.Message}");
                throw new Exception("Database connection or query failed.", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }

            return objdb;
        }

        public List<FetchAssessmentResCls> FetchAssessmentTestsFromDb()
        {
            List<FetchAssessmentResCls> assessmentTests = new List<FetchAssessmentResCls>();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Query to fetch the assessment test data from the database
                    string query = "SELECT Account_Name, Account_Code FROM AssessmentTestsAPI";  

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
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
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error: {ex.Message}");
                throw new Exception("Database connection or query failed.", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }

            return assessmentTests;
        }
    }
}
