using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace NormalAccountProject.Controllers
{
    // ── Request Models ──
    public class ExpenseDetailModel
    {
        public int ExpenseTypeId { get; set; }
        public decimal Amount { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class ExpenseSaveModel
    {
        public string ExpenseNo { get; set; }
        public int CustomerCode { get; set; }
        public string Date { get; set; }
        public decimal TotalAmount { get; set; }
        public List<ExpenseDetailModel> Details { get; set; }
    }

    [Route("[controller]")]
    [ApiController]
    public class ExpenseController : Controller
    {
        private readonly IConfiguration _configuration;

        public ExpenseController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ── Load Expense Voucher Number ──
        [HttpPost("nLoadExpenseVoucherNo")]
        public async Task<IActionResult> nLoadExpenseVoucherNo()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection1");
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Inv_ExpenseSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 0);

                    await con.OpenAsync();
                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        string expenseNo = "";
                        if (await dr.ReadAsync())
                        {
                            expenseNo = dr[0].ToString();
                        }

                        return Ok(new
                        {
                            statusId = 1,
                            expenseNo = expenseNo
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        // ── Load Customer Dropdown ──
        [HttpPost("nLoadExpenseCustomerDropDown")]
        public async Task<IActionResult> nLoadExpenseCustomerDropDown()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection1");
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Inv_ExpenseSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 1);

                    await con.OpenAsync();
                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        var customers = new List<object>();
                        while (await dr.ReadAsync())
                        {
                            customers.Add(new
                            {
                                customerCode = dr["CustomerCode"].ToString(),
                                customerName = dr["CustomerName"].ToString()
                            });
                        }

                        return Ok(new
                        {
                            statusId = 1,
                            customers = customers
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        // ── Load Expense Type Dropdown ──
        [HttpPost("nLoadExpenseTypeDropDown")]
        public async Task<IActionResult> nLoadExpenseTypeDropDown()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection1");
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Inv_ExpenseSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 2);

                    await con.OpenAsync();
                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        var expenseTypes = new List<object>();
                        while (await dr.ReadAsync())
                        {
                            expenseTypes.Add(new
                            {
                                expenseTypeId = dr["ExpenseTypeId"].ToString(),
                                expenseType = dr["ExpenseType"].ToString()
                            });
                        }

                        return Ok(new
                        {
                            statusId = 1,
                            expenseTypes = expenseTypes
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        // ── Save Expense (Master + Detail in ONE database call via XML) ──
        [HttpPost("nSaveExpense")]
        public async Task<IActionResult> nSaveExpense([FromBody] ExpenseSaveModel model)
        {
            try
            {
                // ── Build XML from detail rows ──
                string detailXml = "<Details>";
                foreach (var detail in model.Details)
                {
                    detailXml += "<Detail>" +
                        "<ExpenseTypeId>" + detail.ExpenseTypeId + "</ExpenseTypeId>" +
                        "<Amount>" + detail.Amount + "</Amount>" +
                        "<DiscountAmount>" + detail.DiscountAmount + "</DiscountAmount>" +
                        "</Detail>";
                }
                detailXml += "</Details>";

                string connectionString = _configuration.GetConnectionString("Connection1");
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Inv_ExpenseSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 3);
                    cmd.Parameters.AddWithValue("@ExpenseNo", model.ExpenseNo);
                    cmd.Parameters.AddWithValue("@CustomerCode", model.CustomerCode);
                    cmd.Parameters.AddWithValue("@Date", model.Date);
                    cmd.Parameters.AddWithValue("@TotalAmount", model.TotalAmount);
                    cmd.Parameters.AddWithValue("@DetailXml", detailXml);

                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    return Ok(new { statusId = 1, message = "Expense saved successfully." });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }
    }
}
