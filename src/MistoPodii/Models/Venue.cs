using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MistoPodii.Models;

public class Venue
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Вкажіть назву місця.")]
    [StringLength(120)]
    [Display(Name = "Назва")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть адресу.")]
    [StringLength(240)]
    [Display(Name = "Адреса")]
    public string Address { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Широта має бути від -90 до 90.")]
    [Display(Name = "Широта")]
    [Column(TypeName = "REAL")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Довгота має бути від -180 до 180.")]
    [Display(Name = "Довгота")]
    [Column(TypeName = "REAL")]
    public double Longitude { get; set; }

    [Range(1, 100000, ErrorMessage = "Місткість має бути більшою за нуль.")]
    [Display(Name = "Місткість")]
    public int Capacity { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
