using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace BlobGZipUtility
{
    class Options
    {
        [Option('a', "account", Required = false, MutuallyExclusiveSet = "Account and Key",
          HelpText = "Storage account host. [sample.blob.core.windows.net]")]
        public string StorageAccount { get; set; }

        [Option('k', "key", Required = false, MutuallyExclusiveSet = "Account and Key",
            HelpText = "Storage account key.")]
        public string StorageKey { get; set; }

        [Option('c', "connectionstring", Required = false, MutuallyExclusiveSet = "ConnectionString",
            HelpText = "Storage account key.")]
        public string ConnectionString { get; set; }

        [OptionArray('e', "extensions", Required = true,
            HelpText = "Extensions to operate on. [.js, .css, .dat]")]
        public string[] Extensions { get; set; }

        [Option('r', "replace", Required = false, 
            HelpText = "Replace existing files in-place.")]
        public bool Replace { get; set; }

        [Option('n', "newextension", Required = false, 
            HelpText = "Copy file with a new postfix. [.gz]")]
        public string NewExtension { get; set; }

        [Option('f', "container", Required = false,
            HelpText = "Container to search in.")]
        public string Container { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("<<app title>>", "<<app version>>"),
                Copyright = new CopyrightInfo("<<app author>>", 2014),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("<<license details here.>>");
            help.AddPreOptionsLine("Usage: app -p Someone");
            help.AddOptions(this);
            return help;
        }

    }
}
