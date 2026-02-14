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
    public class SliderRegistrationController : Controller
    {
        private readonly IConfiguration _configuration;
        private string connectionString;

        public SliderRegistrationController(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("Connection1");
        }

        [HttpPost("nLoadSliderRegistrationData")]
        public async Task<IActionResult> nLoadSliderRegistrationData([FromBody] nInfoTab nInfoTabObj)
        {
            try
            {
                // Load Vendor List
                var vendorParameters = new Dictionary<string, object>
                {
                    { "@nCategoryId", 0 },
                    { "@nsCategoryId", 7 }
                };
                List<VendorDD> nVendorList = await nGetDataAsync<VendorDD>("Ecom_SilderSP", vendorParameters);

                // Load Slider Type List
                var sliderTypeParameters = new Dictionary<string, object>
                {
                    { "@nCategoryId", 0 },
                    { "@nsCategoryId", 8 }
                };
                List<SliderTypeDD> nSliderTypeList = await nGetDataAsync<SliderTypeDD>("Ecom_SilderSP", sliderTypeParameters);

                var response = new
                {
                    statusId = 1,
                    VendorList = nVendorList,
                    SliderTypeList = nSliderTypeList
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

        [HttpPost("nSaveSliderRegistrationData")]
        public async Task<IActionResult> nSaveSliderRegistrationData([FromBody] SliderTab nSliderTabObj)
        {
            // Clear model state to bypass automatic validation
            ModelState.Clear();

            // Manual validation
            if (string.IsNullOrEmpty(nSliderTabObj.SilderName))
            {
                return Ok(new { statusId = 0, message = "Slider name is required" });
            }

            if (string.IsNullOrEmpty(nSliderTabObj.VendorId))
            {
                return Ok(new { statusId = 0, message = "Vendor is required" });
            }

            if (string.IsNullOrEmpty(nSliderTabObj.SliderType))
            {
                return Ok(new { statusId = 0, message = "Slider type is required" });
            }

            // Validate image for new slider (not update)
            if (!nSliderTabObj.IsUpdate && string.IsNullOrEmpty(nSliderTabObj.SilderImageAttachmentfilename))
            {
                return Ok(new { statusId = 0, message = "Slider image is required" });
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Ecom_SilderSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 0);
                    cmd.Parameters.AddWithValue("@SilderName", nSliderTabObj.SilderName);
                    cmd.Parameters.AddWithValue("@IsActive", nSliderTabObj.IsActive ? "1" : "0");
                    cmd.Parameters.AddWithValue("@VendorId", nSliderTabObj.VendorId);
                    cmd.Parameters.AddWithValue("@SliderType", nSliderTabObj.SliderType);
                    cmd.Parameters.AddWithValue("@HeadingSlider", nSliderTabObj.HeadingSlider ?? "");
                    cmd.Parameters.AddWithValue("@DescriptionSlider", nSliderTabObj.DescriptionSlider ?? "");
                    cmd.Parameters.AddWithValue("@UserId", nSliderTabObj.Userid);
                    cmd.Parameters.AddWithValue("@IsUpdate", nSliderTabObj.IsUpdate ? "1" : "0");
                    cmd.Parameters.AddWithValue("@SilderImages", nSliderTabObj.SilderImageAttachmentfilename ?? "");

                    if (nSliderTabObj.IsUpdate)
                    {
                        cmd.Parameters.AddWithValue("@SilderId", nSliderTabObj.SilderId);
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
                                // Only process images if a NEW image was uploaded
                                if (!string.IsNullOrEmpty(nSliderTabObj.SilderImageAttachmentfilename))
                                {
                                    try
                                    {
                                        // Delete old image ONLY if updating AND old image exists
                                        if (nSliderTabObj.IsUpdate && !string.IsNullOrEmpty(nSliderTabObj.SilderImageAttachmentfilenameold))
                                        {
                                            Console.WriteLine($"Deleting old image: {nSliderTabObj.SilderImageAttachmentfilenameold}");
                                            await DeleteFromFtp(nSliderTabObj.SilderImageAttachmentfilenameold, nSliderTabObj.FtpPath);
                                        }

                                        // Upload new image
                                        Console.WriteLine($"Uploading new image: {nSliderTabObj.SilderImageAttachmentfilename}");
                                        await UploadToFtp(
                                            nSliderTabObj.SilderImageAttachmentfilename,
                                            nSliderTabObj.SilderImageAttachmentbase64,
                                            nSliderTabObj.FtpPath
                                        );
                                    }
                                    catch (Exception ftpEx)
                                    {
                                        Console.WriteLine($"FTP Operation Warning: {ftpEx.Message}");
                                        // Don't fail the entire operation for FTP errors
                                        // Data is already saved in DB
                                    }
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
                Console.WriteLine($"Save Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
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

                List<ExpandoObject> nDataList = await nGetDataAsync<ExpandoObject>("Ecom_SilderSP", parameters);

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

        [HttpPost("nDeleteSliderRegistrationData")]
        public async Task<IActionResult> nDeleteSliderRegistrationData([FromBody] SliderDeleteRequest deleteRequest)
        {
            try
            {
                Console.WriteLine($"Delete Request - SilderId: {deleteRequest.SilderId}, UserId: {deleteRequest.Userid}");
                Console.WriteLine($"Image filename: {deleteRequest.SilderImageAttachmentfilenameold}");

                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Ecom_SilderSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 3);
                    cmd.Parameters.AddWithValue("@UserId", deleteRequest.Userid ?? "");
                    cmd.Parameters.AddWithValue("@SilderId", deleteRequest.SilderId);

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
                                if (!string.IsNullOrEmpty(deleteRequest.SilderImageAttachmentfilenameold))
                                {
                                    try
                                    {
                                        Console.WriteLine($"Attempting to delete file: {deleteRequest.SilderImageAttachmentfilenameold}");
                                        await DeleteFromFtp(deleteRequest.SilderImageAttachmentfilenameold, deleteRequest.FtpPath);
                                        Console.WriteLine($"File deleted successfully");
                                    }
                                    catch (Exception ftpEx)
                                    {
                                        Console.WriteLine($"FTP Delete Warning: {ftpEx.Message}");
                                        // Don't fail the operation
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

        // FTP Delete Function with File Existence Check
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

                // Use provided ftpPath or default
                string finalFtpPath = !string.IsNullOrEmpty(ftpPath) ? ftpPath : "/wwwroot/Images/SliderRegistration";

                // Extract just filename if full URL is passed
                string fileName = attachmentFileName.Contains("/") || attachmentFileName.Contains("\\")
                    ? Path.GetFileName(attachmentFileName)
                    : attachmentFileName;

                string ftpUrl = $"ftp://{ftpServer}:{ftpPort}{finalFtpPath}/{fileName}";

                Console.WriteLine($"FTP Delete Attempt - URL: {ftpUrl}");

                // Check if file exists first
                bool fileExists = await CheckFtpFileExists(ftpUrl, ftpUser, ftpPassword);

                if (!fileExists)
                {
                    Console.WriteLine($"FTP Delete Skipped: File doesn't exist - {fileName}");
                    return; // Don't throw error, just skip
                }

                // Delete the file
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                request.Timeout = 10000; // 10 seconds timeout

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    Console.WriteLine($"FTP Delete Success: {response.StatusDescription}");
                }
            }
            catch (WebException ex)
            {
                if (ex.Response is FtpWebResponse response)
                {
                    Console.WriteLine($"FTP Delete Error Code: {response.StatusCode}");
                    Console.WriteLine($"FTP Delete Error Message: {response.StatusDescription}");

                    // Don't throw error for file not found - just log it
                    if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        Console.WriteLine($"FTP Delete: File not found (already deleted or never existed) - {attachmentFileName}");
                        return; // Don't propagate error
                    }
                }

                Console.WriteLine($"FTP Delete Exception: {ex.Message}");
                // Don't throw - just log
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FTP Delete General Error: {ex.Message}");
                // Don't throw - just log
            }
        }

        // Helper Method - Check if file exists on FTP
        async Task<bool> CheckFtpFileExists(string ftpUrl, string ftpUser, string ftpPassword)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.GetFileSize; // Just check size
                request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                request.Timeout = 5000; // 5 seconds timeout

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    return true; // File exists
                }
            }
            catch (WebException ex)
            {
                if (ex.Response is FtpWebResponse response)
                {
                    if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        return false; // File doesn't exist
                    }
                }
                return false; // Assume doesn't exist on any error
            }
            catch
            {
                return false; // Assume doesn't exist
            }
        }

        // FTP Upload Function
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

                // Use provided ftpPath or default
                string finalFtpPath = !string.IsNullOrEmpty(ftpPath) ? ftpPath : "/wwwroot/Images/SliderRegistration";

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
                request.Timeout = 30000; // 30 seconds timeout

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
                throw; // Propagate upload errors
            }
        }

        // ✅ FIXED Generic Data Fetcher with Nullable Type Handling
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
                        try
                        {
                            if (!dr.HasColumn(prop.Name))
                                continue;

                            var value = dr[prop.Name];

                            if (value == null || value == DBNull.Value)
                                continue;

                            // ✅ Handle Nullable Types
                            Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                            // ✅ Convert and Set
                            object convertedValue = Convert.ChangeType(value, targetType);
                            prop.SetValue(obj, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            // ✅ Silent fail - log if needed
                            Console.WriteLine($"Property {prop.Name} error: {ex.Message}");
                        }
                    }

                    list.Add(obj);
                }
            }

            return list;
        }
    }

    // ❌ REMOVED - Extension class deleted to avoid ambiguity
    // This extension method is already defined in ProductRegistrationController.cs
    // Extension methods work globally, so one definition is enough
}
