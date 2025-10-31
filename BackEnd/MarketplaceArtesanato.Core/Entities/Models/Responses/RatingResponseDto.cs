namespace MarketplaceArtesanato.API.Models.Responses
{
    public class RatingResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Stars { get; set; }
        public string Review { get; set; } = string.Empty;
    }
}
