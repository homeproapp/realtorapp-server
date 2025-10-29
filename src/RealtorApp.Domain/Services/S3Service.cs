using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using RealtorApp.Contracts.Common.Requests;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Settings;

namespace RealtorApp.Domain.Services;

public class S3Service(AppSettings appSettings, ILogger<S3Service> logger) : IS3Service
{
    private readonly AppSettings _appSettings = appSettings;
    private readonly ILogger<S3Service> _logger = logger;

    public async Task<FileUploadResponseDto> UploadFileAsync(string key, FileUploadRequest fileUploadRequest, string folderName = "")
    {
        try
        {
            var credentials = new Amazon.Runtime.BasicAWSCredentials(
                _appSettings.Aws.AccessKey,
                _appSettings.Aws.SecretKey
            );

            var region = RegionEndpoint.GetBySystemName(_appSettings.Aws.S3.Region);
            using var s3Client = new AmazonS3Client(credentials, region);

            var request = new PutObjectRequest
            {
                BucketName = _appSettings.Aws.S3.ImagesBucketName,
                Key = folderName + key,
                InputStream = fileUploadRequest.Content,
                ContentType = fileUploadRequest.ContentType,
            };

            await s3Client.PutObjectAsync(request);

            _logger.LogInformation("Successfully uploaded file to S3: {Key}", key);

            return new()
            {
                OriginalRequest = fileUploadRequest,
                FileKey = key,
                Successful = true
            };        
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3 error uploading file: {Key}", key);
            return new()
            {
                OriginalRequest = fileUploadRequest,
                FileKey = key,
                Successful = false
            };        
        }

    }

    public async Task<(Stream? FileStream, string? ContentType)> GetFileAsync(string key)
    {
        try
        {
            var credentials = new Amazon.Runtime.BasicAWSCredentials(
                _appSettings.Aws.AccessKey,
                _appSettings.Aws.SecretKey
            );

            var region = RegionEndpoint.GetBySystemName(_appSettings.Aws.S3.Region);
            using var s3Client = new AmazonS3Client(credentials, region);

            var request = new GetObjectRequest
            {
                BucketName = _appSettings.Aws.S3.ImagesBucketName,
                Key = key
            };

            var response = await s3Client.GetObjectAsync(request);
            var contentType = response.Headers.ContentType ?? "application/octet-stream";

            _logger.LogInformation("Successfully retrieved file from S3: {Key}", key);

            return (response.ResponseStream, contentType);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error retrieving file: {Key}", key);
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file: {Key}", key);
            return (null, null);
        }
    }
}
