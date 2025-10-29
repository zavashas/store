using APISportFoodStore.Models;

namespace APISportFoodStore.Logging
{
    public interface ILogger
    {
        Serilog.ILogger GetForSession(HttpContext context, User user);

        Task LogActionRequestAsync(HttpContext context, User user);
    }
}
