using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace ASGE
{
    class Options
    {
        [Option('a', "account", Required = false, SetName = "Account and Key",
          HelpText = "Storage account host. [mystorage]")]
        public string StorageAccount { get; set; }

        [Option('k', "key", Required = false, SetName = "Account and Key",
            HelpText = "Storage account key.")]
        public string StorageKey { get; set; }

        [Option('c', "connectionstring", Required = false, SetName = "ConnectionString",
            HelpText = "Storage account connection string.")]
        public string ConnectionString { get; set; }

        [Option('e', "extensions", Required = true,
            HelpText = "Extensions to operate on. [.js, .css, .dat]")]
        public IEnumerable<string> Extensions { get; set; }

        [Option('r', "replace", Required = false, Default = false,
            HelpText = "Replace existing files in-place.")]
        public bool Replace { get; set; }

        [Option('s', "simulate", Required = false, Default = false,
            HelpText = "Do everything except write to blob store.")]
        public bool Simulate { get; set; }

        [Option('n', "newextension", Required = false,
            HelpText = "Copy file with a new postfix. [.gz]")]
        public string NewExtension { get; set; }

        [Option('f', "container", Required = true,
            HelpText = "Container to search in.")]
        public string Container { get; set; }

        [Option('x', "cachecontrol", Required = false,  Default = "public, max-age=2592000",
            HelpText = "Cache-Control header to be sent for the resource from the server.")]
        public string CacheControlHeader { get; set; }

        [Option('w', "wildcardcors", Required = false, Default = false,
            HelpText = "Enable wildcard CORS for this storage account.")]
        public bool wildcard { get; set; }

        [Usage(ApplicationAlias = "ASGE")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
                    new Example("Azure Storage GZip Encoding", new Options { })
                };
            }
        }
    }
}
