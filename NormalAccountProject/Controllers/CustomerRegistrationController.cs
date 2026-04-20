using Microsoft.AspNetCore.Mvc;
using NormalAccountProject.Models;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;

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

        // ══════════════════════════════════════════════════════════════════
        // LOAD GRID — nsCustomerId = 2
        // ══════════════════════════════════════════════════════════════════
        [HttpPost("nLoadGridViewData")]
        public async Task<IActionResult> nLoadGridViewData([FromBody] nInfoTab nInfoTabObj)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@nCustomerId",  0 },
                    { "@nsCustomerId", 2 }   // 2 = Grid / Select All
                };

                List<ExpandoObject> nDataList =
                    await nGetDataAsync<ExpandoObject>("Inv_CustomerSP", parameters);

                return Ok(new { statusId = 1, GridViewDataList = nDataList });
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // SAVE / UPDATE — nsCustomerId = 0
        // ══════════════════════════════════════════════════════════════════
        [HttpPost("nSaveCustomerData")]
        public async Task<IActionResult> nSaveCustomerData([FromBody] CustomerTab obj)
        {
            ModelState.Clear();

            // ✅ Sirf CustomerName validate karo — CustomerCode SP auto-generate karta hai
            if (string.IsNullOrWhiteSpace(obj.CustomerName))
                return Ok(new { statusId = 0, message = "Customer name is required." });

            try
            {
                using SqlConnection con = new SqlConnection(connectionString);
                using SqlCommand cmd = new SqlCommand("Inv_CustomerSP", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@nCustomerId", 0);
                cmd.Parameters.AddWithValue("@nsCustomerId", 0);                      // 0 = Insert / Update
                cmd.Parameters.AddWithValue("@CustomerCode", obj.CustomerCode ?? ""); // Insert = '', Update = existing code
                cmd.Parameters.AddWithValue("@CustomerName", obj.CustomerName ?? "");
                cmd.Parameters.AddWithValue("@ContactNo", obj.ContactNo ?? "");
                cmd.Parameters.AddWithValue("@IsActive", obj.IsActive ? "1" : "0");
                cmd.Parameters.AddWithValue("@UserId", obj.Userid ?? "");
                cmd.Parameters.AddWithValue("@IsUpdate", obj.IsUpdate ? "1" : "0");
                // ✅ Duplicate @CustomerCode parameter HATAYA gaya

                await con.OpenAsync();

                using SqlDataReader dr = await cmd.ExecuteReaderAsync();
                if (await dr.ReadAsync())
                {
                    // ✅ generatedCode wapas bhejte hain (SP se aata hai)
                    string generatedCode = "";
                    int genOrdinal = -1;
                    try { genOrdinal = dr.GetOrdinal("GeneratedCode"); } catch { }
                    if (genOrdinal >= 0 && !dr.IsDBNull(genOrdinal))
                        generatedCode = dr.GetValue(genOrdinal)?.ToString() ?? "";

                    return Ok(new
                    {
                        statusId = Convert.ToInt32(dr["StatusId"]),
                        message = dr["MessageCaption"]?.ToString() ?? "",
                        generatedCode = generatedCode
                    });
                }

                return Ok(new { statusId = 0, message = "No response from database." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save Error: {ex.Message}");
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // DELETE — nsCustomerId = 3
        // ══════════════════════════════════════════════════════════════════
        [HttpPost("nDeleteCustomerData")]
        public async Task<IActionResult> nDeleteCustomerData([FromBody] CustomerDeleteRequest obj)
        {
            try
            {
                using SqlConnection con = new SqlConnection(connectionString);
                using SqlCommand cmd = new SqlCommand("Inv_CustomerSP", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@nCustomerId", 0);
                cmd.Parameters.AddWithValue("@nsCustomerId", 3);                      // 3 = Delete
                cmd.Parameters.AddWithValue("@UserId", obj.Userid ?? "");
                cmd.Parameters.AddWithValue("@CustomerCode", obj.CustomerCode ?? ""); // ✅ CustomerCode use karo

                await con.OpenAsync();

                using SqlDataReader dr = await cmd.ExecuteReaderAsync();
                if (await dr.ReadAsync())
                {
                    return Ok(new
                    {
                        statusId = Convert.ToInt32(dr["StatusId"]),
                        message = dr["MessageCaption"]?.ToString() ?? ""
                    });
                }

                return Ok(new { statusId = 0, message = "No response from database." });
            }
            catch (SqlException sqlEx)
            {
                return Ok(new { statusId = 0, message = $"Database Error: {sqlEx.Message}" });
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = $"Error: {ex.Message}" });
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // GENERIC DATA FETCHER
        // ══════════════════════════════════════════════════════════════════
        private async Task<List<T>> nGetDataAsync<T>(string storedProcedure,
            Dictionary<string, object> parameters) where T : new()
        {
            List<T> list = new();

            using SqlConnection con = new SqlConnection(connectionString);
            using SqlCommand cmd = new SqlCommand(storedProcedure, con);
            cmd.CommandType = CommandType.StoredProcedure;

            if (parameters != null)
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);

            await con.OpenAsync();
            using SqlDataReader dr = await cmd.ExecuteReaderAsync();

            if (typeof(T) == typeof(ExpandoObject))
            {
                while (await dr.ReadAsync())
                {
                    IDictionary<string, object> expando = new ExpandoObject();
                    for (int i = 0; i < dr.FieldCount; i++)
                        expando[dr.GetName(i)] = dr.IsDBNull(i) ? null : dr.GetValue(i);
                    list.Add((T)expando);
                }
            }

            return list;
        }
    }
}
