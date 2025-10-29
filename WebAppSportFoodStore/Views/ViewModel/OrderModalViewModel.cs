using APISportFoodStore.Models;

namespace WebAppSportFoodStore.Views.ViewModel
{
    public class OrderModalViewModel
    {
        public int UserId { get; set; }
        public List<UserAddress>? Addresses { get; set; }
        public List<UserCard>? Cards { get; set; }
        public List<DeliveryTimeSlot>? Slots { get; set; }
    }

}
