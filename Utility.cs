using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace ASGE
{
    public static class Utility
    {
        public static async Task EnsureGzipFilesAsync(BlobContainerClient containerClient, IEnumerable<string> extensions, bool inPlace, string? newExtension, int cacheControlMaxAgeSeconds, bool simulate, ILogger logger)
        {
            logger.LogInformation("Enumerating files.");

            string cacheControlHeader = $"public, max-age={cacheControlMaxAgeSeconds}";

            var blobs = containerClient.GetBlobsAsync(BlobTraits.Metadata);

            var tasks = new List<Task>();
            await foreach (var blobItem in blobs)
            {
                tasks.Add(ProcessBlobAsync(containerClient, blobItem, extensions, inPlace, newExtension, cacheControlHeader, simulate, logger));
                
                // Process in batches to avoid overwhelming the service
                if (tasks.Count >= 10)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        private static async Task ProcessBlobAsync(BlobContainerClient containerClient, BlobItem blobItem, IEnumerable<string> extensions, bool inPlace, string? newExtension, string cacheControlHeader, bool simulate, ILogger logger)
        {
            // Only work with desired extensions
            string extension = Path.GetExtension(blobItem.Name);
            if (!extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            BlobClient? gzipBlobClient = null;

            // Check if it is already done
            if (inPlace)
            {
                if (string.Equals(blobItem.Properties.ContentEncoding, "gzip", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Skipping already compressed blob: {BlobName}", blobItem.Name);
                    return;
                }
            }
            else
            {
                string gzipBlobName = blobItem.Name + newExtension;
                gzipBlobClient = containerClient.GetBlobClient(gzipBlobName);

                try
                {
                    var exists = await gzipBlobClient.ExistsAsync();
                    if (exists.Value)
                    {
                        logger.LogInformation("Skipping already compressed blob: {BlobName}", blobItem.Name);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error checking if blob exists: {BlobName}", gzipBlobName);
                    return;
                }
            }

            try
            {
                // Compress blob contents
                logger.LogInformation("Downloading blob: {BlobName}", blobItem.Name);

                byte[] compressedBytes;
                string contentType = blobItem.Properties.ContentType ?? "application/octet-stream";

                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                    {
                        var downloadInfo = await blobClient.DownloadStreamingAsync();
                        await downloadInfo.Value.Content.CopyToAsync(gzipStream);
                    }

                    compressedBytes = memoryStream.ToArray();
                }

                // Blob to write to 
                BlobClient destinationBlobClient = inPlace ? blobClient : gzipBlobClient!;

                if (simulate)
                {
                    logger.LogInformation("NOT writing blob, due to simulation: {BlobName}", blobItem.Name);
                }
                else
                {
                    // Upload the compressed bytes to the new blob
                    logger.LogInformation("Writing blob: {BlobName}", blobItem.Name);

                    using var uploadStream = new MemoryStream(compressedBytes);
                    var uploadOptions = new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            CacheControl = cacheControlHeader,
                            ContentType = contentType,
                            ContentEncoding = "gzip"
                        }
                    };

                    await destinationBlobClient.UploadAsync(uploadStream, uploadOptions, cancellationToken: CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing blob: {BlobName}", blobItem.Name);
            }
        }

        public static async Task SetWildcardCorsOnBlobServiceAsync(BlobServiceClient blobServiceClient, ILogger logger)
        {
            logger.LogInformation("Configuring CORS.");

            try
            {
                var serviceProperties = await blobServiceClient.GetPropertiesAsync();
                
                var corsRules = new List<BlobCorsRule>
                {
                    new BlobCorsRule
                    {
                        AllowedMethods = "GET",
                        AllowedOrigins = "*",
                        AllowedHeaders = "*",
                        ExposedHeaders = "*",
                        MaxAgeInSeconds = 3600
                    }
                };

                serviceProperties.Value.Cors.Clear();
                foreach (var rule in corsRules)
                {
                    serviceProperties.Value.Cors.Add(rule);
                }

                await blobServiceClient.SetPropertiesAsync(serviceProperties.Value);
                logger.LogInformation("CORS configuration completed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error configuring CORS");
                throw;
            }
        }
    }
}