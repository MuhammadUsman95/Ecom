using Microsoft.AspNetCore.Mvc;
using NormalAccountProject.Models;
using System.Data;
using System.Data.SqlClient;
using static NormalAccountProject.Controllers.DashboardController;

namespace NormalAccountProject.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CustomerRegistrationController : Controller
    {
        private readonly IConfiguration _configuration;
        private string connectionString;

        public CustomerRegistrationController(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("Connection1");
        }

        [HttpPost("nLoadCustomerRegistrationData")]
        public async Task<IActionResult> nLoadCustomerRegistrationData([FromBody] nInfoTab nInfoTabObj)
        {
            try
            {
                var response = new
                {
                    statusId = 1,
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    statusId = 0,
                    message = "Error: " + ex.Message
                });
            }
        }

        [HttpPost("nSaveCustomerRegistrationData")]
        public async Task<IActionResult> nSaveCustomerRegistrationData([FromBody] CustomerTab nCustomerTabObj)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection1");

                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Customer_SP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 0);
                    cmd.Parameters.AddWithValue("@Customer", nCustomerTabObj.Customer);
                    cmd.Parameters.AddWithValue("@ContactNo", nCustomerTabObj.ContactNo);
                    cmd.Parameters.AddWithValue("@IsActive", nCustomerTabObj.IsActive == true ? "1" : "0");
                    cmd.Parameters.AddWithValue("@Type", nCustomerTabObj.Type);
                    cmd.Parameters.AddWithValue("@UserId", nCustomerTabObj.Userid);

                    await con.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                            return Ok(new
                            {
                                statusId = dr["StatusId"],
                                message = dr["MessageCaption"].ToString()
                            });
                        }
                    }
                }

                return Ok(new
                {
                    statusId = 0,
                    message = "No response from database"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    statusId = 0,
                    message = "Error: " + ex.Message
                });
            }
        }


    }
}
