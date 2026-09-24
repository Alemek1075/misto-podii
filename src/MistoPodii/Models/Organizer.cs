using System.ComponentModel.DataAnnotations;

namespace MistoPodii.Models;

public class Organizer
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Вкажіть назву організатора.")]
    [StringLength(120)]
    [Display(Name = "Назва")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Додайте короткий опис.")]
    [StringLength(1000)]
    [Display(Name = "Опис")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть публічний контакт.")]
    [StringLength(160)]
    [Display(Name = "Контакт")]
    public string Contact { get; set; } = string.Empty;

    [Url(ErrorMessage = "Вкажіть повну адресу сайту, наприклад https://example.org.")]
    [StringLength(300)]
    [Display(Name = "Вебсайт")]
    public string? WebsiteUrl { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
