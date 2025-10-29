using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class ChatSession
{
    public int? IdChat { get; set; }

    public int? CustomerUserId { get; set; }

    public int? AssignedAgentId { get; set; }

    public string Status { get; set; } = null!;

    public int Priority { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public DateTime LastMessageAt { get; set; }

    public bool Deleted { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
