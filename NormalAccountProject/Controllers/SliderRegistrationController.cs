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
                List<VendorDD> nVendorList = await nGetDataAsync<VendorDD>("Ecom_SliderSP", vendorParameters);

                // Load Slider Type List
                var sliderTypeParameters = new Dictionary<string, object>
                {
                    { "@nCategoryId", 0 },
                    { "@nsCategoryId", 8 }
                };
                List<SliderTypeDD> nSliderTypeList = await nGetDataAsync<SliderTypeDD>("Ecom_SliderSP", sliderTypeParameters);

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

            // ✅ ENHANCED: Check FTP Configuration FIRST
            string ftpServer = _configuration["Config:ftpServer"];
            string ftpUser = _configuration["Config:ftpUser"];
            string ftpPassword = _configuration["Config:ftpPassword"];
            string ftpPort = _configuration["Config:ftpPort"];

            Console.WriteLine("========================================");
            Console.WriteLine("🔧 FTP CONFIGURATION CHECK");
            Console.WriteLine($"Server: {(string.IsNullOrEmpty(ftpServer) ? "❌ NOT SET" : $"✅ {ftpServer}")}");
            Console.WriteLine($"User: {(string.IsNullOrEmpty(ftpUser) ? "❌ NOT SET" : $"✅ {ftpUser}")}");
            Console.WriteLine($"Password: {(string.IsNullOrEmpty(ftpPassword) ? "❌ NOT SET" : "✅ SET")}");
            Console.WriteLine($"Port: {(string.IsNullOrEmpty(ftpPort) ? "❌ NOT SET (will use 21)" : $"✅ {ftpPort}")}");
            Console.WriteLine("========================================");

            if (string.IsNullOrEmpty(ftpServer) || string.IsNullOrEmpty(ftpUser) || string.IsNullOrEmpty(ftpPassword))
            {
                return Ok(new
                {
                    statusId = 0,
                    message = "❌ FTP Configuration Missing in appsettings.json!\n\nRequired fields:\n- Config:ftpServer\n- Config:ftpUser\n- Config:ftpPassword\n- Config:ftpPort"
                });
            }

            // Manual validation
            if (string.IsNullOrEmpty(nSliderTabObj.SliderName))
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
            if (!nSliderTabObj.IsUpdate && string.IsNullOrEmpty(nSliderTabObj.SliderImageAttachmentfilename))
            {
                return Ok(new { statusId = 0, message = "Slider image is required" });
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Ecom_SliderSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 0);
                    cmd.Parameters.AddWithValue("@SliderName", nSliderTabObj.SliderName);
                    cmd.Parameters.AddWithValue("@IsActive", nSliderTabObj.IsActive ? "1" : "0");
                    cmd.Parameters.AddWithValue("@VendorId", nSliderTabObj.VendorId);
                    cmd.Parameters.AddWithValue("@SliderType", nSliderTabObj.SliderType);
                    cmd.Parameters.AddWithValue("@HeadingSlider", nSliderTabObj.HeadingSlider ?? "");
                    cmd.Parameters.AddWithValue("@DescriptionSlider", nSliderTabObj.DescriptionSlider ?? "");
                    cmd.Parameters.AddWithValue("@UserId", nSliderTabObj.Userid);
                    cmd.Parameters.AddWithValue("@IsUpdate", nSliderTabObj.IsUpdate ? "1" : "0");
                    cmd.Parameters.AddWithValue("@SliderImages", nSliderTabObj.SliderImageAttachmentfilename ?? "");
                    cmd.Parameters.AddWithValue("@SliderMovingTimer", nSliderTabObj.SliderMovingTimer); // ✅ ADD THIS LINE

                    if (nSliderTabObj.IsUpdate)
                    {
                        cmd.Parameters.AddWithValue("@SliderId", nSliderTabObj.SliderId);
                    }

                    await con.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                            int statusId = Convert.ToInt32(dr["StatusId"]);
                            string dbMessage = dr["MessageCaption"]?.ToString() ?? "";

                            Console.WriteLine($"📊 DB OPERATION - StatusId: {statusId}, Message: {dbMessage}");

                            // ✅ FTP Upload/Delete Logic - Only execute if DB operation succeeded
                            if (statusId == 1)
                            {
                                // Only process images if a NEW image was uploaded
                                if (!string.IsNullOrEmpty(nSliderTabObj.SliderImageAttachmentfilename))
                                {
                                    try
                                    {
                                        Console.WriteLine("========================================");
                                        Console.WriteLine("📂 FTP OPERATION STARTING");
                                        Console.WriteLine($"New File: {nSliderTabObj.SliderImageAttachmentfilename}");
                                        Console.WriteLine($"Old File: {nSliderTabObj.SliderImageAttachmentfilenameold ?? "NONE"}");
                                        Console.WriteLine($"FTP Path: {nSliderTabObj.FtpPath}");
                                        Console.WriteLine($"Base64 Length: {nSliderTabObj.SliderImageAttachmentbase64?.Length ?? 0}");
                                        Console.WriteLine("========================================");

                                        // Delete old image ONLY if updating AND old image exists
                                        if (nSliderTabObj.IsUpdate && !string.IsNullOrEmpty(nSliderTabObj.SliderImageAttachmentfilenameold))
                                        {
                                            Console.WriteLine($"🗑️ Attempting to delete old image: {nSliderTabObj.SliderImageAttachmentfilenameold}");
                                            await DeleteFromFtp(nSliderTabObj.SliderImageAttachmentfilenameold, nSliderTabObj.FtpPath);
                                        }

                                        // Upload new image
                                        Console.WriteLine($"📤 Uploading new image: {nSliderTabObj.SliderImageAttachmentfilename}");
                                        await UploadToFtp(
                                            nSliderTabObj.SliderImageAttachmentfilename,
                                            nSliderTabObj.SliderImageAttachmentbase64,
                                            nSliderTabObj.FtpPath
                                        );

                                        Console.WriteLine("✅ ✅ ✅ IMAGE UPLOADED SUCCESSFULLY! ✅ ✅ ✅");
                                    }
                                    catch (Exception ftpEx)
                                    {
                                        Console.WriteLine("========================================");
                                        Console.WriteLine("❌ FTP OPERATION FAILED");
                                        Console.WriteLine($"Error: {ftpEx.Message}");
                                        Console.WriteLine($"Stack Trace: {ftpEx.StackTrace}");
                                        Console.WriteLine("========================================");

                                        // ✅ RETURN DETAILED ERROR TO USER
                                        return Ok(new
                                        {
                                            statusId = 0,
                                            message = $"⚠️ Database saved successfully but FTP upload failed!\n\n" +
                                                     $"Error Details:\n{ftpEx.Message}\n\n" +
                                                     $"Possible Solutions:\n" +
                                                     $"1. Check appsettings.json FTP configuration\n" +
                                                     $"2. Verify FTP server is accessible\n" +
                                                     $"3. Check FTP folder permissions\n" +
                                                     $"4. Ensure folder path exists: {nSliderTabObj.FtpPath}\n\n" +
                                                     $"Contact system administrator for assistance."
                                        });
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("⚠️ No new image to upload (update without image change)");
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
                Console.WriteLine($"❌ SAVE ERROR: {ex.Message}");
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

                List<ExpandoObject> nDataList = await nGetDataAsync<ExpandoObject>("Ecom_SliderSP", parameters);

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
                Console.WriteLine($"🗑️ DELETE REQUEST - SliderId: {deleteRequest.SliderId}, UserId: {deleteRequest.Userid}");
                Console.WriteLine($"Image filename: {deleteRequest.SliderImageAttachmentfilenameold}");

                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Ecom_SliderSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 3);
                    cmd.Parameters.AddWithValue("@UserId", deleteRequest.Userid ?? "");
                    cmd.Parameters.AddWithValue("@SliderId", deleteRequest.SliderId);

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
                                if (!string.IsNullOrEmpty(deleteRequest.SliderImageAttachmentfilenameold))
                                {
                                    try
                                    {
                                        Console.WriteLine($"Attempting to delete file: {deleteRequest.SliderImageAttachmentfilenameold}");
                                        await DeleteFromFtp(deleteRequest.SliderImageAttachmentfilenameold, deleteRequest.FtpPath);
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

        // ✅ IMPROVED: FTP Delete with Better Error Handling
        async Task DeleteFromFtp(string attachmentFileName, string ftpPath)
        {
            if (string.IsNullOrEmpty(attachmentFileName))
            {
                Console.WriteLine("⚠️ DeleteFromFtp: Filename is empty, skipping");
                return;
            }

            try
            {
                string ftpServer = _configuration["Config:ftpServer"];
                string ftpUser = _configuration["Config:ftpUser"];
                string ftpPassword = _configuration["Config:ftpPassword"];
                string ftpPort = _configuration["Config:ftpPort"] ?? "21";

                // Use provided ftpPath or default
                string finalFtpPath = !string.IsNullOrEmpty(ftpPath) ? ftpPath : "/wwwroot/Images/SliderRegistration";

                // Extract just filename if full URL is passed
                string fileName = attachmentFileName.Contains("/") || attachmentFileName.Contains("\\")
                    ? Path.GetFileName(attachmentFileName)
                    : attachmentFileName;

                string ftpUrl = $"ftp://{ftpServer}:{ftpPort}{finalFtpPath}/{fileName}";

                Console.WriteLine($"🗑️ FTP Delete - URL: {ftpUrl}");

                // Check if file exists first
                bool fileExists = await CheckFtpFileExists(ftpUrl, ftpUser, ftpPassword);

                if (!fileExists)
                {
                    Console.WriteLine($"⚠️ File doesn't exist (already deleted or never uploaded): {fileName}");
                    return; // Don't throw error, just skip
                }

                // Delete the file
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                request.Timeout = 10000;

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    Console.WriteLine($"✅ FTP Delete Success: {response.StatusDescription}");
                }
            }
            catch (WebException ex)
            {
                if (ex.Response is FtpWebResponse response)
                {
                    Console.WriteLine($"❌ FTP Delete Error Code: {response.StatusCode}");
                    Console.WriteLine($"❌ FTP Delete Error: {response.StatusDescription}");

                    if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        Console.WriteLine($"⚠️ File not found (ignoring): {attachmentFileName}");
                        return;
                    }
                }

                Console.WriteLine($"❌ FTP Delete Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FTP Delete Error: {ex.Message}");
            }
        }

        // ✅ Helper Method - Check if file exists on FTP
        async Task<bool> CheckFtpFileExists(string ftpUrl, string ftpUser, string ftpPassword)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.GetFileSize;
                request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                request.Timeout = 5000;

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    Console.WriteLine($"✅ File exists: {ftpUrl}");
                    return true;
                }
            }
            catch (WebException ex)
            {
                if (ex.Response is FtpWebResponse response)
                {
                    if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        Console.WriteLine($"⚠️ File doesn't exist: {ftpUrl}");
                        return false;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        // ✅ IMPROVED: Create FTP Directory with Better Logging
        async Task CreateFtpDirectoryIfNotExists(string ftpServer, string ftpPort, string ftpUser, string ftpPassword, string directoryPath)
        {
            try
            {
                Console.WriteLine($"📁 Checking FTP directory: {directoryPath}");

                string[] pathParts = directoryPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                string currentPath = "";

                foreach (string part in pathParts)
                {
                    currentPath += "/" + part;
                    string ftpUrl = $"ftp://{ftpServer}:{ftpPort}{currentPath}";

                    try
                    {
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
                                    Console.WriteLine($"✅ Created directory: {currentPath}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"⚠️ Directory check failed: {currentPath} - {response.StatusDescription}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Directory creation warning: {ex.Message}");
            }
        }

        // ✅ IMPROVED: FTP Upload with Detailed Logging
        async Task UploadToFtp(string fileName, string base64String, string ftpPath)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("📤 STARTING FTP UPLOAD");
            Console.WriteLine("========================================");

            // Validation
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Console.WriteLine("❌ ERROR: Filename is empty");
                throw new Exception("Filename is empty");
            }

            if (string.IsNullOrWhiteSpace(base64String))
            {
                Console.WriteLine("❌ ERROR: Base64 string is empty");
                throw new Exception("Base64 image data is empty");
            }

            string ftpServer = _configuration["Config:ftpServer"];
            string ftpUser = _configuration["Config:ftpUser"];
            string ftpPassword = _configuration["Config:ftpPassword"];
            string ftpPort = _configuration["Config:ftpPort"] ?? "21";

            Console.WriteLine($"🔧 FTP Server: {ftpServer}");
            Console.WriteLine($"🔧 FTP Port: {ftpPort}");
            Console.WriteLine($"🔧 FTP User: {ftpUser}");
            Console.WriteLine($"📄 Filename: {fileName}");

            if (string.IsNullOrWhiteSpace(ftpServer))
            {
                Console.WriteLine("❌ ERROR: FTP Server not configured");
                throw new Exception("FTP Server not configured in appsettings.json");
            }

            if (string.IsNullOrWhiteSpace(ftpUser))
            {
                Console.WriteLine("❌ ERROR: FTP Username not configured");
                throw new Exception("FTP Username not configured in appsettings.json");
            }

            if (string.IsNullOrWhiteSpace(ftpPassword))
            {
                Console.WriteLine("❌ ERROR: FTP Password not configured");
                throw new Exception("FTP Password not configured in appsettings.json");
            }

            // Remove base64 header
            if (base64String.Contains(","))
            {
                Console.WriteLine("🔧 Removing base64 header prefix");
                base64String = base64String.Split(',')[1];
            }

            byte[] fileBytes;
            try
            {
                fileBytes = Convert.FromBase64String(base64String);
                Console.WriteLine($"✅ Base64 decoded successfully - Size: {fileBytes.Length} bytes ({fileBytes.Length / 1024.0:F2} KB)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: Failed to decode base64 - {ex.Message}");
                throw new Exception($"Invalid base64 image data: {ex.Message}");
            }

            string finalPath = string.IsNullOrWhiteSpace(ftpPath)
                ? "/wwwroot/Images/SliderRegistration"
                : ftpPath;

            Console.WriteLine($"📂 FTP Path: {finalPath}");

            // Create directory if not exists
            Console.WriteLine("📁 Checking/Creating FTP directory...");
            await CreateFtpDirectoryIfNotExists(ftpServer, ftpPort, ftpUser, ftpPassword, finalPath);

            string ftpUrl = $"ftp://{ftpServer}:{ftpPort}{finalPath}/{fileName}";
            Console.WriteLine($"🌐 Full FTP URL: {ftpUrl}");

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

                Console.WriteLine($"📤 Uploading {fileBytes.Length} bytes...");

                using (Stream stream = await request.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
                    Console.WriteLine("✅ Data written to FTP stream");
                }

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    Console.WriteLine("========================================");
                    Console.WriteLine($"✅ ✅ ✅ UPLOAD SUCCESS! ✅ ✅ ✅");
                    Console.WriteLine($"Status: {response.StatusCode} - {response.StatusDescription}");
                    Console.WriteLine($"File: {fileName}");
                    Console.WriteLine($"Size: {fileBytes.Length / 1024.0:F2} KB");
                    Console.WriteLine("========================================");
                }
            }
            catch (WebException webEx)
            {
                Console.WriteLine("========================================");
                Console.WriteLine("❌ FTP UPLOAD FAILED");
                Console.WriteLine("========================================");

                string errorDetails = $"WebException: {webEx.Message}";

                if (webEx.Response is FtpWebResponse ftpResponse)
                {
                    errorDetails += $"\nFTP Status: {ftpResponse.StatusCode} - {ftpResponse.StatusDescription}";

                    try
                    {
                        using (Stream responseStream = ftpResponse.GetResponseStream())
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            string serverResponse = await reader.ReadToEndAsync();
                            if (!string.IsNullOrEmpty(serverResponse))
                            {
                                errorDetails += $"\nServer Response: {serverResponse}";
                            }
                        }
                    }
                    catch { }
                }

                Console.WriteLine(errorDetails);
                Console.WriteLine($"Stack Trace: {webEx.StackTrace}");
                Console.WriteLine("========================================");

                throw new Exception($"FTP Upload Failed:\n{errorDetails}", webEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine("========================================");
                Console.WriteLine("❌ UNEXPECTED ERROR");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("========================================");
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
                        try
                        {
                            if (!dr.HasColumn(prop.Name))
                                continue;

                            var value = dr[prop.Name];

                            if (value == null || value == DBNull.Value)
                                continue;

                            Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                            object convertedValue = Convert.ChangeType(value, targetType);
                            prop.SetValue(obj, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Property {prop.Name} error: {ex.Message}");
                        }
                    }

                    list.Add(obj);
                }
            }

            return list;
        }
    }
}
