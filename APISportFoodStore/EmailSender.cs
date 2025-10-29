using APISportFoodStore.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;
using QRCoder;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace APISportFoodStore
{
    public sealed class EmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public EmailSender(IOptions<EmailSettings> settings)
            => _settings = settings.Value;

        public async Task SendOrderConfirmationAsync(
            string toEmail,
            string fullName,
            Order order,
            IEnumerable<(string Name, int Quantity, decimal Price)> items,
            string? paymentTokenOrUrl = null)
        {
            string qrText = $"Оплата заказа №{order.IdOrder}, сумма {order.TotalAmount:0.##} ₽";

            var qrPngBytes = BuildQrPng(qrText);

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            msg.To.Add(MailboxAddress.Parse(toEmail));
            msg.Subject = $"Ваш заказ №{order.IdOrder} оформлен";

            var builder = new BodyBuilder();

            var qr = new MimePart("image", "png")
            {
                Content = new MimeContent(new MemoryStream(qrPngBytes)),
                ContentId = MimeUtils.GenerateMessageId(),
                ContentDisposition = new MimeKit.ContentDisposition(MimeKit.ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64
            };
            builder.LinkedResources.Add(qr);

            builder.HtmlBody = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;font-size:15px;line-height:1.5;color:#222'>
                  <h2 style='margin-bottom:6px'>{_settings.Branding.Company}</h2>
                  <p>Здравствуйте, <strong>{WebUtility.HtmlEncode(fullName)}</strong>!</p>
                  <p>Спасибо за заказ в магазине <b>{_settings.Branding.Company}</b>.</p>

                  <p><strong>Номер заказа:</strong> {order.IdOrder}<br/>
                  <strong>Дата доставки:</strong> {order.DeliveryDate:dd.MM.yyyy}</p>

                  <p style='font-size:16px;margin-top:10px'><strong>Итого к оплате:</strong> {order.TotalAmount:0.##} ₽</p>

                  <h3 style='margin:16px 0 8px'>QR-код для оплаты</h3>
                  <p>Отсканируйте этот код для оплаты:</p>
                  <img src='cid:{qr.ContentId}' alt='QR-код'
                       style='width:180px;height:180px;border:1px solid #ddd;border-radius:8px;margin:8px 0' />

                  <hr style='margin-top:20px;border:none;border-top:1px solid #eee'/>
                  <p style='font-size:13px;color:#666;margin:0'>
                    По вопросам обращайтесь в поддержку:<br/>
                    <a href='mailto:{_settings.Branding.SupportEmail}'>{_settings.Branding.SupportEmail}</a><br/>
                    Телефон: {_settings.Branding.SupportPhone}
                  </p>
                </div>";


            msg.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.Smtp.Host, _settings.Smtp.Port,
                _settings.Smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
            await smtp.AuthenticateAsync(_settings.Smtp.User, _settings.Smtp.Password);
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);
        }

        private static byte[] BuildQrPng(string payload)
        {
            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            return png.GetGraphic(10);
        }
    

    public async Task SendPasswordResetAsync(string toEmail, string fullName, string resetLink)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            msg.To.Add(MailboxAddress.Parse(toEmail));
            msg.Subject = "Сброс пароля — " + _settings.Branding.Company;

            var builder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family:Segoe UI,Arial,sans-serif;font-size:15px;line-height:1.5;color:#222'>
                      <p>Здравствуйте, <strong>{WebUtility.HtmlEncode(fullName)}</strong>!</p>
                      <p>Вы запросили сброс пароля. Чтобы задать новый пароль, перейдите по ссылке:</p>
                      <p><a href='{resetLink}' target='_blank' style='display:inline-block;padding:10px 16px;border-radius:6px;background:#0d6efd;color:#fff;text-decoration:none'>Сбросить пароль</a></p>
                      <p style='color:#666'>Ссылка действует 1 час. Если вы не запрашивали сброс пароля — просто игнорируйте это письмо.</p>
                      <hr style='border:none;border-top:1px solid #eee;margin:16px 0' />
                      <p style='color:#666;font-size:12px'>Поддержка: <a href='mailto:{_settings.Branding.SupportEmail}'>{_settings.Branding.SupportEmail}</a> · {_settings.Branding.SupportPhone}</p>
                    </div>"
            };

            msg.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.Smtp.Host, _settings.Smtp.Port,
                _settings.Smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
            await smtp.AuthenticateAsync(_settings.Smtp.User, _settings.Smtp.Password);
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);
        }
    }
    public sealed class EmailSettings
    {
        public string FromName { get; set; } = "";
        public string FromAddress { get; set; } = "";
        public SmtpSettings Smtp { get; set; } = new();
        public BrandingSettings Branding { get; set; } = new();
    }

    public sealed class SmtpSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool UseStartTls { get; set; } = true;
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public sealed class BrandingSettings
    {
        public string Company { get; set; } = "FitFuel";
        public string SupportEmail { get; set; } = "";
        public string SupportPhone { get; set; } = "";
        public string SiteUrl { get; set; } = "";
    }
}

