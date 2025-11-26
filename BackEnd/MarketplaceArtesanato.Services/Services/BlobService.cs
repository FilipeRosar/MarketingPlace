using Azure.Storage.Blobs;
using MarketplaceArtesanato.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options; 
using MarketplaceArtesanato.Core.Settings; 
using System;
using System.IO;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class BlobService : IStorageService
    {
        private readonly BlobContainerClient _blobContainer;

        public BlobService(IOptions<AzureBlobSettings> settings)
        {
            var blobSettings = settings.Value;

            var connectionString = blobSettings.ConnectionString;
            var containerName = blobSettings.ContainerName;

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "A ConnectionString não foi encontrada em 'Storage:AzureBlob:ConnectionString'.");

            _blobContainer = new BlobContainerClient(connectionString, containerName);

            try
            {
                _blobContainer.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVISO AZURE] Falha ao criar container publicamente. Verifique permissões: {ex.Message}");
                _blobContainer.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.None);
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var blobClient = _blobContainer.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = file.ContentType });

            return blobClient.Uri.ToString();
        }

        public Task DeleteAsync(string blobUrl)
        {
            if (string.IsNullOrEmpty(blobUrl)) return Task.CompletedTask;

            try
            {
                var fileName = Path.GetFileName(new Uri(blobUrl).LocalPath);
                var blobClient = _blobContainer.GetBlobClient(fileName);
                return blobClient.DeleteIfExistsAsync();
            }
            catch (UriFormatException)
            {
                return Task.CompletedTask;
            }
        }
    }
}