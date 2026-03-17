namespace Cylo_Backend.Models.Escrow_and_Payments
{
    public class EscrowOrder
    {
        public int Id { get; set; }

       
        public int PostId { get; set; }
        public int BuyerId { get; set; }  
        public int SellerId { get; set; } 

        public decimal Amount { get; set; }
        public EscrowStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdatedAt { get; set; }
    }

    public enum EscrowStatus
    {
        Pending = 0,
        Secured = 1,
        Released = 2,
        Cancelled = 3
    }
}
