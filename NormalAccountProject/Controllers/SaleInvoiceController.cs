using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NormalAccountProject.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace NormalAccountProject.Controllers
{
    public class SaleInvoiceController : Controller
    {
        private readonly string _conn;

        public SaleInvoiceController(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection");
        }

        // GET /SaleInvoice/Index
        public IActionResult Index() => View();

        // ── Products via Inv_SaleInvoiceSP ────────────────────────────────────
        // GET /SaleInvoice/GetProducts?nCategoryId=0&nsCategoryId=0
        [HttpGet]
        public IActionResult GetProducts(int nCategoryId = 0, int nsCategoryId = 0)
        {
            try
            {
                var list = new List<object>();
                using var con = new SqlConnection(_conn);
                using var cmd = new SqlCommand("Inv_SaleInvoiceSP", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@nCategoryId", nCategoryId);
                cmd.Parameters.AddWithValue("@nsCategoryId", nsCategoryId);

                con.Open();
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    list.Add(new
                    {
                        ProductCode = rdr["ProductCode"]?.ToString(),
                        Product = rdr["Product"]?.ToString(),
                        Price = rdr["Price"],
                        DiscountAmount = rdr["DiscountAmount"]
                    });
                }
                return Json(new ApiResponse { Success = true, Data = list });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { Success = false, Message = ex.Message });
            }
        }

        // ── Customers via Inv_SaleInvoiceSP ───────────────────────────────────
        // GET /SaleInvoice/GetCustomers?nCategoryId=0&nsCategoryId=1
        [HttpGet]
        public IActionResult GetCustomers(int nCategoryId = 0, int nsCategoryId = 1)
        {
            try
            {
                var list = new List<object>();
                using var con = new SqlConnection(_conn);
                using var cmd = new SqlCommand("Inv_SaleInvoiceSP", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@nCategoryId", nCategoryId);
                cmd.Parameters.AddWithValue("@nsCategoryId", nsCategoryId);

                con.Open();
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    list.Add(new
                    {
                        CustomerCode = rdr["CustomerCode"]?.ToString(),
                        CustomerName = rdr["CustomerName"]?.ToString()
                    });
                }
                return Json(new ApiResponse { Success = true, Data = list });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { Success = false, Message = ex.Message });
            }
        }

        // ── Save Invoice ──────────────────────────────────────────────────────
        // POST /SaleInvoice/SaveInvoice
        [HttpPost]
        public IActionResult SaveInvoice([FromBody] SaveInvoiceRequest req)
        {
            if (req == null)
                return Json(new ApiResponse { Success = false, Message = "Invalid request payload." });

            if (string.IsNullOrWhiteSpace(req.CustomerName))
                return Json(new ApiResponse { Success = false, Message = "Customer name is required." });

            if (req.Items == null || req.Items.Count == 0)
                return Json(new ApiResponse { Success = false, Message = "At least one item is required." });

            try
            {
                using var con = new SqlConnection(_conn);
                con.Open();
                using var tran = con.BeginTransaction();

                int invoiceId = req.InvoiceId;

                if (invoiceId == 0)
                {
                    string insHeader = @"
                        INSERT INTO Inv_SaleInvoiceHeader
                            (InvoiceNo, InvoiceDate, CustomerName,
                             TotalSubtotal, TotalDiscount, GrandTotal, CreatedAt)
                        VALUES
                            (@InvoiceNo, @InvoiceDate, @CustomerName,
                             @TotalSubtotal, @TotalDiscount, @GrandTotal, GETDATE());
                        SELECT SCOPE_IDENTITY();";

                    using var cmdH = new SqlCommand(insHeader, con, tran);
                    BindHeaderParams(cmdH, req);
                    invoiceId = Convert.ToInt32(cmdH.ExecuteScalar());
                }
                else
                {
                    string updHeader = @"
                        UPDATE Inv_SaleInvoiceHeader SET
                            InvoiceDate    = @InvoiceDate,
                            CustomerName   = @CustomerName,
                            TotalSubtotal  = @TotalSubtotal,
                            TotalDiscount  = @TotalDiscount,
                            GrandTotal     = @GrandTotal,
                            UpdatedAt      = GETDATE()
                        WHERE InvoiceId = @InvoiceId";

                    using var cmdH = new SqlCommand(updHeader, con, tran);
                    BindHeaderParams(cmdH, req);
                    cmdH.Parameters.AddWithValue("@InvoiceId", invoiceId);
                    cmdH.ExecuteNonQuery();

                    using var delD = new SqlCommand(
                        "DELETE FROM Inv_SaleInvoiceDetail WHERE InvoiceId = @InvoiceId", con, tran);
                    delD.Parameters.AddWithValue("@InvoiceId", invoiceId);
                    delD.ExecuteNonQuery();
                }

                string insDetail = @"
                    INSERT INTO Inv_SaleInvoiceDetail
                        (InvoiceId, ProductCode, ProductName,
                         Qty, Price, Amount, DiscAmt, NetAmount)
                    VALUES
                        (@InvoiceId, @ProductCode, @ProductName,
                         @Qty, @Price, @Amount, @DiscAmt, @NetAmount)";

                foreach (var item in req.Items)
                {
                    using var cmdD = new SqlCommand(insDetail, con, tran);
                    cmdD.Parameters.AddWithValue("@InvoiceId", invoiceId);
                    cmdD.Parameters.AddWithValue("@ProductCode", item.ProductCode ?? "");
                    cmdD.Parameters.AddWithValue("@ProductName", item.ProductName ?? "");
                    cmdD.Parameters.AddWithValue("@Qty", item.Qty);
                    cmdD.Parameters.AddWithValue("@Price", item.Price);
                    cmdD.Parameters.AddWithValue("@Amount", item.Amount);
                    cmdD.Parameters.AddWithValue("@DiscAmt", item.DiscAmt);
                    cmdD.Parameters.AddWithValue("@NetAmount", item.NetAmount);
                    cmdD.ExecuteNonQuery();
                }

                tran.Commit();
                return Json(new ApiResponse
                {
                    Success = true,
                    Message = $"Invoice {req.InvoiceNo} saved successfully.",
                    Data = new { InvoiceId = invoiceId }
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { Success = false, Message = ex.Message });
            }
        }

        // ── Delete Invoice ────────────────────────────────────────────────────
        // POST /SaleInvoice/DeleteInvoice?id=5
        [HttpPost]
        public IActionResult DeleteInvoice(int id)
        {
            try
            {
                using var con = new SqlConnection(_conn);
                con.Open();
                using var tran = con.BeginTransaction();

                using var d1 = new SqlCommand(
                    "DELETE FROM Inv_SaleInvoiceDetail WHERE InvoiceId = @id", con, tran);
                d1.Parameters.AddWithValue("@id", id);
                d1.ExecuteNonQuery();

                using var d2 = new SqlCommand(
                    "DELETE FROM Inv_SaleInvoiceHeader WHERE InvoiceId = @id", con, tran);
                d2.Parameters.AddWithValue("@id", id);
                d2.ExecuteNonQuery();

                tran.Commit();
                return Json(new ApiResponse { Success = true, Message = "Invoice deleted." });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse { Success = false, Message = ex.Message });
            }
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private static void BindHeaderParams(SqlCommand cmd, SaveInvoiceRequest req)
        {
            cmd.Parameters.AddWithValue("@InvoiceNo", req.InvoiceNo ?? "");
            cmd.Parameters.AddWithValue("@InvoiceDate", req.InvoiceDate ?? DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@CustomerName", req.CustomerName ?? "");
            cmd.Parameters.AddWithValue("@TotalSubtotal", req.TotalSubtotal);
            cmd.Parameters.AddWithValue("@TotalDiscount", req.TotalDiscount);
            cmd.Parameters.AddWithValue("@GrandTotal", req.GrandTotal);
        }
    }
}
