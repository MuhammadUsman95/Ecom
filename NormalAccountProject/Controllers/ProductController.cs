using Microsoft.AspNetCore.Mvc;
using NormalAccountProject.Models;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Net;

namespace NormalAccountProject.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ProductRegistrationController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;

        public ProductRegistrationController(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("Connection1");
        }

        // ─────────────────────────────────────────────────────────────
        // Load Dropdowns (Category + Vendor)
        // ─────────────────────────────────────────────────────────────
        [HttpPost("nLoadProductRegistrationData")]
        public async Task<IActionResult> nLoadProductRegistrationData([FromBody] nInfoTab nInfoTabObj)
        {
            try
            {
                var categoryParams = new Dictionary<string, object> { { "@nCategoryId", 0 }, { "@nsCategoryId", 1 } };
                var vendorParams = new Dictionary<string, object> { { "@nCategoryId", 0 }, { "@nsCategoryId", 7 } };

                var nCategoryList = await nGetDataAsync<CategoryDD>("Ecom_ProductSP", categoryParams);
                var nVendorList = await nGetDataAsync<VendorDD>("Ecom_ProductSP", vendorParams);

                return Ok(new { statusId = 1, CategoryList = nCategoryList, VendorList = nVendorList });
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Save / Update Product
        // ─────────────────────────────────────────────────────────────
        [HttpPost("nSaveProductRegistrationData")]
        public async Task<IActionResult> nSaveProductRegistrationData([FromBody] ProductTab nProductTabObj)
        {
            ModelState.Clear();

            if (string.IsNullOrEmpty(nProductTabObj.Product))
                return Ok(new { statusId = 0, message = "Product name is required" });

            if (string.IsNullOrEmpty(nProductTabObj.CategoryId))
                return Ok(new { statusId = 0, message = "Category is required" });

            if (string.IsNullOrEmpty(nProductTabObj.VendorId))
                return Ok(new { statusId = 0, message = "Vendor is required" });

            if (string.IsNullOrEmpty(nProductTabObj.Prices))
                return Ok(new { statusId = 0, message = "Price is required" });

            //if (!nProductTabObj.IsUpdate && string.IsNullOrEmpty(nProductTabObj.ProductImageAttachmentfilename))
            //    return Ok(new { statusId = 0, message = "Product image is required" });

            try
            {
                using SqlConnection con = new(connectionString);
                using SqlCommand cmd = new("Ecom_ProductSP", con) { CommandType = CommandType.StoredProcedure };

                cmd.Parameters.AddWithValue("@nCategoryId", 0);
                cmd.Parameters.AddWithValue("@nsCategoryId", 0);
                cmd.Parameters.AddWithValue("@Product", nProductTabObj.Product);
                cmd.Parameters.AddWithValue("@ProductDescription", nProductTabObj.ProductDescription ?? "");
                cmd.Parameters.AddWithValue("@IsActive", nProductTabObj.IsActive ? "1" : "0");
                cmd.Parameters.AddWithValue("@CategoryId", nProductTabObj.CategoryId);
                cmd.Parameters.AddWithValue("@VendorId", nProductTabObj.VendorId);
                cmd.Parameters.AddWithValue("@UserId", nProductTabObj.Userid ?? "");
                cmd.Parameters.AddWithValue("@IsUpdate", nProductTabObj.IsUpdate ? "1" : "0");
                cmd.Parameters.AddWithValue("@ProductImage", nProductTabObj.ProductImageAttachmentfilename ?? "");
                cmd.Parameters.AddWithValue("@Prices", nProductTabObj.Prices);
                cmd.Parameters.AddWithValue("@DiscountAmount", nProductTabObj.DiscountAmount ?? "0");

                if (nProductTabObj.IsUpdate)
                    cmd.Parameters.AddWithValue("@ProductId", nProductTabObj.ProductId);

                await con.OpenAsync();

                using SqlDataReader dr = await cmd.ExecuteReaderAsync();
                if (await dr.ReadAsync())
                {
                    int statusId = Convert.ToInt32(dr["StatusId"]);

                    if (statusId == 1 && !string.IsNullOrEmpty(nProductTabObj.ProductImageAttachmentfilename))
                    {
                        try
                        {
                            if (nProductTabObj.IsUpdate && !string.IsNullOrEmpty(nProductTabObj.ProductImageAttachmentfilenameold))
                                await DeleteFromFtp(nProductTabObj.ProductImageAttachmentfilenameold, nProductTabObj.FtpPath);

                            await UploadToFtp(nProductTabObj.ProductImageAttachmentfilename,
                                              nProductTabObj.ProductImageAttachmentbase64,
                                              nProductTabObj.FtpPath);
                        }
                        catch (Exception ftpEx)
                        {
                            Console.WriteLine($"FTP Warning: {ftpEx.Message}");
                        }
                    }

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
        // Load Grid Data
        // ─────────────────────────────────────────────────────────────
        [HttpPost("nLoadGridViewData")]
        public async Task<IActionResult> nLoadGridViewData([FromBody] nInfoTab nInfoTabObj)
        {
            try
            {
                var parameters = new Dictionary<string, object> { { "@nCategoryId", 0 }, { "@nsCategoryId", 2 } };
                var nDataList = await nGetDataAsync<ExpandoObject>("Ecom_ProductSP", parameters);
                return Ok(new { statusId = 1, GridViewDataList = nDataList });
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Delete Product
        // ─────────────────────────────────────────────────────────────
        [HttpPost("nDeleteProductRegistrationData")]
        public async Task<IActionResult> nDeleteProductRegistrationData([FromBody] ProductDeleteRequest deleteRequest)
        {
            try
            {
                using SqlConnection con = new(connectionString);
                using SqlCommand cmd = new("Ecom_ProductSP", con) { CommandType = CommandType.StoredProcedure };

                cmd.Parameters.AddWithValue("@nCategoryId", 0);
                cmd.Parameters.AddWithValue("@nsCategoryId", 3);
                cmd.Parameters.AddWithValue("@UserId", deleteRequest.Userid ?? "");
                cmd.Parameters.AddWithValue("@ProductId", deleteRequest.ProductId);

                await con.OpenAsync();

                using SqlDataReader dr = await cmd.ExecuteReaderAsync();
                if (await dr.ReadAsync())
                {
                    int statusId = Convert.ToInt32(dr["StatusId"]);
                    string message = dr["MessageCaption"]?.ToString() ?? "";

                    if (statusId == 1 && !string.IsNullOrEmpty(deleteRequest.ProductImageAttachmentfilenameold))
                    {
                        try { await DeleteFromFtp(deleteRequest.ProductImageAttachmentfilenameold, deleteRequest.FtpPath); }
                        catch (Exception ftpEx) { Console.WriteLine($"FTP Delete Warning: {ftpEx.Message}"); }
                    }

                    return Ok(new { statusId, message });
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
        // FTP: Upload
        // ─────────────────────────────────────────────────────────────
        async Task UploadToFtp(string fileName, string base64, string ftpPath)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new Exception("FTP Upload: Filename is empty");
            if (string.IsNullOrWhiteSpace(base64)) throw new Exception("FTP Upload: Base64 is empty");

            string server = _configuration["Config:ftpServer"];
            string user = _configuration["Config:ftpUser"];
            string password = _configuration["Config:ftpPassword"];
            string port = _configuration["Config:ftpPort"];
            string path = !string.IsNullOrWhiteSpace(ftpPath) ? ftpPath : "/wwwroot/Images/ProductRegistration";

            if (base64.Contains(",")) base64 = base64.Split(',')[1];
            byte[] bytes = Convert.FromBase64String(base64);

            string url = string.IsNullOrWhiteSpace(port)
                ? $"ftp://{server}{path}/{fileName}"
                : $"ftp://{server}:{port}{path}/{fileName}";

            FtpWebRequest req = (FtpWebRequest)WebRequest.Create(url);
            req.Method = WebRequestMethods.Ftp.UploadFile;
            req.Credentials = new NetworkCredential(user, password);
            req.UseBinary = true;
            req.UsePassive = false;
            req.KeepAlive = false;
            req.ContentLength = bytes.Length;
            req.Timeout = 30000;

            using Stream stream = await req.GetRequestStreamAsync();
            await stream.WriteAsync(bytes, 0, bytes.Length);

            using FtpWebResponse res = (FtpWebResponse)await req.GetResponseAsync();
            Console.WriteLine($"FTP Upload OK: {res.StatusDescription}");
        }

        // ─────────────────────────────────────────────────────────────
        // FTP: Delete (with existence check)
        // ─────────────────────────────────────────────────────────────
        async Task DeleteFromFtp(string attachmentFileName, string ftpPath)
        {
            if (string.IsNullOrEmpty(attachmentFileName)) return;

            string server = _configuration["Config:ftpServer"];
            string user = _configuration["Config:ftpUser"];
            string password = _configuration["Config:ftpPassword"];
            string port = _configuration["Config:ftpPort"];
            string path = !string.IsNullOrEmpty(ftpPath) ? ftpPath : "/wwwroot/Images/ProductRegistration";

            string fileName = attachmentFileName.Contains("/") || attachmentFileName.Contains("\\")
                ? Path.GetFileName(attachmentFileName)
                : attachmentFileName;

            string url = $"ftp://{server}:{port}{path}/{fileName}";

            if (!await FtpFileExists(url, user, password))
            {
                Console.WriteLine($"FTP Delete Skipped: File not found – {fileName}");
                return;
            }

            try
            {
                FtpWebRequest req = (FtpWebRequest)WebRequest.Create(url);
                req.Method = WebRequestMethods.Ftp.DeleteFile;
                req.Credentials = new NetworkCredential(user, password);
                req.UseBinary = true;
                req.UsePassive = true;
                req.KeepAlive = false;
                req.Timeout = 10000;

                using FtpWebResponse res = (FtpWebResponse)await req.GetResponseAsync();
                Console.WriteLine($"FTP Delete OK: {res.StatusDescription}");
            }
            catch (WebException ex)
            {
                Console.WriteLine($"FTP Delete Error: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────
        // FTP: Check File Exists
        // ─────────────────────────────────────────────────────────────
        async Task<bool> FtpFileExists(string url, string user, string password)
        {
            try
            {
                FtpWebRequest req = (FtpWebRequest)WebRequest.Create(url);
                req.Method = WebRequestMethods.Ftp.GetFileSize;
                req.Credentials = new NetworkCredential(user, password);
                req.UseBinary = true;
                req.UsePassive = true;
                req.KeepAlive = false;
                req.Timeout = 5000;

                using FtpWebResponse res = (FtpWebResponse)await req.GetResponseAsync();
                return true;
            }
            catch { return false; }
        }

        // ─────────────────────────────────────────────────────────────
        // Generic DB Fetcher
        // ─────────────────────────────────────────────────────────────
        public async Task<List<T>> nGetDataAsync<T>(string storedProcedure, Dictionary<string, object> parameters) where T : new()
        {
            List<T> list = new();

            using SqlConnection con = new(connectionString);
            using SqlCommand cmd = new(storedProcedure, con) { CommandType = CommandType.StoredProcedure };

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

    public static class SqlDataReaderExtensions
    {
        public static bool HasColumn(this SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            return false;
        }
    }
}
