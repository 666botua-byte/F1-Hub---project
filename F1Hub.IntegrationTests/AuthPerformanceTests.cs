using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using F1Hub.Api.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace F1Hub.IntegrationTests;

public class AuthPerformanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthPerformanceTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<F1Hub.Api.Data.AppDbContext>();
            db.Database.Migrate();
        }
    }

    [Fact]
    public async Task RegisterEndpoint_ShouldRespondInUnderOneSecond()
    {
        var request = new RegisterRequest($"test{Guid.NewGuid()}@example.com", "Passw0rd123!", "TestUser");
        var stopwatch = Stopwatch.StartNew();

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        stopwatch.Stop();

        response.EnsureSuccessStatusCode();
        Assert.True(stopwatch.ElapsedMilliseconds <= 1000, 
            $"Запит тривав {stopwatch.ElapsedMilliseconds} мс, що перевищує ліміт у 1000 мс.");
    }
}
