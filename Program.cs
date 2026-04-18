using LotteryCrawler.Interface;
using LotteryCrawler.Models;
using LotteryCrawler.Services;
using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Chat;
using Serilog;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);

// Bind MongoDB configuration
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDB")
);

// User Open AI
builder.Services.Configure<OpenAIOptions>(
    builder.Configuration.GetSection("OpenAI")
);

builder.Services.AddSingleton<MongoDBService>();

// Detect CLI mode and parse arguments
var isCli = args != null && args.Contains("--cli");
string? promptArg = null;
string? providerArg = null;

if (args != null)
{
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--prompt" && i + 1 < args.Length)
        {
            promptArg = args[i + 1];
        }
        else if (args[i] == "--provider" && i + 1 < args.Length)
        {
            providerArg = args[i + 1];
        }
    }
}

// Get provider from config or use default, allow CLI override
var provider = builder.Configuration["AIProvider"] ?? "Claude";
if (!string.IsNullOrEmpty(providerArg))
{
    provider = providerArg;
}

// Validate provider
provider = provider.ToLower() switch
{
    "claude" => "Claude",
    "openai" => "OpenAI",
    _ => throw new InvalidOperationException($"Unsupported AI provider: {provider}. Use 'Claude' or 'OpenAI'.")
};

// Configure Serilog using appsettings
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();


// --- Claude/OpenAI code commented out ---
// builder.Services.Configure<ClaudeOptions>(builder.Configuration.GetSection("Claude"));
// builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
// builder.Services.AddHttpClient<IClaudeService, ClaudeService>();
// builder.Services.AddHttpClient<IOpenAIService, OpenAIService>();
// builder.Services.AddScoped<IClaudeService, ClaudeService>();
// builder.Services.AddScoped<IOpenAIService, OpenAIService>();
// builder.Services.AddScoped<IAIService>(sp =>
// {
//     return provider.ToLower() switch
//     {
//         "openai" => sp.GetRequiredService<IOpenAIService>(),
//         "claude" => sp.GetRequiredService<IClaudeService>(),
//         _ => throw new InvalidOperationException($"No service implementation found for provider: {provider}")
//     };
// });

