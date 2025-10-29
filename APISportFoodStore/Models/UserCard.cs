using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class UserCard
{
    public int? IdUserCard { get; set; }

    public int UserId { get; set; }

    public string Last4Digits { get; set; } = null!;

    public bool Deleted { get; set; }
}
