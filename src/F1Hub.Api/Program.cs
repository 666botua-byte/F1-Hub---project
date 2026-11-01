using F1Hub.Api.Data;
using F1Hub.Api.Services; // NEW
using Microsoft.EntityFrameworkCore;
using F1Hub.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// NEW: клієнт для F1 API
builder.Services.AddHttpClient<IF1ApiClient, F1ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["F1Api:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
    options.AddPolicy("client", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();
app.UseCors("client");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapAuthEndpoints();
app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// NEW: розклад гонок з F1 API
app.MapGet("/api/schedule/{season?}", async (string? season, IF1ApiClient f1, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await f1.GetScheduleAsync(season ?? "current", ct));
    }
    catch (F1ApiException ex)
    {
        return Results.Problem(title: "F1 API error", detail: ex.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}