using System.Text.Json;
using System.Text.Json.Serialization;

namespace F1Hub.Api.Services;

public class F1ApiException : Exception
{
    public F1ApiException(string message, Exception? inner = null) : base(message, inner) { }
}

public record RaceDto(
    [property: JsonPropertyName("season")] string Season,
    [property: JsonPropertyName("round")] string Round,
    [property: JsonPropertyName("raceName")] string RaceName,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("time")] string? Time,
    [property: JsonPropertyName("Circuit")] CircuitDto Circuit);

public record CircuitDto(
    [property: JsonPropertyName("circuitName")] string CircuitName,
    [property: JsonPropertyName("Location")] LocationDto Location);

public record LocationDto(
    [property: JsonPropertyName("locality")] string Locality,
    [property: JsonPropertyName("country")] string Country);

public interface IF1ApiClient
{
    Task<List<RaceDto>> GetScheduleAsync(string season, CancellationToken ct);
}

public class F1ApiClient(HttpClient http) : IF1ApiClient
{
    public async Task<List<RaceDto>> GetScheduleAsync(string season, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync($"{season}.json", ct);

            if (!response.IsSuccessStatusCode)
                throw new F1ApiException($"F1 API returned {(int)response.StatusCode}");

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var races = doc.RootElement
                .GetProperty("MRData").GetProperty("RaceTable").GetProperty("Races")
                .Deserialize<List<RaceDto>>() ?? new();

            if (races.Count == 0)
                throw new F1ApiException($"No races found for season '{season}'");

            return races;
        }
        catch (HttpRequestException ex)
        {
            throw new F1ApiException("F1 API is unreachable", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new F1ApiException("F1 API request timed out", ex);
        }
        catch (JsonException ex)
        {
            throw new F1ApiException("F1 API returned invalid data", ex);
        }
        catch (KeyNotFoundException ex)
        {
            throw new F1ApiException("F1 API response has unexpected format", ex);
        }
    }
}
