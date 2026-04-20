using Microsoft.AspNetCore.Mvc;
using NormalAccountProject.Models;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;

namespace NormalAccountProject.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class StockViewController : Controller
    {
        private readonly IConfiguration _configuration;
        private string connectionString;

        public StockViewController(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("Connection1");
        }

        [HttpPost("nLoadStockViewData")]
        public async Task<IActionResult> nLoadStockViewData([FromBody] nInfoTab nInfoTabObj)
        {
            try
            {
                List<ExpandoObject> nDataList = await nGetStockDataAsync();

                return Ok(new
                {
                    statusId = 1,
                    StockDataList = nDataList
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stock Load Error: {ex.Message}");
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        private async Task<List<ExpandoObject>> nGetStockDataAsync()
        {
            List<ExpandoObject> list = new();

            using SqlConnection con = new SqlConnection(connectionString);

            // Query: ProductCode, Product, Category, SalesPrice, DiscountAmount, Stock
            // If Category comes from a join, adjust accordingly e.g.:
            // JOIN Inv_CategoryTab c ON p.CategoryId = c.CategoryId
            string query = @"
                SELECT 
                 p.ProductCode,
                 p.Product,
                 c.Category,
                 p.SalesPrice,
                 p.DiscountAmount,
                 254 AS Stock
             FROM Inv_ProductsTab p
             LEFT JOIN Ecom_CategoryTab c ON p.CategoryId = c.CategoryId
             ORDER BY c.Category, p.Product";

            // NOTE: If Category is already a column in Inv_ProductsTab (not a join), use this simpler query:
            // string query = @"SELECT ProductCode, Product, Category, SalesPrice, DiscountAmount, 254 AS Stock
            //                  FROM Inv_ProductsTab ORDER BY Category, Product";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.CommandType = CommandType.Text;

            await con.OpenAsync();
            using SqlDataReader dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                IDictionary<string, object> expando = new ExpandoObject();
                for (int i = 0; i < dr.FieldCount; i++)
                    expando[dr.GetName(i)] = dr.IsDBNull(i) ? null : dr.GetValue(i);
                list.Add((ExpandoObject)expando);
            }

            return list;
        }
    }
}
