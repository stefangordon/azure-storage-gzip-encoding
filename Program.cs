using System;
using System.Threading.Tasks;
using CommandLine;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace ASGE
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Set up logging
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                var result = await Parser.Default.ParseArguments<Options>(args)
                    .MapResult(
                        async (Options options) => await RunAsync(options, logger),
                        errors => Task.FromResult(1));

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception occurred");
                return 1;
            }
        }

        static async Task<int> RunAsync(Options options, ILogger logger)
        {
            try
            {
                if (string.IsNullOrEmpty(options.NewExtension) && !options.Replace)
                {
                    logger.LogError("Must provide either -r (in-place replacement) or -n (new extension/postfix to append to compressed version).");
                    return 1;
                }

                BlobServiceClient blobServiceClient;

                if (!string.IsNullOrEmpty(options.ConnectionString))
                {
                    blobServiceClient = new BlobServiceClient(options.ConnectionString);
                }
                else if (!string.IsNullOrEmpty(options.StorageAccount) && !string.IsNullOrEmpty(options.StorageKey))
                {
                    var connectionString = $"DefaultEndpointsProtocol=https;AccountName={options.StorageAccount};AccountKey={options.StorageKey};EndpointSuffix=core.windows.net";
                    blobServiceClient = new BlobServiceClient(connectionString);
                }
                else
                {
                    logger.LogError("Must provide either connection string (-c) or account name and key (-a and -k).");
                    return 1;
                }

                var containerClient = blobServiceClient.GetBlobContainerClient(options.Container);

                // Verify container exists
                try
                {
                    var exists = await containerClient.ExistsAsync();
                    if (!exists.Value)
                    {
                        logger.LogError("Container '{Container}' does not exist.", options.Container);
                        return 1;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error accessing container '{Container}'.", options.Container);
                    return 1;
                }

                // Do the compression work
                await Utility.EnsureGzipFilesAsync(containerClient, options.Extensions, options.Replace, options.NewExtension, options.MaxAgeSeconds, options.Simulate, logger);

                // Enable CORS if appropriate
                if (options.Wildcard)
                {
                    await Utility.SetWildcardCorsOnBlobServiceAsync(blobServiceClient, logger);
                }

                logger.LogInformation("Complete.");
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during execution");
                return 1;
            }
        }
    }
}