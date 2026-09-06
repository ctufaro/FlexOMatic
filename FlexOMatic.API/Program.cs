var builder = WebApplication.CreateBuilder(args);

// Load config values
var polisherProvider = builder.Configuration["PromptPolisher:Provider"]?.ToLower();
var sanitizerProvider = builder.Configuration["PromptSanitizer:Provider"]?.ToLower(); // NEW

// Universal HttpClient registration
builder.Services.AddHttpClient();

// Register the appropriate PromptPolisher based on config
builder.Services.AddScoped<IPromptPolisher>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var client = clientFactory.CreateClient();

    return polisherProvider switch
    {
        "groq" => new GroqPromptPolisher(client, config),
        "openai" => new OpenAIPromptPolisher(client, config),
        _ => throw new InvalidOperationException("Invalid PromptPolisher.Provider value. Use 'Groq' or 'OpenAI'.")
    };
});

// Register the PromptSanitizer service
builder.Services.AddScoped<IPromptSanitizer>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var client = clientFactory.CreateClient();

    // You can support other providers later like "GroqSanitizer" if needed
    return sanitizerProvider switch
    {
        "openai" or null => new PromptSanitizerService(client, config),
        _ => throw new InvalidOperationException("Invalid PromptSanitizer.Provider value. Use 'OpenAI'.")
    };
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new AzureBlobUploader(
        config["Azure:BlobConnectionString"],
        config["Azure:ContainerName"]
    );
});


builder.Services.AddScoped<FlexCropRepository>();

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5105") // <-- your frontend dev port
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Expose submit toggle to frontend
app.MapGet("/api/config", (IConfiguration config) =>
{
    var enabled = config.GetValue<bool>("EnableSubmit");
    return Results.Json(new { enableSubmit = enabled });
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS must come before routing/mapping
app.UseCors("AllowWebFrontend");

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
