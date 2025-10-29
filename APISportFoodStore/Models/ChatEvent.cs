using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class ChatEvent
{
    public long? IdEvent { get; set; }

    public int ChatId { get; set; }

    public int? ActorUserId { get; set; }

    public string EventType { get; set; } = null!;

    public string? EventData { get; set; }

    public DateTime CreatedAt { get; set; }
}
