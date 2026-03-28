using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NormalAccountProject.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BarcodePrintingController : Controller
    {
        private readonly IConfiguration _configuration;
        private string connectionString;

        public BarcodePrintingController(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("Connection1");
        }

        // Method to load products for barcode generation
        [HttpPost("nLoadBarcodePrintingData")]
        public async Task<IActionResult> nLoadBarcodePrintingData([FromBody] UserRequest userRequest)
        {
            try
            {
                var productList = await GetProductDataAsync();

                if (productList.Count > 0)
                {
                    return Ok(new
                    {
                        statusId = 1,
                        MessageCaption = "Data loaded successfully",
                        ProductList = productList
                    });
                }
                else
                {
                    return Ok(new
                    {
                        statusId = 0,
                        MessageCaption = "No products found."
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    statusId = 0,
                    MessageCaption = "Error: " + ex.Message
                });
            }
        }

        // ✅ FIX: DB column is "Product" not "ProductName" — aliased as ProductName
        private async Task<List<Product>> GetProductDataAsync()
        {
            var products = new List<Product>();

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    // ✅ FIXED: "Product AS ProductName" — your DB column is "Product"
                    string query = "SELECT ProductCode, Product AS ProductName FROM Inv_ProductsTab Where IsActive  = 1 ORDER BY Product";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        await con.OpenAsync();

                        using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                        {
                            while (await dr.ReadAsync())
                            {
                                products.Add(new Product
                                {
                                    ProductCode = dr["ProductCode"]?.ToString() ?? "",
                                    ProductName = dr["ProductName"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching product data: " + ex.Message);
                Console.WriteLine("Stack Trace: " + ex.StackTrace);
            }

            return products;
        }
    }

    // Product model
    public class Product
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
    }

    // UserRequest model
    public class UserRequest
    {
        public string UserId { get; set; }
    }
}
