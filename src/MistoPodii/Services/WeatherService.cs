using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using MistoPodii.Models;

namespace MistoPodii.Services;

public sealed record DailyForecast(double MinCelsius, double MaxCelsius, int? RainChancePercent);
public sealed record WeatherResult(DailyForecast? Forecast, string Message);

public sealed class WeatherService(HttpClient client, IMemoryCache cache)
{
    public async Task<WeatherResult> GetForEventAsync(Event item, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(item.StartsAtUtc);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (day < today) return new WeatherResult(null, "Подія вже відбулася.");
        if (day > today.AddDays(15)) return new WeatherResult(null, "Прогноз з'явиться ближче до дати події.");

        var key = $"weather:{item.VenueId}:{day:yyyy-MM-dd}";
        if (cache.TryGetValue<DailyForecast>(key, out var cached) && cached is not null)
            return new WeatherResult(cached, string.Empty);

        var lat = item.Venue.Latitude.ToString("G17", CultureInfo.InvariantCulture);
        var lon = item.Venue.Longitude.ToString("G17", CultureInfo.InvariantCulture);
        var date = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var path = $"v1/forecast?latitude={lat}&longitude={lon}&daily=temperature_2m_max,temperature_2m_min,precipitation_probability_max&timezone=UTC&start_date={date}&end_date={date}";
        try
        {
            using var response = await client.GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var data = await JsonSerializer.DeserializeAsync<OpenMeteoResponse>(stream, cancellationToken: cancellationToken);
            var daily = data?.Daily;
            if (daily?.MinTemperature is not { Length: > 0 } min ||
                daily.MaxTemperature is not { Length: > 0 } max ||
                min[0] is null || max[0] is null)
                return new WeatherResult(null, "Прогноз для цієї дати тимчасово недоступний.");

            var rain = daily.RainChance is { Length: > 0 } ? daily.RainChance[0] : null;
            var forecast = new DailyForecast(min[0]!.Value, max[0]!.Value, rain);
            cache.Set(key, forecast, TimeSpan.FromHours(1));
            return new WeatherResult(forecast, string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return new WeatherResult(null, "Сервіс прогнозу зараз недоступний.");
        }
    }

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("daily")]
        public DailyData? Daily { get; set; }
    }

    private sealed class DailyData
    {
        [JsonPropertyName("temperature_2m_min")]
        public double?[]? MinTemperature { get; set; }

        [JsonPropertyName("temperature_2m_max")]
        public double?[]? MaxTemperature { get; set; }

        [JsonPropertyName("precipitation_probability_max")]
        public int?[]? RainChance { get; set; }
    }
}
