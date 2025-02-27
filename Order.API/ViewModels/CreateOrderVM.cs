namespace Order.API.ViewModels
{
    public class CreateOrderVM
    {
        public int BuyerId { get; set; }
        public List<OrterItemVM> OrderItem { get; set; } 
        
    }
}
