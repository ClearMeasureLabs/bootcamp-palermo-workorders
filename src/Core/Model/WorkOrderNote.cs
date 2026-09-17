// ReSharper disable PropertyCanBeMadeInitOnly.Global -- Qodana P5 (#9440): NHibernate proxy / System.Text.Json set-by-convention requires mutable setters

namespace ClearMeasure.Bootcamp.Core.Model;

public class WorkOrderNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkOrderId { get; set; }
    public Guid AuthorId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Employee? Author { get; set; }
}
