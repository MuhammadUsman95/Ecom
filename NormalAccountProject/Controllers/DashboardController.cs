using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

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
                    cmd.Parameters.AddWithValue("@UserId", nInfoTabObj.Userid);

                    await con.OpenAsync();

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        // ---------------- CARD 1 ----------------
                        int card1Total = 0;
                        string card1Caption = "";

                        if (await dr.ReadAsync())
                        {
                            card1Total = Convert.ToInt32(dr["Card1TotalValue"]);
                            card1Caption = dr["Card1Caption"].ToString();
                        }

                        // Move to Chart 2
                        await dr.NextResultAsync();

                        List<ChartData> chart2Data = new List<ChartData>();
                        while (await dr.ReadAsync())
                        {
                            chart2Data.Add(new ChartData
                            {
                                Text = dr["Text"].ToString(),
                                Value = Convert.ToInt32(dr["Value"])
                            });
                        }

                        // Move to Chart 3
                        await dr.NextResultAsync();

                        List<ChartData> chart3Data = new List<ChartData>();
                        while (await dr.ReadAsync())
                        {
                            chart3Data.Add(new ChartData
                            {
                                Text = dr["Text"].ToString(),
                                Value = Convert.ToInt32(dr["Value"])
                            });
                        }

                        // Move to Chart 4
                        await dr.NextResultAsync();

                        List<ChartData> chart4Data = new List<ChartData>();
                        while (await dr.ReadAsync())
                        {
                            chart4Data.Add(new ChartData
                            {
                                Text = dr["Text"].ToString(),
                                Value = Convert.ToInt32(dr["Value"])
                            });
                        }

                        // Move to Captions
                        await dr.NextResultAsync();

                        string card2Caption = "";
                        string card3Caption = "";
                        string card4Caption = "";

                        if (await dr.ReadAsync())
                        {
                            card2Caption = dr["Card2Caption"].ToString();
                            card3Caption = dr["Card3Caption"].ToString();
                            card4Caption = dr["Card4Caption"].ToString();
                        }

                        var response = new
                        {
                            statusId = 1,

                            Card1TotalValue = card1Total,
                            Card1Caption = card1Caption,

                            chart2Data = chart2Data,
                            Card2TotalValue = chart2Data.Sum(x => x.Value),
                            Card2Caption = card2Caption,

                            chart3Data = chart3Data,
                            Card3TotalValue = chart3Data.Sum(x => x.Value),
                            Card3Caption = card3Caption,

                            chart4Data = chart4Data,
                            Card4TotalValue = chart4Data.Sum(x => x.Value),
                            Card4Caption = card4Caption
                        };

                        return Ok(response);
                    }
                }
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


        public class ChartData
        {
            public string Text { get; set; }
            public int Value { get; set; }
        }
        public class nInfoTab
        {
            public string? Userid { get; set; }
        }
    }
}
