using Microsoft.AspNetCore.Mvc;
using NormalAccountProject.Models;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Net;
using static NormalAccountProject.Controllers.DashboardController;

namespace NormalAccountProject.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CustomerRegistrationController : Controller
    {
        private readonly IConfiguration _configuration;
        private string connectionString;

        public CustomerRegistrationController(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("Connection1");
        }

        [HttpPost("nLoadCustomerRegistrationData")]
        public async Task<IActionResult> nLoadCustomerRegistrationData([FromBody] nInfoTab nInfoTabObj)
        {
            try
            {

                var parameters = new Dictionary<string, object>
                {
                    { "@nType", 0 },
                    { "@nsType", 1 }
                };

                List<CustomerTypedd> nTypeList = await nGetDataAsync<CustomerTypedd>("Customer_SP", parameters);

                var response = new
                {
                    statusId = 1,
                    TypeList = nTypeList
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

        [HttpPost("nSaveCustomerRegistrationData")]
        public async Task<IActionResult> nSaveCustomerRegistrationData([FromBody] CustomerTab nCustomerTabObj)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Customer_SP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 0);
                    cmd.Parameters.AddWithValue("@Customer", nCustomerTabObj.Customer);
                    cmd.Parameters.AddWithValue("@ContactNo", nCustomerTabObj.ContactNo);
                    cmd.Parameters.AddWithValue("@IsActive", nCustomerTabObj.IsActive ? "1" : "0");
                    cmd.Parameters.AddWithValue("@Type", nCustomerTabObj.Type);
                    cmd.Parameters.AddWithValue("@UserId", nCustomerTabObj.Userid);
                    cmd.Parameters.AddWithValue("@IsUpdate", nCustomerTabObj.IsUpdate ? "1" : "0");
                    cmd.Parameters.AddWithValue("@ImagePath", nCustomerTabObj.CustomerImageAttachmentfilename);
                    if (nCustomerTabObj.IsUpdate)
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", nCustomerTabObj.CustomerId);
                    }

                    // 🔹 Build SQL exec line for debugging
                    string sqlDebug = $"EXEC Customer_SP " +
                                      $"@nType=0, " +
                                      $"@nsType=0, " +
                                      $"@Customer='{nCustomerTabObj.Customer}', " +
                                      $"@ContactNo='{nCustomerTabObj.ContactNo}', " +
                                      $"@IsActive='{(nCustomerTabObj.IsActive ? "1" : "0")}', " +
                                      $"@Type='{nCustomerTabObj.Type}', " +
                                      $"@UserId='{nCustomerTabObj.Userid}', " +
                                      $"@IsUpdate='{(nCustomerTabObj.IsUpdate ? "1" : "0")}'" +
                                      $"@ImagePath='{(nCustomerTabObj.CustomerImageAttachmentfilename)}'";

                    if (nCustomerTabObj.IsUpdate)
                    {
                        sqlDebug += $", @CustomerId='{nCustomerTabObj.CustomerId}'";
                    }

                    // 🔹 You can now log or store sqlDebug for SQL Server testing
                    Console.WriteLine(sqlDebug);

                    await con.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                            int statusId = Convert.ToInt32(dr["StatusId"]);

                            if (statusId == 1)
                            {
                                if (!string.IsNullOrEmpty(nCustomerTabObj.CustomerImageAttachmentfilename))
                                {
                                    string oldFileName = Path.GetFileName(nCustomerTabObj.CustomerImageAttachmentfilenameold);
                                    await DeleteFromFtp(oldFileName);
                                    await UploadToFtp(nCustomerTabObj.CustomerImageAttachmentfilename, nCustomerTabObj.CustomerImageAttachmentbase64);
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
                    { "@nType", 0 },
                    { "@nsType", 2 }
                };

                List<ExpandoObject> nDataList = await nGetDataAsync<ExpandoObject>("Customer_SP", parameters);

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

        [HttpPost("nDeleteCustomerRegistrationData")]
        public async Task<IActionResult> nDeleteCustomerRegistrationData([FromBody] CustomerTab nCustomerTabObj)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Customer_SP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 3);
                    cmd.Parameters.AddWithValue("@UserId", nCustomerTabObj.Userid);
                    cmd.Parameters.AddWithValue("@CustomerId", nCustomerTabObj.CustomerId);

                    await con.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                            int statusId = Convert.ToInt32(dr["StatusId"]);
                            if (statusId == 1)
                            {
                                if (!string.IsNullOrEmpty(nCustomerTabObj.CustomerImageAttachmentfilenameold))
                                {
                                    string oldFileName = Path.GetFileName(nCustomerTabObj.CustomerImageAttachmentfilenameold);
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


        async Task DeleteFromFtp(string attachmentFileName)
        {
            if (string.IsNullOrEmpty(attachmentFileName))
                return;

            string ftpPath = _configuration["Config:ftpPath"];
            string ftpServer = _configuration["Config:ftpServer"];
            string ftpUser = _configuration["Config:ftpUser"];
            string ftpPassword = _configuration["Config:ftpPassword"];
            string ftpPort = _configuration["Config:ftpPort"];

            string ftpUrl = $"ftp://{ftpServer}:{ftpPort}{ftpPath}/{attachmentFileName}";

            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    // Optional: log response.StatusDescription
                }
            }
            catch (WebException ex)
            {
                FtpWebResponse response = (FtpWebResponse)ex.Response;
                if (response != null && response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    // File does not exist → ignore
                }
                else
                {
                    throw; // rethrow other exceptions
                }
            }
        }
        async Task UploadToFtp(string attachmentFileName, string attachmentBase64)
        {
            string ftpPath = _configuration["Config:ftpPath"];
            string ftpServer = _configuration["Config:ftpServer"];
            string ftpUser = _configuration["Config:ftpUser"];
            string ftpPassword = _configuration["Config:ftpPassword"];
            string ftpPort = _configuration["Config:ftpPort"];

            // Remove base64 header if exists
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
                // Optional: log response.StatusDescription
            }
        }
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
                // ExpandoObject handling
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
                // Normal class handling via reflection
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
}
