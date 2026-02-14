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
                            string dbMessage = dr["MessageCaption"]?.ToString() ?? "";

                            // ✅ FTP Upload/Delete Logic - Only execute if DB operation succeeded
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

                                        Console.WriteLine("✅ Image uploaded successfully!");
                                    }
                                    catch (Exception ftpEx)
                                    {
                                        Console.WriteLine($"❌ FTP Operation Error: {ftpEx.Message}");
                                        Console.WriteLine($"Stack Trace: {ftpEx.StackTrace}");

                                        // ✅ RETURN ERROR TO USER (Don't silently ignore)
                                        return Ok(new
                                        {
                                            statusId = 0,
                                            message = $"Data saved but image upload failed: {ftpEx.Message}\n\nPlease check FTP configuration or contact administrator."
                                        });
                                    }
                                }
                            }

                            return Ok(new
                            {
                                statusId = statusId,
                                message = dbMessage
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

        // ✅ NEW METHOD: Create FTP Directory Recursively
        async Task CreateFtpDirectoryIfNotExists(string ftpServer, string ftpPort, string ftpUser, string ftpPassword, string directoryPath)
        {
            try
            {
                // Split path into parts (e.g., /wwwroot/Images/SliderRegistration -> ["wwwroot", "Images", "SliderRegistration"])
                string[] pathParts = directoryPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                string currentPath = "";

                foreach (string part in pathParts)
                {
                    currentPath += "/" + part;
                    string ftpUrl = $"ftp://{ftpServer}:{ftpPort}{currentPath}";

                    try
                    {
                        // Check if directory exists
                        FtpWebRequest listRequest = (FtpWebRequest)WebRequest.Create(ftpUrl);
                        listRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                        listRequest.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                        listRequest.UsePassive = true;
                        listRequest.KeepAlive = false;
                        listRequest.Timeout = 5000;

                        using (FtpWebResponse response = (FtpWebResponse)await listRequest.GetResponseAsync())
                        {
                            Console.WriteLine($"✅ Directory exists: {currentPath}");
                        }
                    }
                    catch (WebException ex)
                    {
                        if (ex.Response is FtpWebResponse response)
                        {
                            // Directory doesn't exist - create it
                            if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                            {
                                Console.WriteLine($"📁 Creating directory: {currentPath}");

                                FtpWebRequest createRequest = (FtpWebRequest)WebRequest.Create(ftpUrl);
                                createRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
                                createRequest.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                                createRequest.UsePassive = true;
                                createRequest.KeepAlive = false;
                                createRequest.Timeout = 10000;

                                using (FtpWebResponse createResponse = (FtpWebResponse)await createRequest.GetResponseAsync())
                                {
                                    Console.WriteLine($"✅ Created directory: {currentPath} - {createResponse.StatusDescription}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Directory creation warning: {ex.Message}");
                // Don't throw - directory might already exist or we might not have permission
            }
        }

        // ✅ UPDATED FTP Upload Function with Auto Directory Creation
        async Task UploadToFtp(string fileName, string base64String, string ftpPath)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new Exception("Filename is empty");

            if (string.IsNullOrWhiteSpace(base64String))
                throw new Exception("Base64 string is empty");

            string ftpServer = _configuration["Config:ftpServer"];
            string ftpUser = _configuration["Config:ftpUser"];
            string ftpPassword = _configuration["Config:ftpPassword"];
            string ftpPort = _configuration["Config:ftpPort"];

            if (string.IsNullOrWhiteSpace(ftpServer))
                throw new Exception("FTP Server not configured");

            if (string.IsNullOrWhiteSpace(ftpUser))
                throw new Exception("FTP Username not configured");

            if (string.IsNullOrWhiteSpace(ftpPassword))
                throw new Exception("FTP Password not configured");

            Console.WriteLine($"🔧 FTP Config - Server: {ftpServer}, Port: {ftpPort}, User: {ftpUser}");

            // Remove base64 header
            if (base64String.Contains(","))
                base64String = base64String.Split(',')[1];

            byte[] fileBytes = Convert.FromBase64String(base64String);

            string finalPath = string.IsNullOrWhiteSpace(ftpPath)
                ? "/wwwroot/Images/SliderRegistration"
                : ftpPath;

            Console.WriteLine($"📂 FTP Path: {finalPath}");

            // ✅ CREATE DIRECTORY IF NOT EXISTS
            await CreateFtpDirectoryIfNotExists(ftpServer, ftpPort, ftpUser, ftpPassword, finalPath);

            string ftpUrl = $"ftp://{ftpServer}:{ftpPort}{finalPath}/{fileName}";

            Console.WriteLine($"📤 Uploading To: {ftpUrl}");

            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                request.ContentLength = fileBytes.Length;
                request.Timeout = 30000;

                using (Stream stream = await request.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
                }

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    Console.WriteLine($"✅ FTP Upload Success: {response.StatusDescription}");
                }
            }
            catch (WebException webEx)
            {
                string errorDetails = "";

                if (webEx.Response is FtpWebResponse ftpResponse)
                {
                    errorDetails = $"FTP Status: {ftpResponse.StatusCode} - {ftpResponse.StatusDescription}";

                    try
                    {
                        using (Stream responseStream = ftpResponse.GetResponseStream())
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            errorDetails += "\nServer Response: " + await reader.ReadToEndAsync();
                        }
                    }
                    catch { }
                }

                Console.WriteLine($"❌ WebException: {webEx.Message}");
                Console.WriteLine($"❌ Details: {errorDetails}");

                throw new Exception($"FTP Upload Failed: {errorDetails}", webEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FTP Upload Error: {ex.Message}");
                throw;
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
//```

//---

//## **Key Changes:**

//1. ✅ **Added `CreateFtpDirectoryIfNotExists` method** - Automatically creates directories
//2. ✅ **Updated `UploadToFtp`** - Calls directory creation before upload
//3. ✅ **Better error messages** - User ko proper error dikhega
//4. ✅ **Detailed console logging** - Debugging ke liye

//---

//## **Testing:**

//Run karo aur console output dekho:
//```
//🔧 FTP Config - Server: your - server.com, Port: 21, User: youruser
//📂 FTP Path: / wwwroot / Images / SliderRegistration
//📁 Creating directory: / wwwroot
//✅ Created directory: / wwwroot
//📁 Creating directory: / wwwroot / Images
//✅ Created directory: / wwwroot / Images
//📁 Creating directory: / wwwroot / Images / SliderRegistration
//✅ Created directory: / wwwroot / Images / SliderRegistration
//📤 Uploading To: ftp://your-server.com:21/wwwroot/Images/SliderRegistration/Test_15022026.jpg
//✅ FTP Upload Success: 226 Transfer complete