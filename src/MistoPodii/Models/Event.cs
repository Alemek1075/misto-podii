using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MistoPodii.Models;

public class Event : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Вкажіть назву події.")]
    [StringLength(160)]
    [Display(Name = "Назва")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Додайте опис події.")]
    [StringLength(3000)]
    [Display(Name = "Опис")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть категорію.")]
    [StringLength(80)]
    [Display(Name = "Категорія")]
    public string Category { get; set; } = string.Empty;

    [EnumDataType(typeof(EventStatus), ErrorMessage = "Оберіть коректний статус.")]
    [Display(Name = "Статус")]
    public EventStatus Status { get; set; } = EventStatus.Planned;

    [Display(Name = "Початок (UTC)")]
    public DateTime StartsAtUtc { get; set; }

    [Required(ErrorMessage = "Вкажіть посилання на зображення.")]
    [StringLength(500)]
    [Display(Name = "Зображення (URL)")]
    public string ImageUrl { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть організатора.")]
    [Display(Name = "Організатор")]
    public int OrganizerId { get; set; }

    [ValidateNever]
    public Organizer Organizer { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть місце.")]
    [Display(Name = "Місце")]
    public int VenueId { get; set; }

    [ValidateNever]
    public Venue Venue { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartsAtUtc == default)
        {
            yield return new ValidationResult("Вкажіть дату й час початку.", [nameof(StartsAtUtc)]);
        }

        if (string.IsNullOrWhiteSpace(ImageUrl) ||
            !ImageUrl.StartsWith("/images/", StringComparison.Ordinal) &&
            (!Uri.TryCreate(ImageUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps))
        {
            yield return new ValidationResult("Вкажіть HTTPS-посилання або шлях /images/…", [nameof(ImageUrl)]);
        }
    }
}
