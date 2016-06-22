using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BlobGZipUtility
{
    class Utility
    {


        public static void EnsureGzipFiles(CloudBlobContainer container, IEnumerable<string> extensions, bool inPlace, string newExtension, int cacheControlMaxAgeSeconds)
        {
            Trace.TraceInformation("Enumerating files.");

            string cacheControlHeader = "public, max-age=" + cacheControlMaxAgeSeconds.ToString();

            var blobInfos = container.ListBlobs(null, true);
            //Parallel.ForEach(blobInfos, (blobInfo) =>
            foreach(var blobInfo in blobInfos)
            {
                CloudBlob gzipBlob = null;
                string blobUrl = blobInfo.Uri.ToString();
                CloudBlob blob = (CloudBlob)blobInfo;

                // Only work with desired extensions
                string extension = Path.GetExtension(blobInfo.Uri.LocalPath);
                if (!extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Check if it is already done
                if (inPlace)
                {
                    if (string.Equals(blob.Properties.ContentEncoding, "gzip", StringComparison.OrdinalIgnoreCase))
                    {
                        Trace.TraceInformation("Skipping already compressed blob: " + blob.Name);
                        continue;
                    }
                }
                else
                {
                    string gzipUrl = blobUrl + newExtension;
                    gzipBlob = container.GetBlobReference(gzipUrl);

                    if (gzipBlob.Exists())
                    {
                        Trace.TraceInformation("Skipping already compressed blob: " + blob.Name);
                        continue;
                    }
                }

                // create a gzip version of the file
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    // push the original blob into the gzip stream
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                    using (var blobStream = blob.OpenRead())
                    {
                        blobStream.CopyTo(gzipStream);
                    }

                    // the gzipStream MUST be closed before its safe to read from the memory stream
                    byte[] compressedBytes = memoryStream.ToArray();

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

                    Trace.TraceInformation("Writing blob: " + blob.Name);

                    // upload the compressed bytes to the new blob
                    destinationBlob.UploadFromByteArray(compressedBytes, 0, compressedBytes.Length);

                    // set the blob headers
                    destinationBlob.Properties.CacheControl = cacheControlHeader;
                    destinationBlob.Properties.ContentType = blob.Properties.ContentType;
                    destinationBlob.Properties.ContentEncoding = "gzip";
                    destinationBlob.SetProperties();
                }
            }//);
        }
    }
}
