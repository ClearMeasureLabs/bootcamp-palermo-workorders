using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

public class RoomManageModel
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(Room.NameMaxLength)]
    public string? Name { get; set; }
}
