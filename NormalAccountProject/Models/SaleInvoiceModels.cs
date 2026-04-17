using System.Collections.Generic;

namespace NormalAccountProject.Models
{
    // ── Lookup Models ────────────────────────────────────────────
    public class ProductModel
    {
        public string ProductCode { get; set; }
        public string Product { get; set; }
    }

    public class CustomerModel
    {
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
    }

    // ── Invoice Detail Row ───────────────────────────────────────
    public class InvoiceItemModel
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }   // Qty × Price
        public decimal DiscPct { get; set; }
        public decimal DiscAmt { get; set; }
        public decimal NetAmount { get; set; }   // Amount − DiscAmt
    }

    // ── Save Invoice Request (from JS) ───────────────────────────
    public class SaveInvoiceRequest
    {
        public int InvoiceId { get; set; }   // 0 = new, >0 = update
        public string InvoiceNo { get; set; }
        public string InvoiceDate { get; set; }
        public string PaymentStatus { get; set; }
        public string CustomerName { get; set; }
        public string CustomerContact { get; set; }
        public string CustomerRef { get; set; }
        public string Remarks { get; set; }
        public decimal DeliveryCharges { get; set; }
        public decimal TotalSubtotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal GrandTotal { get; set; }

        public List<InvoiceItemModel> Items { get; set; } = new();
    }

    // ── Generic API Response ─────────────────────────────────────
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}
