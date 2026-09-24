using System.ComponentModel.DataAnnotations;

namespace MistoPodii.Models;

public enum EventStatus
{
    [Display(Name = "Заплановано")]
    Planned,

    [Display(Name = "Перенесено")]
    Postponed,

    [Display(Name = "Скасовано")]
    Cancelled
}

public static class EventStatusLabels
{
    public static string Get(EventStatus status) => status switch
    {
        EventStatus.Planned => "Заплановано",
        EventStatus.Postponed => "Перенесено",
        EventStatus.Cancelled => "Скасовано",
        _ => "Невідомий статус"
    };
}
