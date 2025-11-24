using Azure.Storage.Blobs;
using MarketplaceArtesanato.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class BlobService : IStorageService
    {
        private readonly BlobContainerClient _blobContainer;

        public BlobService(IConfiguration configuration)
        {
            var blobConfig = configuration.GetSection("Storage:AzureBlob");

            var connectionString = blobConfig["ConnectionString"];
            var containerName = blobConfig["ContainerName"];

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "A ConnectionString não foi encontrada em 'Storage:AzureBlob:ConnectionString'.");

            if (string.IsNullOrEmpty(containerName))
                throw new ArgumentNullException(nameof(containerName), "O ContainerName não foi encontrado em 'Storage:AzureBlob:ContainerName'.");

            _blobContainer = new BlobContainerClient(connectionString, containerName);

            _blobContainer.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
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