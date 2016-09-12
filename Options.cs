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
        [Option('a', "account", Required = false, MutuallyExclusiveSet = "Account and Key",
          HelpText = "Storage account host. [mystorage]")]
        public string StorageAccount { get; set; }

        [Option('k', "key", Required = false, MutuallyExclusiveSet = "Account and Key",
            HelpText = "Storage account key.")]
        public string StorageKey { get; set; }

        [Option('c', "connectionstring", Required = false, MutuallyExclusiveSet = "ConnectionString",
            HelpText = "Storage account connection string.")]
        public string ConnectionString { get; set; }

        [OptionArray('e', "extensions", Required = true,
            HelpText = "Extensions to operate on. [.js, .css, .dat]")]
        public string[] Extensions { get; set; }

        [Option('r', "replace", Required = false, DefaultValue = false,
            HelpText = "Replace existing files in-place.")]
        public bool Replace { get; set; }

        [Option('s', "simulate", Required = false, DefaultValue = false,
            HelpText = "Do everything except write to blob store.")]
        public bool Simulate { get; set; }

        [Option('n', "newextension", Required = false,
            HelpText = "Copy file with a new postfix. [.gz]")]
        public string NewExtension { get; set; }

        [Option('f', "container", Required = true,
            HelpText = "Container to search in.")]
        public string Container { get; set; }

        [Option('x', "cacheage", Required = false,  DefaultValue = 2592000,
            HelpText = "Duration for cache control max age header, in seconds.  Default 2592000 (30 days).")]
        public int MaxAgeSeconds { get; set; }

        [Option('w', "wildcardcors", Required = false, DefaultValue = false,
            HelpText = "Enable wildcard CORS for this storage account.")]
        public bool wildcard { get; set; }


        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("Azure Storage GZip Encoder", "1.0"),
                Copyright = new CopyrightInfo("Stefan Gordon", 2016),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("https://github.com/stefangordon/azure-storage-gzip-encoding\n");
            help.AddPreOptionsLine("\nExample 1: asge --extensions .css .js --container assets --replace --connectionstring <conn string>");
            help.AddPreOptionsLine("Example 2: asge --extensions .css .js --container assets --newextension .gz --account mystorage --key <storage key>");
            help.AddPreOptionsLine("\nUse either connection string (-c) or account and key (-a and -k).");
            help.AddOptions(this);
            return help;
        }

    }
}
