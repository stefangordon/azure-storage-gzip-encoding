using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Serilog;

namespace ASGE
{
    static class Utility
    {
        public static async Task EnsureGzipFiles(CloudBlobContainer container, IEnumerable<string> extensions, bool inPlace, string newExtension, int cacheControlMaxAgeSeconds, bool simulate)
        {
            Log.Information("Enumerating files.");

            string cacheControlHeader = "public, max-age=" + cacheControlMaxAgeSeconds.ToString();

            BlobContinuationToken blobContinuationToken = null;
            do
            {
                var resultSegment = await container.ListBlobsSegmentedAsync(
                    prefix: null,
                    useFlatBlobListing: true,
                    blobListingDetails: BlobListingDetails.Metadata,
                    maxResults: null,
                    currentToken: blobContinuationToken,
                    options: null,
                    operationContext: null
                );

                blobContinuationToken = resultSegment.ContinuationToken;
                await resultSegment.Results.ForEachAsync(
                    async (blobInfo) => 
                        await EnsureGzipOneFile(container, extensions, inPlace, newExtension, simulate, blobInfo, cacheControlHeader));

            } while (blobContinuationToken != null); // Loop while the continuation token is not null.
        }

        private static async Task EnsureGzipOneFile(CloudBlobContainer container, IEnumerable<string> extensions, bool inPlace, string newExtension, bool simulate, IListBlobItem blobInfo, string cacheControlHeader)
        {
            CloudBlob gzipBlob = null;
            CloudBlob blob = (CloudBlob)blobInfo;

            // Only work with desired extensions
            string extension = Path.GetExtension(blobInfo.Uri.LocalPath);
            if (!extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            // Check if it is already done
            if (inPlace)
            {
                if (string.Equals(blob.Properties.ContentEncoding, "gzip", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("Skipping already compressed blob: " + blob.Name);
                    return;
                }
            }
            else
            {
                string gzipUrl = blob.Name + newExtension;
                gzipBlob = container.GetBlockBlobReference(gzipUrl);

                if (await gzipBlob.ExistsAsync())
                {
                    Log.Information("Skipping already compressed blob: " + blob.Name);
                    return;
                }
            }

            // Compress blob contents
            Log.Information("Downloading blob: " + blob.Name);

            byte[] compressedBytes;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                using (var blobStream = await blob.OpenReadAsync())
                {
                    blobStream.CopyTo(gzipStream);
                }

                compressedBytes = memoryStream.ToArray();
            }

            // Blob to write to 
            CloudBlockBlob destinationBlob;

            if (inPlace)
            {
                destinationBlob = (CloudBlockBlob)blob;
            }
            else
            {
                destinationBlob = (CloudBlockBlob)gzipBlob;
            }

            if (simulate)
            {
                Log.Information("NOT writing blob, due to simulation: " + blob.Name);
            }
            else
            {
                // Upload the compressed bytes to the new blob
                Log.Information("Writing blob: " + blob.Name);
                await destinationBlob.UploadFromByteArrayAsync(compressedBytes, 0, compressedBytes.Length);

                // Set the blob headers
                Log.Information("Configuring headers");
                destinationBlob.Properties.CacheControl = cacheControlHeader;
                destinationBlob.Properties.ContentType = blob.Properties.ContentType;
                destinationBlob.Properties.ContentEncoding = "gzip";
                await destinationBlob.SetPropertiesAsync();
            }
        }

        public static async Task SetWildcardCorsOnBlobService(this CloudStorageAccount storageAccount)
        {
            await storageAccount.SetCORSPropertiesOnBlobService(cors =>
            {
                var wildcardRule = new CorsRule() { AllowedMethods = CorsHttpMethods.Get, AllowedOrigins = { "*" } };
                cors.CorsRules.Clear();
                cors.CorsRules.Add(wildcardRule);
                return cors;
            });
        }            

        public static async Task SetCORSPropertiesOnBlobService(this CloudStorageAccount storageAccount,
            Func<CorsProperties, CorsProperties> alterCorsRules)
        {
            Log.Information("Configuring CORS.");

            if (storageAccount == null || alterCorsRules == null) throw new ArgumentNullException();

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            ServiceProperties serviceProperties = await blobClient.GetServicePropertiesAsync();

            serviceProperties.Cors = alterCorsRules(serviceProperties.Cors) ?? new CorsProperties();

            await blobClient.SetServicePropertiesAsync(serviceProperties);
        }
    }
}
