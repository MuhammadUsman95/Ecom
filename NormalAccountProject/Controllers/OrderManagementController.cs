using Microsoft.AspNetCore.Mvc;
using NormalAccountProject.Models;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Text.Json;

namespace NormalAccountProject.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class OrderManagementController : Controller
    {
        private readonly IConfiguration _configuration;
        private string connectionString;

        public OrderManagementController(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("Connection1");
        }

        [HttpPost("nLoadCustomerRegistrationData")]
        public async Task<IActionResult> nLoadCustomerRegistrationData([FromBody] Models.nInfoTab nInfoTabObj)
        {
            try
            {
                return Ok(new { statusId = 1 });
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        [HttpPost("nLoadGridViewData")]
        public async Task<IActionResult> nLoadGridViewData([FromBody] Models.nInfoTab nInfoTabObj)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@nType", 0 },
                    { "@nsType", 2 }
                };

                List<ExpandoObject> nDataList = await nGetDataAsync<ExpandoObject>("EcomOrder_SP", parameters);

                return Ok(new { statusId = 1, OrderList = nDataList });
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        [HttpPost("nUpdateOrderStatus")]
        public async Task<IActionResult> nUpdateOrderStatus([FromBody] JsonElement body)
        {
            try
            {
                string orderNo = body.GetProperty("OrderNo").GetString();
                string orderStatus = body.GetProperty("OrderStatus").GetString();

                using SqlConnection con = new SqlConnection(connectionString);
                using SqlCommand cmd = new SqlCommand("EcomOrder_SP", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@nType", 0);
                cmd.Parameters.AddWithValue("@nsType", 3);
                cmd.Parameters.AddWithValue("@OrderStatus", orderStatus);
                cmd.Parameters.AddWithValue("@OrderNo", orderNo);

                await con.OpenAsync();

                using SqlDataReader dr = await cmd.ExecuteReaderAsync();

                int statusId = 0;
                string message = "Something went wrong.";

                if (await dr.ReadAsync())
                {
                    statusId = dr["StatusId"] != DBNull.Value ? Convert.ToInt32(dr["StatusId"]) : 0;
                    message = dr["MessageCaption"] != DBNull.Value ? dr["MessageCaption"].ToString() : "";
                }

                if (statusId == 1)
                    return Ok(new { statusId = 1, message = "Status updated successfully." });
                else
                    return Ok(new { statusId = 0, message = string.IsNullOrEmpty(message) ? "Update failed." : message });
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        public async Task<List<T>> nGetDataAsync<T>(string storedProcedure, Dictionary<string, object> parameters) where T : new()
        {
            List<T> list = new();

            using SqlConnection con = new SqlConnection(connectionString);
            using SqlCommand cmd = new SqlCommand(storedProcedure, con);
            cmd.CommandType = CommandType.StoredProcedure;

            if (parameters != null)
            {
                foreach (var param in parameters)
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }

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
            else
            {
                var props = typeof(T).GetProperties();
                while (await dr.ReadAsync())
                {
                    T obj = new T();
                    foreach (var prop in props)
                    {
                        if (!dr.HasColumn(prop.Name) || dr[prop.Name] == DBNull.Value)
                            continue;
                        prop.SetValue(obj, Convert.ChangeType(dr[prop.Name], prop.PropertyType));
                    }
                    list.Add(obj);
                }
            }

            return list;
        }
    }
}