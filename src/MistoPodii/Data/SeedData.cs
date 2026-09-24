using Microsoft.EntityFrameworkCore;
using MistoPodii.Models;

namespace MistoPodii.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (await db.Organizers.AnyAsync())
        {
            return;
        }

        var organizers = new[]
        {
            new Organizer { Name = "Майстерня району", Description = "Міські майстерні та зустрічі для сусідів.", Contact = "hello@example.org", WebsiteUrl = "https://example.org" },
            new Organizer { Name = "Відкрита сцена", Description = "Культурні події та камерні виступи.", Contact = "stage@example.org" }
        };
        var venues = new[]
        {
            new Venue { Name = "Культурний центр на Подолі", Address = "Київ, вул. Спаська, 12", Latitude = 50.4647, Longitude = 30.5153, Capacity = 120 },
            new Venue { Name = "Сад біля бібліотеки", Address = "Київ, вул. Хорива, 19", Latitude = 50.4674, Longitude = 30.5171, Capacity = 60 }
        };

        db.Organizers.AddRange(organizers);
        db.Venues.AddRange(venues);
        await db.SaveChangesAsync();

        var start = DateTime.UtcNow.Date.AddDays(7).AddHours(15);
        db.Events.AddRange(
            new Event { Title = "Майстерня міських плакатів", Description = "Створюємо плакати для подій нашого району. Матеріали вже на місці, досвід не потрібен.", Category = "Майстерня", StartsAtUtc = start, ImageUrl = "/images/posters.svg", OrganizerId = organizers[0].Id, VenueId = venues[0].Id },
            new Event { Title = "Музика у вечірньому саду", Description = "Невеликий акустичний концерт просто неба. Візьміть із собою зручний плед.", Category = "Музика", StartsAtUtc = start.AddDays(2).AddHours(2), ImageUrl = "/images/music.svg", OrganizerId = organizers[1].Id, VenueId = venues[1].Id });
        await db.SaveChangesAsync();
    }
}
