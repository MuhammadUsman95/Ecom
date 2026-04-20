using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace NormalAccountProject.Controllers
{
    public class PurchaseInvoiceDetailModel
    {
        public int ProductCode { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
    }

    public class PurchaseInvoiceSaveModel
    {
        public string DocumentNo { get; set; }
        public int VendorCode { get; set; }
        public string Date { get; set; }
        public string Remarks { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public List<PurchaseInvoiceDetailModel> Details { get; set; }
    }

    [Route("[controller]")]
    [ApiController]
    public class PurchaseInvoiceController : Controller
    {
        private readonly IConfiguration _configuration;

        public PurchaseInvoiceController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("nLoadPurchaseInvoiceNo")]
        public async Task<IActionResult> nLoadPurchaseInvoiceNo()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection1");
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Inv_PurchaseInvoiceSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 0);
                    await con.OpenAsync();
                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        string documentNo = "";
                        if (await dr.ReadAsync()) documentNo = dr[0].ToString();
                        return Ok(new { statusId = 1, documentNo });
                    }
                }
            }
            catch (Exception ex) { return Ok(new { statusId = 0, message = "Error: " + ex.Message }); }
        }

        [HttpPost("nLoadPurchaseInvoiceVendorDropDown")]
        public async Task<IActionResult> nLoadPurchaseInvoiceVendorDropDown()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection1");
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Inv_PurchaseInvoiceSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 1);
                    await con.OpenAsync();
                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        var vendors = new List<object>();
                        while (await dr.ReadAsync())
                            vendors.Add(new
                            {
                                vendorCode = dr["VendorCode"].ToString(),
                                vendorName = dr["VendorName"].ToString()
                            });
                        return Ok(new { statusId = 1, vendors });
                    }
                }
            }
            catch (Exception ex) { return Ok(new { statusId = 0, message = "Error: " + ex.Message }); }
        }

        [HttpPost("nLoadPurchaseInvoiceProductDropDown")]
        public async Task<IActionResult> nLoadPurchaseInvoiceProductDropDown()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection1");
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Inv_PurchaseInvoiceSP", con))
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
                                rate = dr["Rate"].ToString(),
                                discountAmount = dr["DiscountAmount"].ToString()
                            });
                        return Ok(new { statusId = 1, products });
                    }
                }
            }
            catch (Exception ex) { return Ok(new { statusId = 0, message = "Error: " + ex.Message }); }
        }

        [HttpPost("nSavePurchaseInvoice")]
        public async Task<IActionResult> nSavePurchaseInvoice([FromBody] PurchaseInvoiceSaveModel model)
        {
            try
            {
                if (model == null)
                    return Ok(new { statusId = 0, message = "Payload null aa raha hai - JSON binding issue" });

                if (model.Details == null || model.Details.Count == 0)
                    return Ok(new { statusId = 0, message = "Details empty hain - frontend se details nahi aa rahi" });

                // Build XML
                var ci = CultureInfo.InvariantCulture;
                string detailXml = "<Details>";
                foreach (var d in model.Details)
                {
                    detailXml += "<Detail>" +
                        "<ProductCode>" + d.ProductCode + "</ProductCode>" +
                        "<Qty>" + d.Qty.ToString("F2", ci) + "</Qty>" +
                        "<Rate>" + d.Rate.ToString("F2", ci) + "</Rate>" +
                        "<Amount>" + d.Amount.ToString("F2", ci) + "</Amount>" +
                        "<DiscountAmount>" + d.DiscountAmount.ToString("F2", ci) + "</DiscountAmount>" +
                        "<NetAmount>" + d.NetAmount.ToString("F2", ci) + "</NetAmount>" +
                        "</Detail>";
                }
                detailXml += "</Details>";

                string connectionString = _configuration.GetConnectionString("Connection1");
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Inv_PurchaseInvoiceSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 3);
                    cmd.Parameters.AddWithValue("@DocumentNo", model.DocumentNo ?? "");
                    cmd.Parameters.AddWithValue("@VendorCode", model.VendorCode);
                    cmd.Parameters.AddWithValue("@Date", model.Date ?? "");
                    cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? "");
                    cmd.Parameters.AddWithValue("@TotalQty", model.TotalQty);
                    cmd.Parameters.AddWithValue("@TotalAmount", model.TotalAmount);
                    cmd.Parameters.AddWithValue("@TotalDiscountAmount", model.TotalDiscountAmount);
                    cmd.Parameters.AddWithValue("@NetAmount", model.NetAmount);

                    var xmlParam = cmd.Parameters.Add("@DetailXml", SqlDbType.Xml);
                    xmlParam.Value = detailXml;

                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    return Ok(new { statusId = 1, message = "Purchase Invoice saved successfully." });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }
    }
}
