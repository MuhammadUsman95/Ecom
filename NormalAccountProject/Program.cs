
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddRazorPages();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});
builder.Services.AddHttpClient();

var app = builder.Build();



app.UseStaticFiles();


app.UseRouting();


app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
    endpoints.MapControllers();

    endpoints.MapGet("/", async context =>
    {
        var baseUri = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : "";
        context.Response.Redirect($"{baseUri}/Login");
    });



    endpoints.MapFallback(async context =>
    {
        context.Response.StatusCode = 404;
        context.Response.ContentType = "text/html";

        var html = @"
            <html>
            <head><title>404 Not Found</title></head>
            <body>
                <div id='404NotFound' style='display: flex; justify-content: center; margin-top: 7%;'>
                    <img src='/images/404-NotFound.png' style='width: 30%;' />
                </div>
            </body>
            </html>";

        await context.Response.WriteAsync(html);
    });
});



app.Run();
