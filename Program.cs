using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Serilog;

namespace ASGE
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var result = await CommandLine.Parser.Default.ParseArguments<Options>(args)
                .MapResult(
                    async opts => await RunOptionsAndReturnExitCode(opts),
                    async errs => await HandleParseError(errs));
            Log.Information("Return code= {0}", result);
        }
        static async Task<int> RunOptionsAndReturnExitCode(Options options)
        {
            if (string.IsNullOrEmpty(options.NewExtension) && !options.Replace)
            {
                Log.Error("Must provide either -r (in-place replacement) or -n (new extension/postfix to append to compressed version).");
                return -1;
            }

            CloudStorageAccount storageAccount;

            if (!string.IsNullOrEmpty(options.ConnectionString))
            {
                storageAccount = CloudStorageAccount.Parse(options.ConnectionString);
            }
            else if (!string.IsNullOrEmpty(options.StorageAccount) && !string.IsNullOrEmpty(options.StorageKey))
            {
                storageAccount = new CloudStorageAccount(new StorageCredentials(options.StorageAccount, options.StorageKey), true);
            }
            else
            {
                Log.Error("Must provide either storagAccount+storageKey or connectionString.");
                return -1;
            }

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(options.Container);

            // Do the compression work
            await Utility.EnsureGzipFiles(blobContainer, options.Extensions, options.Replace, options.NewExtension, options.MaxAgeSeconds, options.Simulate);

            // Enable CORS if appropriate
            if (options.wildcard)
            {
                await Utility.SetWildcardCorsOnBlobService(storageAccount);
            }

            Log.Information("Complete.");
            return 0;
        }

        //in case of errors or --help or --version
        static async Task<int> HandleParseError(IEnumerable<Error> errs)
        {
            return await Task.FromResult(-1);
        }
    }
}