// Only register LotteryDraw service
builder.Services.AddHttpClient<ILotteryDrawService, LotteryDrawService>();
builder.Services.AddScoped<ILotteryDrawService, LotteryDrawService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<LotteryPredictionService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- CLI mode for LotteryDraw ---
if (isCli)
{
    using var scope = app.Services.CreateScope();
    var mongoService = scope.ServiceProvider.GetRequiredService<MongoDBService>();
    var openAIService = scope.ServiceProvider.GetRequiredService<LotteryPredictionService>();
    var tomorrowStations = await mongoService.GetTomorrowStationsAsync();
    await openAIService.RunPredictionAsync();
    // List tomorrow's stations
    foreach (var province in tomorrowStations)
    {
        Console.WriteLine($"Đài: {province.Name}");
    }
    ////stop app from running web server
    //var sender = new LotteryEmailSender();
    //TimeSpan targetTime = new TimeSpan(18, 08, 00); // 6:00 PM

    //Console.WriteLine("⏰ Auto email scheduler started. Waiting for 6:00 PM...");
    //while (true)
    //{
    //    DateTime now = DateTime.Now;
    //    DateTime nextRun = DateTime.Today.Add(targetTime);

    //    // If current time already passed 6:00 PM, schedule for tomorrow 
    //    if (now > nextRun)
    //        nextRun = nextRun.AddDays(1);

    //    TimeSpan waitTime = nextRun - now;
    //    Console.WriteLine($"🕕 Next send scheduled at {nextRun} (waiting {waitTime.TotalMinutes:F0} minutes)");

    //    // Wait until the next run
    //    await Task.Delay(waitTime);

    //    using var scope = app.Services.CreateScope();
    //    var drawService = scope.ServiceProvider.GetRequiredService<ILotteryDrawService>();
    //    var config = builder.Configuration.GetSection("LotteryDraw");
    //    var baseUrl = config["BaseUrl"] ?? "https://xskt.com.vn/xsmn/30-ngay";
    //    // Loop elementId from MN0 to MN29 inclusive
    //    for (int i = 0; i <= 29; i++)
    //    {
    //        string elementId = $"MN{i}";
    //        Console.WriteLine($"Crawling {baseUrl} element #{elementId}...");

    //        try
    //        {
    //            // Get lottery result in the new LotteryResult format
    //            var lotteryResult = await drawService.GetLotteryResultAsync(baseUrl, elementId);
    //            Console.WriteLine($"Found lottery result for {elementId} with {lotteryResult.Prizes.Count} provinces");

    //            // Save results as JSON into the repository-level `result/` folder
    //            var fileName = $"lottery-draw-{elementId}.json";
    //            var saved = await LotteryCrawler.Services.FileSaver.SaveJsonAsync(lotteryResult, fileName);
    //            Console.WriteLine($"Saved results to: {saved}");
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Failed to process {elementId}: {ex.Message}");
    //        }

    //        // small delay to be polite to remote server
    //        await Task.Delay(200);
    //    }
    //    //// After processing all elementIds, run prediction and save the output
    //    //try
    //    //{
    //    //    var predictionPath = await LotteryCrawler.Services.Predictor.SaveLotteryPredictionAsync();
    //    //    Console.WriteLine($"Lottery prediction saved to: {predictionPath}");
    //    //}
    //    //catch (Exception ex)
    //    //{
    //    //    Console.WriteLine($"Failed to run/save lottery prediction: {ex.Message}");
    //    //}

    //    // Deserilialize lottery-draw from MB0 to MB29 before save to DB
    //    for (int i = 0; i <= 29; i++)
    //    {
    //        string elementId = $"MN{i}";
    //        try
    //        {
    //            var fileName = $"lottery-draw-{elementId}.json";
    //            var filePath = Path.Combine("result", fileName);
    //            if (File.Exists(filePath))
    //            {
    //                string json = await File.ReadAllTextAsync(filePath);
    //                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    //                var lotteryResult = System.Text.Json.JsonSerializer.Deserialize<LotteryResult>(json, options);
    //                if (lotteryResult != null)
    //                {
    //                    var mongoService = scope.ServiceProvider.GetRequiredService<MongoDBService>();
    //                    await mongoService.SaveLotteryResultAsync(lotteryResult);
    //                    Console.WriteLine($"✅ Lottery result {elementId} saved to MongoDB.");
    //                }
    //                else
    //                {
    //                    Console.WriteLine($"❌ Failed to deserialize lottery result JSON for {elementId}.");
    //                }
    //            }
    //            else
    //            {
    //                Console.WriteLine($"❌ Lottery result file not found: {filePath}");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"❌ Error saving lottery result {elementId} to MongoDB: {ex.Message}");
    //        }
    //    }

    //    // Deserilialize lottery-prediction before save to DB
    //    try
    //    {
    //        var filePath = Path.Combine("result", "predictor", "lottery-prediction-next-day.json");
    //        if (File.Exists(filePath))
    //        {
    //            string json = await File.ReadAllTextAsync(filePath);
    //            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    //            var prediction = System.Text.Json.JsonSerializer.Deserialize<LotteryResult>(json, options);
    //            if (prediction != null)
    //            {
    //                var mongoService = scope.ServiceProvider.GetRequiredService<MongoDBService>();
    //                await mongoService.SavePredictionResultAsync(prediction);
    //                Console.WriteLine("✅ Prediction result saved to MongoDB.");
    //            }
    //            else
    //            {
    //                Console.WriteLine("❌ Failed to deserialize prediction JSON.");
    //            }
    //        }
    //        else
    //        {
    //            Console.WriteLine($"❌ Prediction file not found: {filePath}");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"❌ Error saving prediction to MongoDB: {ex.Message}");
    //    }

    //    //try
    //    //{
    //    //    Console.WriteLine($"📤 Sending email at {DateTime.Now}");
    //    //    await sender.SendLotteryPredictionEmailAsync("yukunvip21@gmail.com");
    //    //}
    //    //catch (Exception ex)
    //    //{
    //    //    Console.WriteLine($"❌ Error sending email: {ex.Message}");
    //    //}

    //    // Wait 1 minute before checking next run (avoid immediate re-trigger)
    //    //await Task.Delay(TimeSpan.FromMinutes(1));

    //    return;
}


// --- CLI mode for LotteryDraw not implemented ---
// If you want CLI for crawling, add here.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.MapControllers();

app.MapGet("/", () => "OpenAI Service is running!");

// ✅ Endpoint test CLI
app.MapPost("/api/ask", async ([FromBody] string prompt, IOpenAIService ai) =>
{ 
    var response = await ai.SendAsync(prompt);
    return Results.Ok(response);
});


app.Run();
