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

                            // ✅ FTP Upload/Delete Logic - Only execute if DB operation succeeded
                            if (statusId == 1)
                            {
                                // If new image is uploaded
                                if (!string.IsNullOrEmpty(nProductTabObj.ProductImageAttachmentfilename))
                                {
                                    // Delete old image if exists (during update)
                                    if (!string.IsNullOrEmpty(nProductTabObj.ProductImageAttachmentfilenameold))
                                    {
                                        string oldFileName = Path.GetFileName(nProductTabObj.ProductImageAttachmentfilenameold);
                                        await DeleteFromFtp(oldFileName);
                                    }

                                    // Upload new image
                                    await UploadToFtp(nProductTabObj.ProductImageAttachmentfilename, nProductTabObj.ProductImageAttachmentbase64);
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
        public async Task<IActionResult> nDeleteProductRegistrationData([FromBody] ProductTab nProductTabObj)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Ecom_ProductSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 3);
                    cmd.Parameters.AddWithValue("@UserId", nProductTabObj.Userid);
                    cmd.Parameters.AddWithValue("@ProductId", nProductTabObj.ProductId);

                    await con.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                            int statusId = Convert.ToInt32(dr["StatusId"]);

                            // Delete image from FTP if delete succeeded
                            if (statusId == 1)
                            {
                                if (!string.IsNullOrEmpty(nProductTabObj.ProductImageAttachmentfilenameold))
                                {
                                    string oldFileName = Path.GetFileName(nProductTabObj.ProductImageAttachmentfilenameold);
                                    await DeleteFromFtp(oldFileName);
                                }
                            }

                            return Ok(new
                            {
                                statusId = statusId,
                                message = dr["MessageCaption"].ToString()
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

        // ✅ FTP Delete Function
        async Task DeleteFromFtp(string attachmentFileName)
        {
            if (string.IsNullOrEmpty(attachmentFileName))
                return;

            try
            {
                string ftpPath = _configuration["Config:ftpPath"];
                string ftpServer = _configuration["Config:ftpServer"];
                string ftpUser = _configuration["Config:ftpUser"];
                string ftpPassword = _configuration["Config:ftpPassword"];
                string ftpPort = _configuration["Config:ftpPort"];

                string ftpUrl = $"ftp://{ftpServer}:{ftpPort}{ftpPath}/{attachmentFileName}";

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    // Successfully deleted
                    Console.WriteLine($"FTP Delete Success: {response.StatusDescription}");
                }
            }
            catch (WebException ex)
            {
                if (ex.Response is FtpWebResponse response)
                {
                    if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        // File does not exist - ignore
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

        // ✅ FTP Upload Function
        async Task UploadToFtp(string attachmentFileName, string attachmentBase64)
        {
            if (string.IsNullOrEmpty(attachmentFileName) || string.IsNullOrEmpty(attachmentBase64))
                return;

            try
            {
                string ftpPath = _configuration["Config:ftpPath"];
                string ftpServer = _configuration["Config:ftpServer"];
                string ftpUser = _configuration["Config:ftpUser"];
                string ftpPassword = _configuration["Config:ftpPassword"];
                string ftpPort = _configuration["Config:ftpPort"];

                // Remove data:image/png;base64, prefix if present
                if (attachmentBase64.Contains(","))
                    attachmentBase64 = attachmentBase64.Split(',')[1];

                byte[] fileBytes = Convert.FromBase64String(attachmentBase64);

                string ftpUrl = $"ftp://{ftpServer}:{ftpPort}{ftpPath}/{attachmentFileName}";

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

        // ✅ Generic Data Fetcher
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

    // ✅ Extension Method for SqlDataReader
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
