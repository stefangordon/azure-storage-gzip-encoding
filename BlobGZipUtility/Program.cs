using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BlobGZipUtility
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
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

                Utility.EnsureGzipFiles(blobContainer, options.Extensions, options.Replace, options.NewExtension, (int)TimeSpan.FromDays(30).TotalSeconds);

            }
            else
            {
                // Display the default usage information
                //Console.WriteLine(options.GetUsage());
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
