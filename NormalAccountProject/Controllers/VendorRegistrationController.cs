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
    public class VendorRegistrationController : Controller
    {
        private readonly IConfiguration _configuration;
        private string connectionString;

        public VendorRegistrationController(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("Connection1");
        }

        [HttpPost("nLoadVendorRegistrationData")]
        public async Task<IActionResult> nLoadVendorRegistrationData([FromBody] nInfoTab nInfoTabObj)
        {
            try
            {
                // Load Type List
                var typeParameters = new Dictionary<string, object>
                {
                    { "@nType", 0 },
                    { "@nsType", 1 }
                };
                List<VendorTypedd> nTypeList = await nGetDataAsync<VendorTypedd>("Ecom_VendorSP", typeParameters);

                // Load Department List
                var departmentParameters = new Dictionary<string, object>
                {
                    { "@nType", 0 },
                    { "@nsType", 5 }
                };
                List<DepartmentDD> nDepartmentList = await nGetDataAsync<DepartmentDD>("Ecom_VendorSP", departmentParameters);

                var response = new
                {
                    statusId = 1,
                    TypeList = nTypeList,
                    DepartmentList = nDepartmentList
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

        [HttpPost("nSaveVendorRegistrationData")]
        public async Task<IActionResult> nSaveVendorRegistrationData([FromBody] Ecom_VendorTab nEcom_VendorTabObj)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Ecom_VendorSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 0);
                    cmd.Parameters.AddWithValue("@Vendor", nEcom_VendorTabObj.Vendor);
                    cmd.Parameters.AddWithValue("@ContactNo", nEcom_VendorTabObj.ContactNo);
                    cmd.Parameters.AddWithValue("@IsActive", nEcom_VendorTabObj.IsActive ? "1" : "0");
                    cmd.Parameters.AddWithValue("@Type", nEcom_VendorTabObj.Type);
                    cmd.Parameters.AddWithValue("@DepartmentId", nEcom_VendorTabObj.DepartmentId);
                    cmd.Parameters.AddWithValue("@UserId", nEcom_VendorTabObj.Userid);
                    cmd.Parameters.AddWithValue("@IsUpdate", nEcom_VendorTabObj.IsUpdate ? "1" : "0");
                    cmd.Parameters.AddWithValue("@ImagePath", nEcom_VendorTabObj.VendorImageAttachmentfilename);

                    cmd.Parameters.AddWithValue("@TimeIn", nEcom_VendorTabObj.TimeIn);
                    cmd.Parameters.AddWithValue("@TimeOut", nEcom_VendorTabObj.TimeOut);
                    cmd.Parameters.AddWithValue("@DeliveryCharges", nEcom_VendorTabObj.DeliveryCharges);
                    cmd.Parameters.AddWithValue("@PerProductAmount", nEcom_VendorTabObj.PerProductAmount);

                    if (nEcom_VendorTabObj.IsUpdate)
                    {
                        cmd.Parameters.AddWithValue("@VendorId", nEcom_VendorTabObj.VendorId);
                    }

                    // 🔹 Build SQL exec line for debugging
                    string sqlDebug = $"EXEC Ecom_VendorSP " +
                                      $"@nType=0, " +
                                      $"@nsType=0, " +
                                      $"@Vendor='{nEcom_VendorTabObj.Vendor}', " +
                                      $"@ContactNo='{nEcom_VendorTabObj.ContactNo}', " +
                                      $"@IsActive='{(nEcom_VendorTabObj.IsActive ? "1" : "0")}', " +
                                      $"@Type='{nEcom_VendorTabObj.Type}', " +
                                      $"@DepartmentId='{nEcom_VendorTabObj.DepartmentId}', " +
                                      $"@UserId='{nEcom_VendorTabObj.Userid}', " +
                                      $"@IsUpdate='{(nEcom_VendorTabObj.IsUpdate ? "1" : "0")}', " +
                                      $"@ImagePath='{nEcom_VendorTabObj.VendorImageAttachmentfilename}', " +
                                      $"@TimeIn='{nEcom_VendorTabObj.TimeIn}', " +
                                      $"@TimeOut='{nEcom_VendorTabObj.TimeOut}', " +
                                      $"@DeliveryCharges='{nEcom_VendorTabObj.DeliveryCharges}', " +
                                      $"@PerProductAmount='{nEcom_VendorTabObj.PerProductAmount}'";

                    if (nEcom_VendorTabObj.IsUpdate)
                    {
                        sqlDebug += $", @VendorId='{nEcom_VendorTabObj.VendorId}'";
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
                                if (!string.IsNullOrEmpty(nEcom_VendorTabObj.VendorImageAttachmentfilename))
                                {
                                    string oldFileName = Path.GetFileName(nEcom_VendorTabObj.VendorImageAttachmentfilenameold);
                                    await DeleteFromFtp(oldFileName);
                                    await UploadToFtp(nEcom_VendorTabObj.VendorImageAttachmentfilename, nEcom_VendorTabObj.VendorImageAttachmentbase64);
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

                List<ExpandoObject> nDataList = await nGetDataAsync<ExpandoObject>("Ecom_VendorSP", parameters);

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

        [HttpPost("nDeleteVendorRegistrationData")]
        public async Task<IActionResult> nDeleteVendorRegistrationData([FromBody] Ecom_VendorTab nEcom_VendorTabObj)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Ecom_VendorSP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 3);
                    cmd.Parameters.AddWithValue("@UserId", nEcom_VendorTabObj.Userid);
                    cmd.Parameters.AddWithValue("@VendorId", nEcom_VendorTabObj.VendorId);

                    await con.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                            int statusId = Convert.ToInt32(dr["StatusId"]);
                            if (statusId == 1)
                            {
                                if (!string.IsNullOrEmpty(nEcom_VendorTabObj.VendorImageAttachmentfilenameold))
                                {
                                    string oldFileName = Path.GetFileName(nEcom_VendorTabObj.VendorImageAttachmentfilenameold);
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
