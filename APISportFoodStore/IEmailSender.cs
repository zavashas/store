using APISportFoodStore.Models;

namespace APISportFoodStore
{
    public interface IEmailSender
    {
        /// <summary>
        /// Отправка письма с подтверждением заказа.
        /// </summary>
        /// <param name="toEmail">Почта получателя</param>
        /// <param name="fullName">Имя клиента</param>
        /// <param name="order">Данные заказа</param>
        /// <param name="items">Список товаров: Название, Количество, Цена</param>
        /// <param name="paymentTokenOrUrl">Необязательная ссылка на оплату (для учебного проекта можно null)</param>
        Task SendOrderConfirmationAsync(
            string toEmail,
            string fullName,
            Order order,
            IEnumerable<(string Name, int Quantity, decimal Price)> items,
            string? paymentTokenOrUrl = null);

        Task SendPasswordResetAsync(string toEmail, string fullName, string resetLink);

    }
}
