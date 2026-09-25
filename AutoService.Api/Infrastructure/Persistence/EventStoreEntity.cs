using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoService.Api.Infrastructure.Persistence;

[Table("events")]
public class EventStoreEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid AggregateId { get; set; }

    public string AggregateType { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public long SequenceNumber { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string Payload { get; set; } = string.Empty;

    public string Metadata { get; set; } = string.Empty;
}
