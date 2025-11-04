namespace MarketplaceArtesanato.API.Models.Responses
{
    public class RatingResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int Stars { get; set; }
        public string Review { get; set; } = string.Empty;
    }
}
