using Microsoft.AspNetCore.Mvc;
using NormalAccountProject.Models;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;

namespace NormalAccountProject.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ProductInfoController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;

        public ProductInfoController(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("Connection1");
        }

        // ─────────────────────────────────────────────────────────────
        // Load Category Dropdown  →  nsCategoryId = 1
        // ─────────────────────────────────────────────────────────────
        [HttpPost("nLoadProductInfoData")]
        public async Task<IActionResult> nLoadProductInfoData([FromBody] nInfoTab nInfoTabObj)
        {
            try
            {
                var categoryParams = new Dictionary<string, object>
                {
                    { "@nCategoryId",  0 },
                    { "@nsCategoryId", 1 }
                };
                var nCategoryList = await nGetDataAsync<CategoryDD>("Inv_ProductsSP", categoryParams);
                return Ok(new { statusId = 1, CategoryList = nCategoryList });
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Save / Update Product  →  nsCategoryId = 0
        // ─────────────────────────────────────────────────────────────
        [HttpPost("nSaveProductInfoData")]
        public async Task<IActionResult> nSaveProductInfoData([FromBody] ProductInfoTab obj)
        {
            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(obj.Product))
                return Ok(new { statusId = 0, message = "Product name is required" });

            if (string.IsNullOrWhiteSpace(obj.CategoryId))
                return Ok(new { statusId = 0, message = "Category is required" });

            if (obj.SalesPrice <= 0)
                return Ok(new { statusId = 0, message = "Sales price must be greater than 0" });

            if (obj.PurchaseRate < 0)
                return Ok(new { statusId = 0, message = "Purchase rate cannot be negative" });

            if (obj.DiscountAmount < 0)
                return Ok(new { statusId = 0, message = "Discount amount cannot be negative" });

            try
            {
                using SqlConnection con = new(connectionString);
                await con.OpenAsync();

                // ── Compute next ProductCode before calling SP (for INSERT only)
                int productCode = obj.ProductCode;
                if (!obj.IsUpdate)
                {
                    using SqlCommand codeCmd = new(
                        "SELECT ISNULL(MAX(CAST(ProductCode AS INT)), 0) + 1 FROM Inv_ProductsTab", con);
                    productCode = Convert.ToInt32(await codeCmd.ExecuteScalarAsync());
                }

                using SqlCommand cmd = new("Inv_ProductsSP", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@nCategoryId", 0);
                cmd.Parameters.AddWithValue("@nsCategoryId", 0);
                cmd.Parameters.AddWithValue("@ProductCode", productCode);
                cmd.Parameters.AddWithValue("@Product", obj.Product);
                cmd.Parameters.AddWithValue("@SalesPrice", obj.SalesPrice);
                cmd.Parameters.AddWithValue("@DiscountAmount", obj.DiscountAmount);
                cmd.Parameters.AddWithValue("@PurchaseRate", obj.PurchaseRate);
                cmd.Parameters.AddWithValue("@CategoryId", obj.CategoryId);
                cmd.Parameters.AddWithValue("@IsActive", obj.IsActive ? 1 : 0);
                cmd.Parameters.AddWithValue("@IsUpdate", obj.IsUpdate ? 1 : 0);
                cmd.Parameters.AddWithValue("@UserId", obj.Userid ?? "");
                cmd.Parameters.AddWithValue("@EditUserId", obj.Userid ?? "");

                using SqlDataReader dr = await cmd.ExecuteReaderAsync();

                // INSERT path → SP returns 2 result sets:
                //   RS1: next ProductCode (skip — already computed above)
                //   RS2: StatusId / MessageCaption
                if (!obj.IsUpdate)
                    await dr.NextResultAsync();

                // UPDATE path → SP returns 1 result set: StatusId / MessageCaption
                if (await dr.ReadAsync())
                {
                    int statusId = Convert.ToInt32(dr["StatusId"]);
                    return Ok(new { statusId, message = dr["MessageCaption"]?.ToString() });
                }

                return Ok(new { statusId = 0, message = "No response from database" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save Error: {ex.Message}\n{ex.StackTrace}");
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Load Grid  →  nsCategoryId = 2
        // ─────────────────────────────────────────────────────────────
        [HttpPost("nLoadGridViewData")]
        public async Task<IActionResult> nLoadGridViewData([FromBody] nInfoTab nInfoTabObj)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@nCategoryId",  0 },
                    { "@nsCategoryId", 2 }
                };
                var nDataList = await nGetDataAsync<ExpandoObject>("Inv_ProductsSP", parameters);
                return Ok(new { statusId = 1, GridViewDataList = nDataList });
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Delete Product  →  nsCategoryId = 3
        // ─────────────────────────────────────────────────────────────
        [HttpPost("nDeleteProductInfoData")]
        public async Task<IActionResult> nDeleteProductInfoData([FromBody] ProductInfoDeleteRequest req)
        {
            try
            {
                using SqlConnection con = new(connectionString);
                using SqlCommand cmd = new("Inv_ProductsSP", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@nCategoryId", 0);
                cmd.Parameters.AddWithValue("@nsCategoryId", 3);
                cmd.Parameters.AddWithValue("@ProductCode", req.ProductCode);
                cmd.Parameters.AddWithValue("@UserId", req.Userid ?? "");

                await con.OpenAsync();
                using SqlDataReader dr = await cmd.ExecuteReaderAsync();

                if (await dr.ReadAsync())
                {
                    int statusId = Convert.ToInt32(dr["StatusId"]);
                    return Ok(new { statusId, message = dr["MessageCaption"]?.ToString() ?? "" });
                }

                return Ok(new { statusId = 0, message = "No response from database" });
            }
            catch (SqlException sqlEx)
            {
                return Ok(new { statusId = 0, message = "Database Error: " + sqlEx.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete Error: {ex.Message}\n{ex.StackTrace}");
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Generic DB Fetcher
        // ─────────────────────────────────────────────────────────────
        public async Task<List<T>> nGetDataAsync<T>(
            string storedProcedure,
            Dictionary<string, object> parameters) where T : new()
        {
            List<T> list = new();

            using SqlConnection con = new(connectionString);
            using SqlCommand cmd = new(storedProcedure, con)
            {
                CommandType = CommandType.StoredProcedure
            };

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
            else
            {
                var props = typeof(T).GetProperties();
                while (await dr.ReadAsync())
                {
                    T obj = new();
                    foreach (var prop in props)
                    {
                        if (!dr.HasColumn(prop.Name) || dr[prop.Name] == DBNull.Value) continue;
                        prop.SetValue(obj, Convert.ChangeType(dr[prop.Name], prop.PropertyType));
                    }
                    list.Add(obj);
                }
            }

            return list;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Models
    // ─────────────────────────────────────────────────────────────

    /// <summary>Payload for Save / Update</summary>
    public class ProductInfoTab
    {
        public int ProductCode { get; set; }
        public string? Product { get; set; }
        public decimal SalesPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PurchaseRate { get; set; }
        public string? CategoryId { get; set; }
        public bool IsActive { get; set; }
        public bool IsUpdate { get; set; }
        public string? Userid { get; set; }
    }

    /// <summary>Payload for Delete</summary>
    public class ProductInfoDeleteRequest
    {
        public int ProductCode { get; set; }
        public string? Userid { get; set; }
    }
}
