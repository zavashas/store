using APISportFoodStore.Models;
using Newtonsoft.Json;
using Serilog;
using System.Text;

namespace APISportFoodStore.Logging
{
    public class Logger : ILogger
    {
        /// <summary>
        /// возвращает логгер, настроенный на запись логов в файл, уникальный для текущей пользовательской сессии
        /// имя файла зависит от ID сессии и типа пользователя (админ/агентство).
        /// </summary>
        public Serilog.ILogger GetForSession(HttpContext context, User user)
        {
            var sessionId = context.Session.Id;
            var folder = DateTime.Now.ToString("yyyy-MM-dd");
            var userPart = user.IdUser?.ToString() ?? "unknown";
            var fileName = $"{userPart}-{sessionId}.log";
            var fullPath = Path.Combine("C:/WebAppSportFoodStore/logs/", folder, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            // конфигурируем Serilog для записи в файл
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.File(
                    path: fullPath,
                    outputTemplate: "[{Timestamp:yyyy-dd-MM HH:mm:ss}] [{Level:u3}] {Message}{NewLine}",
                    rollingInterval: RollingInterval.Infinite,
                    shared: true)
                .CreateLogger();
        }

        /// <summary>
        /// логирует информацию при вызове Action: url, query и payload)
        /// </summary>
        public async Task LogActionRequestAsync(HttpContext context, User user)
        {
            var logger = GetForSession(context, user);
            var url = context.Request.Path + context.Request.QueryString;
            var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "-";
            string payload = "-";

            // обрабатываем только action
            if (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put || context.Request.Method == HttpMethods.Patch)
            {
                context.Request.EnableBuffering();

                if (context.Request.HasFormContentType)
                {
                    var form = await context.Request.ReadFormAsync();
                    var dict = form.ToDictionary(f => f.Key, f => f.Value.ToString());

                    // маскируем необходимые поля
                    var keysToMask = new[] { "password", "token", "smart-token" };
                    foreach (var key in keysToMask)
                    {
                        if (dict.ContainsKey(key))
                        {
                            dict[key] = "***";
                        }
                    }
                    payload = JsonConvert.SerializeObject(dict);
                }
                else if (context.Request.ContentType?.Contains("application/json") == true)
                {
                    // считываем json тело
                    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                    payload = await reader.ReadToEndAsync();
                }

                context.Request.Body.Position = 0;
            }

            logger.Information(
                "Action request: URL: {Url} Query: {Query} Payload: {Payload}", url, query, payload);
        }
    }
}
