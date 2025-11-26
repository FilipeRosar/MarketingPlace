using System.ComponentModel.DataAnnotations;

namespace MarketplaceArtesanato.Core.Settings
{
    public class AzureBlobSettings
    {
        [Required]
        public string ConnectionString { get; set; } = string.Empty;

        [Required]
        public string ContainerName { get; set; } = string.Empty;
    }
}