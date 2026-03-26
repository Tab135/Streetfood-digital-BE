namespace StreetFood.Services
{
    public interface IS3Service
    {
        Task<string> UploadFileAsync(IFormFile file, string folder);
    }
}
