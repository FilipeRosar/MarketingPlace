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
            {
                throw new ArgumentNullException(nameof(connectionString),
                    "A ConnectionString do Azure Blob está vazia. Verifique se o valor está preenchido no appsettings.Development.json.");
            }

            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException(nameof(containerName),
                   "O ContainerName do Azure Blob está vazio.");
            }

            _blobContainer = new BlobContainerClient(connectionString, containerName);

            try
            {
                _blobContainer.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVISO AZURE] Falha ao criar container (pode ser permissão ou emulação): {ex.Message}");
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            var fileName = $"images/{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var blobClient = _blobContainer.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = file.ContentType
            });

            return blobClient.Uri.ToString();
        }

        public async Task<string> UploadVideoAsync(IFormFile videoFile)
        {
            if (videoFile == null || videoFile.Length == 0)
                throw new ArgumentException("Vídeo inválido ou vazio.");

            if (videoFile.Length > 100 * 1024 * 1024)
                throw new ArgumentException("Vídeo muito grande. Máximo permitido: 100MB.");

            var allowedVideoTypes = new[] { "video/mp4", "video/quicktime", "video/webm", "video/x-matroska" };
            if (!allowedVideoTypes.Contains(videoFile.ContentType.ToLower()))
                throw new ArgumentException("Formato de vídeo não suportado. Use MP4, MOV, WebM ou MKV.");

            var fileName = $"videos/moments/{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(videoFile.FileName)}.mp4";

            var blobClient = _blobContainer.GetBlobClient(fileName);

            using var stream = videoFile.OpenReadStream();

            var headers = new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = "video/mp4" 
            };

            await blobClient.UploadAsync(stream, headers);

            return blobClient.Uri.ToString();
        }

        public async Task<string> UploadVideoThumbnailAsync(IFormFile thumbFile)
        {
            if (thumbFile == null || thumbFile.Length == 0)
                return string.Empty; 

            var fileName = $"videos/thumbnails/{Guid.NewGuid()}_{Path.GetFileName(thumbFile.FileName)}";
            var blobClient = _blobContainer.GetBlobClient(fileName);

            using var stream = thumbFile.OpenReadStream();
            await blobClient.UploadAsync(stream, new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = thumbFile.ContentType
            });

            return blobClient.Uri.ToString();
        }

        public async Task DeleteAsync(string blobUrl)
        {
            if (string.IsNullOrEmpty(blobUrl))
                return;

            try
            {
                var uri = new Uri(blobUrl);
                var fileName = Path.GetFileName(uri.LocalPath);
                var blobClient = _blobContainer.GetBlobClient(fileName);
                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVISO] Falha ao deletar blob {blobUrl}: {ex.Message}");
            }
        }
    }
}