using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(IFormFile file);
        Task<string> UploadVideoAsync(IFormFile videoFile);
        Task<string> UploadVideoThumbnailAsync(IFormFile thumbFile);
        Task DeleteAsync(string blobUrl);
    }
}
