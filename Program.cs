using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ASGE
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                if (string.IsNullOrEmpty(options.NewExtension) && !options.Replace)
                {
                    Console.WriteLine("Must provide either -r (in-place replacement) or -n (new extension/postfix to append to compressed version).");
                    return;
                }

                CloudStorageAccount storageAccount;

                if (!string.IsNullOrEmpty(options.ConnectionString))
                {
                    storageAccount = CloudStorageAccount.Parse(options.ConnectionString);
                }
                else if (!string.IsNullOrEmpty(options.StorageAccount) && !String.IsNullOrEmpty(options.StorageKey))
                {
                    storageAccount = new CloudStorageAccount(new StorageCredentials(options.StorageAccount, options.StorageKey), true);        
                }
                else
                {
                    options.GetUsage();
                    return;
                }

                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer blobContainer = blobClient.GetContainerReference(options.Container);

                // Do the compression work
                Utility.EnsureGzipFiles(blobContainer, options.Extensions, options.Replace, options.NewExtension, options.MaxAgeSeconds, options.Simulate);

                // Enable CORS if appropriate
                if (options.wildcard)
                {
                    Utility.SetWildcardCorsOnBlobService(storageAccount);
                }

                Trace.TraceInformation("Complete.");                
            }
        }
    }
}
