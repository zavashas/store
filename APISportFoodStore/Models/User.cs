using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class User
{
    public int? IdUser { get; set; }

    public string Surname { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public bool Deleted { get; set; }

    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpires { get; set; }

}
