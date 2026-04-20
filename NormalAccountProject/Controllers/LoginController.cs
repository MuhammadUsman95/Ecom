using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace NormalAccountProject.Controllers
{
    [Route("[controller]/[action]")]
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ══════════════════════════════════════════
        //  POST /Login/Verify
        // ══════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> Verify(string userId, string password)
        {
            string connectionString = _configuration.GetConnectionString("Connection1");
            string sql = "SELECT COUNT(*) FROM Tbl_Users WHERE UserId = @UserId AND Password = @Password";
            int count = 0;

            using (var con = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Password", password);
                await con.OpenAsync();
                count = (int)await cmd.ExecuteScalarAsync();
            }

            if (count > 0)
                return Json(new { success = true, userId = userId, password = password });
            else
                return Json(new { success = false, message = "Invalid username or password." });
        }

        // ══════════════════════════════════════════
        //  POST /Login/LayoutLoad
        // ══════════════════════════════════════════
        [HttpPost]
        public IActionResult LayoutLoad([FromBody] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Json(new { loggedIn = false });

            return Json(new { loggedIn = true, userId = userId });
        }

        // ══════════════════════════════════════════
        //  POST /Login/Logout
        // ══════════════════════════════════════════
        [HttpPost]
        public IActionResult Logout([FromBody] string userId)
        {
            return Json(new { success = true });
        }

        // ══════════════════════════════════════════
        //  GET /Login/GetMenu?userId=admin
        //  MenuType:
        //    0 = Direct (form name shown as-is)
        //    1 = Setup  (grouped under Setup)
        //    2 = Transaction (grouped under Transaction)
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetMenu(string userId = "admin")
        {
            var menuItems = new List<object>();

            try
            {
                string connectionString = _configuration.GetConnectionString("Connection1");

                using var con = new SqlConnection(connectionString);
                using var cmd = new SqlCommand("User_SP", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@nCategoryId", 0);
                cmd.Parameters.AddWithValue("@nsCategoryId", 1);
                cmd.Parameters.AddWithValue("@UserId", userId);

                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    // ✅ MenuType bhi read karo
                    int menuType = 0;
                    if (reader.GetOrdinal("MenuType") >= 0 &&
                        reader["MenuType"] != DBNull.Value)
                        menuType = Convert.ToInt32(reader["MenuType"]);

                    menuItems.Add(new
                    {
                        menuId = reader["MenuId"] != DBNull.Value ? Convert.ToInt32(reader["MenuId"]) : 0,
                        menuName = reader["MenuName"] != DBNull.Value ? reader["MenuName"].ToString() : "",
                        menuUrl = reader["MenuUrl"] != DBNull.Value ? reader["MenuUrl"].ToString() : "#",
                        menuIcon = reader["MenuIcon"] != DBNull.Value ? reader["MenuIcon"].ToString() : "fas fa-circle",
                        menuType = menuType   // 0 = Direct, 1 = Setup, 2 = Transaction
                    });
                }

                return Json(new { success = true, data = menuItems });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ══════════════════════════════════════════
        //  GET /Login/GetCompanyInfo
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetCompanyInfo()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection1");

                using var con = new SqlConnection(connectionString);
                using var cmd = new SqlCommand("User_SP", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@nCategoryId", 0);
                cmd.Parameters.AddWithValue("@nsCategoryId", 2);
                cmd.Parameters.AddWithValue("@UserId", "");

                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return Json(new
                    {
                        success = true,
                        companyName = reader["Company"] != DBNull.Value ? reader["Company"].ToString() : "Ecommerce Admin",
                        companyLogo = reader["Companylogo"] != DBNull.Value ? reader["Companylogo"].ToString() : "logo.png"
                    });
                }

                return Json(new { success = false });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
