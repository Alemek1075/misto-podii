using System.ComponentModel.DataAnnotations;

namespace MistoPodii.Api;

public sealed class OrganizerInput
{
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string Contact { get; set; } = string.Empty;

    [Url, StringLength(300)]
    public string? WebsiteUrl { get; set; }
}

public sealed class VenueInput
{
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(240)]
    public string Address { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Range(1, 100000)]
    public int Capacity { get; set; }
}

public sealed class EventInput : IValidatableObject
{
    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(3000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Category { get; set; } = string.Empty;

    [Required, RegularExpression("Planned|Postponed|Cancelled")]
    public string Status { get; set; } = "Planned";

    public DateTimeOffset StartsAt { get; set; }

    [Required, StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int OrganizerId { get; set; }

    [Range(1, int.MaxValue)]
    public int VenueId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartsAt == default)
            yield return new ValidationResult("Вкажіть дату й час із часовим поясом.", [nameof(StartsAt)]);

        if (string.IsNullOrWhiteSpace(ImageUrl) ||
            !ImageUrl.StartsWith("/images/", StringComparison.Ordinal) &&
            (!Uri.TryCreate(ImageUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps))
            yield return new ValidationResult("Вкажіть HTTPS-посилання або шлях /images/…", [nameof(ImageUrl)]);
    }
}

public sealed record OrganizerResponse(int Id, string Name, string Description, string Contact, string? WebsiteUrl);
public sealed record VenueResponse(int Id, string Name, string Address, double Latitude, double Longitude, int Capacity);
public sealed record EventResponse(int Id, string Title, string Description, string Category, string Status, DateTime StartsAtUtc,
    string ImageUrl, int OrganizerId, string OrganizerName, int VenueId, string VenueName);
public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Skip, int Limit, string? NextLink);
