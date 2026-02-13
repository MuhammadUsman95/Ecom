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
        private string connectionString;

        public ProductRegistrationController(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("Connection1");
        }

        [HttpPost("nLoadProductRegistrationData")]
        public async Task<IActionResult> nLoadProductRegistrationData([FromBody] nInfoTab nInfoTabObj)
        {
            try
            {
                // Load Category List
                var categoryParameters = new Dictionary<string, object>
                {
                    { "@nCategoryId", 0 },
                    { "@nsCategoryId", 1 }
                };
                List<CategoryDD> nCategoryList = await nGetDataAsync<CategoryDD>("Ecom_ProductSP", categoryParameters);

                // Load Vendor List
                var vendorParameters = new Dictionary<string, object>
                {
                    { "@nCategoryId", 0 },
                    { "@nsCategoryId", 7 }
                };
                List<VendorDD> nVendorList = await nGetDataAsync<VendorDD>("Ecom_ProductSP", vendorParameters);

                var response = new
                {
                    statusId = 1,
                    CategoryList = nCategoryList,
                    VendorList = nVendorList
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    statusId = 0,
                    message = "Error: " + ex.Message
                });
            }
        }

        [HttpPost("nSaveProductRegistrationData")]
        public async Task<IActionResult> nSaveProductRegistrationData([FromBody] ProductTab nProductTabObj)
        {
            // ✅ Clear model state to bypass automatic validation
            ModelState.Clear();

            // ✅ Manual validation
            if (string.IsNullOrEmpty(nProductTabObj.Product))
            {
                return Ok(new { statusId = 0, message = "Product name is required" });
            }

            if (string.IsNullOrEmpty(nProductTabObj.CategoryId))
            {
                return Ok(new { statusId = 0, message = "Category is required" });
            }

            if (string.IsNullOrEmpty(nProductTabObj.VendorId))
            {
                return Ok(new { statusId = 0, message = "Vendor is required" });
            }

            if (string.IsNullOrEmpty(nProductTabObj.Prices))
            {
                return Ok(new { statusId = 0, message = "Price is required" });
            }

            // ✅ Validate image for new product (not update)
            if (!nProductTabObj.IsUpdate && string.IsNullOrEmpty(nProductTabObj.ProductImageAttachmentfilename))
            {
                return Ok(new { statusId = 0, message = "Product image is required" });
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Ecom_ProductSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 0);
                    cmd.Parameters.AddWithValue("@Product", nProductTabObj.Product);
                    cmd.Parameters.AddWithValue("@IsActive", nProductTabObj.IsActive ? "1" : "0");
                    cmd.Parameters.AddWithValue("@CategoryId", nProductTabObj.CategoryId);
                    cmd.Parameters.AddWithValue("@VendorId", nProductTabObj.VendorId);
                    cmd.Parameters.AddWithValue("@UserId", nProductTabObj.Userid);
                    cmd.Parameters.AddWithValue("@IsUpdate", nProductTabObj.IsUpdate ? "1" : "0");
                    cmd.Parameters.AddWithValue("@ProductImage", nProductTabObj.ProductImageAttachmentfilename ?? "");
                    cmd.Parameters.AddWithValue("@Prices", nProductTabObj.Prices);
                    cmd.Parameters.AddWithValue("@DiscountAmount", nProductTabObj.DiscountAmount ?? "0");

                    if (nProductTabObj.IsUpdate)
                    {
                        cmd.Parameters.AddWithValue("@ProductId", nProductTabObj.ProductId);
                    }

                    await con.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                            int statusId = Convert.ToInt32(dr["StatusId"]);

                            // FTP Upload/Delete Logic - Only execute if DB operation succeeded
                            if (statusId == 1)
                            {
                                // If new image is uploaded
                                if (!string.IsNullOrEmpty(nProductTabObj.ProductImageAttachmentfilename))
                                {
                                    // Delete old image if exists (during update)
                                    if (!string.IsNullOrEmpty(nProductTabObj.ProductImageAttachmentfilenameold))
                                    {
                                        string oldFileName = Path.GetFileName(nProductTabObj.ProductImageAttachmentfilenameold);
                                        await DeleteFromFtp(oldFileName, nProductTabObj.FtpPath);
                                    }

                                    // Upload new image
                                    await UploadToFtp(nProductTabObj.ProductImageAttachmentfilename, nProductTabObj.ProductImageAttachmentbase64, nProductTabObj.FtpPath);
                                }
                            }

                            return Ok(new
                            {
                                statusId = statusId,
                                message = dr["MessageCaption"]?.ToString()
                            });
                        }
                    }
                }

                return Ok(new
                {
                    statusId = 0,
                    message = "No response from database"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    statusId = 0,
                    message = "Error: " + ex.Message
                });
            }
        }

        [HttpPost("nLoadGridViewData")]
        public async Task<IActionResult> nLoadGridViewData([FromBody] nInfoTab nInfoTabObj)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@nCategoryId", 0 },
                    { "@nsCategoryId", 2 }
                };

                List<ExpandoObject> nDataList = await nGetDataAsync<ExpandoObject>("Ecom_ProductSP", parameters);

                var response = new
                {
                    statusId = 1,
                    GridViewDataList = nDataList
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    statusId = 0,
                    message = "Error: " + ex.Message
                });
            }
        }

        [HttpPost("nDeleteProductRegistrationData")]
        public async Task<IActionResult> nDeleteProductRegistrationData([FromBody] ProductDeleteRequest deleteRequest)
        {
            try
            {
                Console.WriteLine($"Delete Request - ProductId: {deleteRequest.ProductId}, UserId: {deleteRequest.Userid}");
                Console.WriteLine($"Image filename: {deleteRequest.ProductImageAttachmentfilenameold}");

                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Ecom_ProductSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 3);
                    cmd.Parameters.AddWithValue("@UserId", deleteRequest.Userid ?? "");
                    cmd.Parameters.AddWithValue("@ProductId", deleteRequest.ProductId);

                    await con.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                            int statusId = Convert.ToInt32(dr["StatusId"]);
                            string message = dr["MessageCaption"]?.ToString() ?? "";

                            Console.WriteLine($"SP Response - StatusId: {statusId}, Message: {message}");

                            // Delete image from FTP if delete succeeded
                            if (statusId == 1)
                            {
                                if (!string.IsNullOrEmpty(deleteRequest.ProductImageAttachmentfilenameold))
                                {
                                    try
                                    {
                                        string fileName = deleteRequest.ProductImageAttachmentfilenameold;

                                        if (fileName.Contains("/"))
                                        {
                                            fileName = Path.GetFileName(fileName);
                                        }

                                        Console.WriteLine($"Attempting to delete file: {fileName}");
                                        await DeleteFromFtp(fileName, deleteRequest.FtpPath);
                                        Console.WriteLine($"File deleted successfully: {fileName}");
                                    }
                                    catch (Exception ftpEx)
                                    {
                                        Console.WriteLine($"FTP Delete Warning: {ftpEx.Message}");
                                    }
                                }
                            }

                            return Ok(new
                            {
                                statusId = statusId,
                                message = message
                            });
                        }
                        else
                        {
                            Console.WriteLine("No data returned from stored procedure");
                            return Ok(new
                            {
                                statusId = 0,
                                message = "No response from database"
                            });
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error: {sqlEx.Message}");
                return Ok(new
                {
                    statusId = 0,
                    message = $"Database Error: {sqlEx.Message}"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Ok(new
                {
                    statusId = 0,
                    message = $"Error: {ex.Message}"
                });
            }
        }

        // ✅ Updated FTP Delete Function with FtpPath parameter
        async Task DeleteFromFtp(string attachmentFileName, string ftpPath)
        {
            if (string.IsNullOrEmpty(attachmentFileName))
                return;

            try
            {
                string ftpServer = _configuration["Config:ftpServer"];
                string ftpUser = _configuration["Config:ftpUser"];
                string ftpPassword = _configuration["Config:ftpPassword"];
                string ftpPort = _configuration["Config:ftpPort"];

                // ✅ Use provided ftpPath or default
                string finalFtpPath = !string.IsNullOrEmpty(ftpPath) ? ftpPath : "/wwwroot/Images/ProductRegistration";

                string ftpUrl = $"ftp://{ftpServer}:{ftpPort}{finalFtpPath}/{attachmentFileName}";

                Console.WriteLine($"FTP Delete URL: {ftpUrl}");

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    Console.WriteLine($"FTP Delete Success: {response.StatusDescription}");
                }
            }
            catch (WebException ex)
            {
                if (ex.Response is FtpWebResponse response)
                {
                    if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        Console.WriteLine($"FTP Delete: File not found - {attachmentFileName}");
                    }
                    else
                    {
                        Console.WriteLine($"FTP Delete Error: {response.StatusDescription}");
                        throw;
                    }
                }
                else
                {
                    Console.WriteLine($"FTP Delete Error: {ex.Message}");
                    throw;
                }
            }
        }

        // ✅ Updated FTP Upload Function with FtpPath parameter
        async Task UploadToFtp(string attachmentFileName, string attachmentBase64, string ftpPath)
        {
            if (string.IsNullOrEmpty(attachmentFileName) || string.IsNullOrEmpty(attachmentBase64))
                return;

            try
            {
                string ftpServer = _configuration["Config:ftpServer"];
                string ftpUser = _configuration["Config:ftpUser"];
                string ftpPassword = _configuration["Config:ftpPassword"];
                string ftpPort = _configuration["Config:ftpPort"];

                // ✅ Use provided ftpPath or default
                string finalFtpPath = !string.IsNullOrEmpty(ftpPath) ? ftpPath : "/wwwroot/Images/ProductRegistration";

                // Remove data:image/png;base64, prefix if present
                if (attachmentBase64.Contains(","))
                    attachmentBase64 = attachmentBase64.Split(',')[1];

                byte[] fileBytes = Convert.FromBase64String(attachmentBase64);

                string ftpUrl = $"ftp://{ftpServer}:{ftpPort}{finalFtpPath}/{attachmentFileName}";

                Console.WriteLine($"FTP Upload URL: {ftpUrl}");

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                request.ContentLength = fileBytes.Length;

                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                }

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    Console.WriteLine($"FTP Upload Success: {response.StatusDescription}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FTP Upload Error: {ex.Message}");
                throw;
            }
        }

        // Generic Data Fetcher
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
                    {
                        expando[dr.GetName(i)] = dr.IsDBNull(i) ? null : dr.GetValue(i);
                    }

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

    // Extension Method for SqlDataReader
    public static class SqlDataReaderExtensions
    {
        public static bool HasColumn(this SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}