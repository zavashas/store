using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class AgentPresence
{
    public int? AgentUserId { get; set; }

    public bool IsOnline { get; set; }

    public int Capacity { get; set; }

    public int CurrentActive { get; set; }

    public DateTime UpdatedAt { get; set; }
}
