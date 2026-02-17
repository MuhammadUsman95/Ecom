using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;

namespace NormalAccountProject.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DashboardController : Controller
    {
        private readonly IConfiguration _configuration;
        public DashboardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("nLoadDashboardData")]
        public async Task<IActionResult> nLoadDashboardData([FromBody] nInfoTab nInfoTabObj)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("Connection1");

                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Dashboard_SP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 0);
                    cmd.Parameters.AddWithValue("@UserId", nInfoTabObj.Userid ?? "");

                    await con.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        // -------- RESULT SET 1: All 5 Cards in ONE Row --------
                        int card1Total = 0, card2Total = 0, card3Total = 0, card4Total = 0, card5Total = 0;
                        string card1Caption = "", card2Caption = "", card3Caption = "", card4Caption = "", card5Caption = "";

                        if (await dr.ReadAsync())
                        {
                            card1Total = GetInt(dr, "Card1Tota1Value");
                            card1Caption = GetStr(dr, "Card1Caption");
                            card2Total = GetInt(dr, "Card1Tota2Value");
                            card2Caption = GetStr(dr, "Card2Caption");
                            card3Total = GetInt(dr, "Card1Tota3Value");
                            card3Caption = GetStr(dr, "Card3Caption");
                            card4Total = GetInt(dr, "Card1Tota4Value");
                            card4Caption = GetStr(dr, "Card4Caption");
                            card5Total = GetInt(dr, "Card1Tota5Value");
                            card5Caption = GetStr(dr, "Card5Caption");
                        }

                        // -------- RESULT SET 2: Summary Table (Text + Value) --------
                        await dr.NextResultAsync();

                        List<ExpandoObject> grid2Data = new List<ExpandoObject>();
                        while (await dr.ReadAsync())
                        {
                            var expando = new ExpandoObject() as IDictionary<string, object>;
                            for (int i = 0; i < dr.FieldCount; i++)
                                expando[dr.GetName(i)] = dr.IsDBNull(i) ? "" : dr.GetValue(i);
                            grid2Data.Add((ExpandoObject)expando);
                        }

                        // -------- RESULT SET 3: Main Vendor Grid --------
                        await dr.NextResultAsync();

                        List<ExpandoObject> grid1Data = new List<ExpandoObject>();
                        while (await dr.ReadAsync())
                        {
                            var expando = new ExpandoObject() as IDictionary<string, object>;
                            for (int i = 0; i < dr.FieldCount; i++)
                                expando[dr.GetName(i)] = dr.IsDBNull(i) ? "" : dr.GetValue(i);
                            grid1Data.Add((ExpandoObject)expando);
                        }

                        // -------- RESULT SET 4: Graph Data (GraphValue + GraphCaption) --------
                        List<GraphData> graphData = new List<GraphData>();
                        if (await dr.NextResultAsync())
                        {
                            while (await dr.ReadAsync())
                            {
                                graphData.Add(new GraphData
                                {
                                    GraphCaption = GetStr(dr, "GraphCaption"),
                                    GraphValue = GetInt(dr, "GraphValue")
                                });
                            }
                        }

                        var response = new
                        {
                            statusId = 1,

                            Card1TotalValue = card1Total,
                            Card1Caption = card1Caption,

                            Card2TotalValue = card2Total,
                            Card2Caption = card2Caption,

                            Card3TotalValue = card3Total,
                            Card3Caption = card3Caption,

                            Card4TotalValue = card4Total,
                            Card4Caption = card4Caption,

                            Card5TotalValue = card5Total,
                            Card5Caption = card5Caption,

                            Grid1Data = grid1Data,   // Main Vendor Table
                            Grid2Data = grid2Data,   // Summary Table (Text+Value)
                            GraphData = graphData    // Donut Chart (GraphValue+GraphCaption)
                        };

                        return Ok(response);
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(new { statusId = 0, message = "Error: " + ex.Message });
            }
        }

        // ---- Helpers (safe - no exception if column missing) ----
        private static bool HasColumn(SqlDataReader dr, string col)
        {
            for (int i = 0; i < dr.FieldCount; i++)
                if (dr.GetName(i).Equals(col, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static int GetInt(SqlDataReader dr, string col)
        {
            try { return HasColumn(dr, col) && !dr.IsDBNull(dr.GetOrdinal(col)) ? Convert.ToInt32(dr[col]) : 0; }
            catch { return 0; }
        }

        private static string GetStr(SqlDataReader dr, string col)
        {
            try { return HasColumn(dr, col) && !dr.IsDBNull(dr.GetOrdinal(col)) ? dr[col].ToString()! : ""; }
            catch { return ""; }
        }

        // ---- Models ----
        public class GraphData
        {
            public string? GraphCaption { get; set; }
            public int GraphValue { get; set; }
        }

        public class nInfoTab
        {
            public string? Userid { get; set; }
        }
    }
}
