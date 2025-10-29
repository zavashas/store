using Microsoft.AspNetCore.SignalR;
using System.Net.Http.Json;

namespace ChatHubService.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IHttpClientFactory _httpFactory;
        public ChatHub(IHttpClientFactory factory) => _httpFactory = factory;

        private HttpClient Api => _httpFactory.CreateClient("ChatApi");
        private static string Group(int chatId) => $"chat-{chatId}";

        private sealed class ChatSessionDto
        {
            public int IdChat { get; set; }
            public int CustomerUserId { get; set; }
            public int? AssignedAgentId { get; set; }
            public DateTime StartedAt { get; set; }
            public DateTime? LastMessageAt { get; set; }
            public DateTime? ClosedAt { get; set; }
            public string Status { get; set; } = "";
            public int Priority { get; set; }
            public bool Deleted { get; set; }
        }
        public async Task<int> StartChat(int customerUserId)
        {
            var resp = await Api.PostAsJsonAsync("ChatSessions/start", customerUserId);
            resp.EnsureSuccessStatusCode();

            var chat = await resp.Content.ReadFromJsonAsync<ChatSessionDto>();
            if (chat is null || chat.IdChat <= 0)
                throw new HubException("Пустой или некорректный ответ API при создании чата.");

            await Groups.AddToGroupAsync(Context.ConnectionId, Group(chat.IdChat));
            return chat.IdChat;
        }

        public Task JoinChat(int chatId) =>
            Groups.AddToGroupAsync(Context.ConnectionId, Group(chatId));

        public async Task SendFromCustomer(int chatId, int userId, string text)
        {
            var message = new
            {
                chatId,
                senderUserId = (int?)userId,
                senderRole = "customer",
                messageType = "text",
                body = text
            };

            var resp = await Api.PostAsJsonAsync("ChatMessages", message);
            resp.EnsureSuccessStatusCode();

            await Clients.Group(Group(chatId)).SendAsync("ReceiveMessage", message);
        }

        public async Task SendFromConsultant(int chatId, int userId, string text)
        {
            var message = new
            {
                chatId,
                senderUserId = (int?)userId,
                senderRole = "consultant",
                messageType = "text",
                body = text
            };

            var resp = await Api.PostAsJsonAsync("ChatMessages", message);
            resp.EnsureSuccessStatusCode();

            await Clients.Group(Group(chatId)).SendAsync("ReceiveMessage", message);
        }

        public async Task ClaimChat(int chatId, int agentId)
        {
            var resp = await Api.PostAsJsonAsync($"ChatSessions/{chatId}/claim", agentId);
            resp.EnsureSuccessStatusCode();

            await Clients.All.SendAsync("ChatClaimed", new { chatId, agentId });
        }

        public async Task CloseChat(int chatId, int actorId)
        {
            var resp = await Api.PostAsJsonAsync($"ChatSessions/{chatId}/close", actorId);
            resp.EnsureSuccessStatusCode();

            await Clients.Group(Group(chatId)).SendAsync("ChatClosed", new { chatId });
        }

        public async Task CloseByCustomer(int chatId, int customerUserId)
        {
            var resp = await Api.PostAsJsonAsync($"ChatSessions/{chatId}/resolve", customerUserId);
            resp.EnsureSuccessStatusCode();

            await Clients.Group(Group(chatId)).SendAsync("ChatClosed", new { chatId });
        }
    }
}
