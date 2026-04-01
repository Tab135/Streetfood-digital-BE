using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace StreetFood.Services
{
    public class S3Service : IS3Service
    {
        private readonly string _bucketName;
        private readonly AmazonS3Client _s3Client;

        public S3Service(IConfiguration configuration)
        {
            _bucketName = configuration["AWS:BucketName"]!;
            var accessKey = configuration["AWS:AccessKeyId"]!;
            var secretKey = configuration["AWS:SecretAccessKey"]!;
            var region = RegionEndpoint.GetBySystemName(configuration["AWS:Region"]!);
            _s3Client = new AmazonS3Client(accessKey, secretKey, region);
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var key = $"{folder}/{uniqueFileName}";

            using var stream = file.OpenReadStream();
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(request);

            return $"https://{_bucketName}.s3.amazonaws.com/{key}";
        }
    }
}
