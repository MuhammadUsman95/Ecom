using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text.Json;

namespace NormalAccountProject.Controllers
{
    public class SaleInvoiceDetailModel
    {
        public int ProductCode { get; set; }
        public decimal Qty { get; set; }
        public decimal SalesPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
    }

    public class SaleInvoiceSaveModel
    {
        public string InvoiceNo { get; set; }
        public int CustomerCode { get; set; }
        public string Date { get; set; }
        public string Remarks { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal NetAmount { get; set; }
        public List<SaleInvoiceDetailModel> Details { get; set; }
    }

    [Route("[controller]")]
    [ApiController]
    public class SaleInvoiceController : Controller
    {
        private readonly IConfiguration _configuration;

        public SaleInvoiceController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("nLoadSaleInvoiceNo")]
        public async Task<IActionResult> nLoadSaleInvoiceNo()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection1");
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Inv_SaleInvoiceSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 0);
                    await con.OpenAsync();
                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        string invoiceNo = "";
                        if (await dr.ReadAsync()) invoiceNo = dr[0].ToString();
                        return Ok(new { statusId = 1, invoiceNo });
                    }
                }
            }
            catch (Exception ex) { return Ok(new { statusId = 0, message = "Error: " + ex.Message }); }
        }

        [HttpPost("nLoadSaleInvoiceCustomerDropDown")]
        public async Task<IActionResult> nLoadSaleInvoiceCustomerDropDown()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection1");
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Inv_SaleInvoiceSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 1);
                    await con.OpenAsync();
                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        var customers = new List<object>();
                        while (await dr.ReadAsync())
                            customers.Add(new
                            {
                                customerCode = dr["CustomerCode"].ToString(),
                                customerName = dr["CustomerName"].ToString()
                            });
                        return Ok(new { statusId = 1, customers });
                    }
                }
            }
            catch (Exception ex) { return Ok(new { statusId = 0, message = "Error: " + ex.Message }); }
        }

        [HttpPost("nLoadSaleInvoiceProductDropDown")]
        public async Task<IActionResult> nLoadSaleInvoiceProductDropDown()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection1");
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Inv_SaleInvoiceSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 2);
                    await con.OpenAsync();
                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        var products = new List<object>();
                        while (await dr.ReadAsync())
                            products.Add(new
                            {
                                productCode = dr["ProductCode"].ToString(),
                                productName = dr["Product"].ToString(),
                                salesPrice = dr["SalesPrice"].ToString(),
                                discountAmount = dr["DiscountAmount"].ToString()
                            });
                        return Ok(new { statusId = 1, products });
                    }
                }
            }
            catch (Exception ex) { return Ok(new { statusId = 0, message = "Error: " + ex.Message }); }
        }

        // ── DEBUG endpoint - payload check karo ──
        [HttpPost("nDebugPayload")]
        public IActionResult nDebugPayload([FromBody] SaleInvoiceSaveModel model)
        {
            if (model == null)
                return Ok(new { statusId = 0, message = "model is NULL - JSON binding failed" });

            return Ok(new
            {
                statusId = 1,
                invoiceNo = model.InvoiceNo,
                customerCode = model.CustomerCode,
                date = model.Date,
                totalAmount = model.TotalAmount,
                detailCount = model.Details?.Count ?? 0,
                details = model.Details
            });
        }

        [HttpPost("nSaveSaleInvoice")]
        public async Task<IActionResult> nSaveSaleInvoice([FromBody] SaleInvoiceSaveModel model)
        {
            try
            {
                // ── Null / empty check ──
                if (model == null)
                    return Ok(new { statusId = 0, message = "Payload null aa raha hai - JSON binding issue" });

                if (model.Details == null || model.Details.Count == 0)
                    return Ok(new { statusId = 0, message = "Details empty hain - frontend se details nahi aa rahi" });

                // ── Build XML ──
                var ci = CultureInfo.InvariantCulture;
                string detailXml = "<Details>";
                foreach (var d in model.Details)
                {
                    detailXml += "<Detail>" +
                        "<ProductCode>" + d.ProductCode + "</ProductCode>" +
                        "<Qty>" + d.Qty.ToString("F2", ci) + "</Qty>" +
                        "<SalesPrice>" + d.SalesPrice.ToString("F2", ci) + "</SalesPrice>" +
                        "<Amount>" + d.Amount.ToString("F2", ci) + "</Amount>" +
                        "<DiscountAmount>" + d.DiscountAmount.ToString("F2", ci) + "</DiscountAmount>" +
                        "<NetAmount>" + d.NetAmount.ToString("F2", ci) + "</NetAmount>" +
                        "</Detail>";
                }
                detailXml += "</Details>";

                string connectionString = _configuration.GetConnectionString("Connection1");
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Inv_SaleInvoiceSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 3);
                    cmd.Parameters.AddWithValue("@InvoiceNo", model.InvoiceNo ?? "");
                    cmd.Parameters.AddWithValue("@CustomerCode", model.CustomerCode);
                    cmd.Parameters.AddWithValue("@Date", model.Date ?? "");
                    cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? "");
                    cmd.Parameters.AddWithValue("@TotalAmount", model.TotalAmount);
                    cmd.Parameters.AddWithValue("@TotalDiscount", model.TotalDiscount);
                    cmd.Parameters.AddWithValue("@NetAmount", model.NetAmount);

                    // SqlDbType.Xml explicitly - NVarChar nahi
                    var xmlParam = cmd.Parameters.Add("@DetailXml", SqlDbType.Xml);
                    xmlParam.Value = detailXml;

                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    return Ok(new { statusId = 1, message = "Invoice saved successfully." });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }
    }
}
