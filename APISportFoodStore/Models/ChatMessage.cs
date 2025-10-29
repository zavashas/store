using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class ChatMessage
{
    public long? IdMessage { get; set; }

    public int ChatId { get; set; }

    public int? SenderUserId { get; set; }

    public string SenderRole { get; set; } = null!;

    public string MessageType { get; set; } = null!;

    public string? Body { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? EditedAt { get; set; }

    public bool IsDeleted { get; set; }
}
