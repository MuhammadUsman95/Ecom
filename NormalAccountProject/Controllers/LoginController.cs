using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Threading.Tasks;

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
            {
                // Return success + credentials from server (for storage)
                return Json(new
                {
                    success = true,
                    userId = userId,       // future encryption possible
                    password = password    // future encryption possible
                });
            }
            else
            {
                return Json(new { success = false, message = "Invalid username or password." });
            }
        }

        [HttpPost]
        public IActionResult LayoutLoad([FromBody] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Json(new { loggedIn = false });

            string companyName = "Smart Solutions Ltd";
            string branchName = "Main Branch";

            return Json(new
            {
                loggedIn = true,
                userId = userId,
            });
        }

        [HttpPost]
        public IActionResult Logout([FromBody] string userId)
        {
            // Success response
            return Json(new { success = true });
        }
    }
}
