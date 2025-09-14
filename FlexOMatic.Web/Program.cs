var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStaticFiles(); // Serve from wwwroot

// Simple endpoint to check if submit is enabled
app.MapGet("/api/config", (IConfiguration config) =>
{
    var enabled = config.GetValue<bool>("EnableSubmit");
    return Results.Json(new { enableSubmit = enabled });
});

app.MapFallbackToFile("index.html"); // SPA fallback

app.Run();
