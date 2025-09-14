[![Build status](https://ci.appveyor.com/api/projects/status/5b7d5wk4pwv21htt?svg=true)](https://ci.appveyor.com/project/stefangordon/azure-storage-gzip-encoding)

# Azure Storage GZip Encoding
A cross-platform utility to automatically configure [HTTP Compression](https://en.wikipedia.org/wiki/HTTP_compression) for blobs in Azure Blob storage. Blobs can be consumed directly from a client browser or via Azure CDN.

This tool is inspired by a code sample from David Rousset for optimizing BablyonJS Assets.

## Cross-Platform Support
This tool is built on .NET 8 and runs natively on:
- **Linux** (x64) - Perfect for Jenkins pipelines and CI/CD systems
- **Windows** (x64) 
- **macOS** (x64)

Pre-compiled binaries are available for all platforms, or you can run it with the .NET 8 runtime.

## Why
Azure storage is an excellent option for storing assets and data consumed by web applications, but it is often preferable to have this data delivered to the browser compressed.  Azure CDN can be used to provide compression and performance improvements on top of blob storage but has an upper limit of 1MB for HTTP compression.

Azure Blob Storage is capable of delivering the correct `content-encoding: gzip` headers directly, but data must be compressed in storage, and the headers must be configured correctly.  Then this content is delivered compressed either directly or through CDN.

Managing this manual compression and configuration of headers yourself would be tedious, but this tool can be run to do it for you, and happily ignores files that are already compressed.  You can run it automatically as part of a build process, or manually on an as-needed basis.

## What it does
The utility can enumerate all of the files in a container.  It then filters to files matching your provided extensions.  These files are compressed using GZip and the content-encoding and cache headers are configured on them so they are compatible with all browsers HTTP compression features.  The tool will not alter a file which is already compressed (based on inspecting the headers), so it is safe to run multiple times to catch new files.

The utility can also automatically configure your storage account with wildcard CORS settings which are often desirable if serving certain types of assets through Azure CDN.

## Getting Started

### Prerequisites
- .NET 8 runtime (if using the cross-platform binaries)
- OR use the self-contained executables that include the runtime

### Installation Options

#### Option 1: Self-Contained Executables (Recommended)
Download the appropriate executable for your platform:
- Linux: `asge` (no extension)
- Windows: `asge.exe` 
- macOS: `asge` (no extension)

No additional runtime installation required.

#### Option 2: .NET Runtime Required
If you have .NET 8 installed:
```bash
dotnet run --project ASGE.csproj -- [arguments]
```

#### Option 3: Build from Source
```bash
# Clone the repository
git clone https://github.com/stefangordon/azure-storage-gzip-encoding
cd azure-storage-gzip-encoding

# Build for your platform
dotnet build

# Or publish self-contained for specific platform
dotnet publish -c Release --self-contained -r linux-x64 -o ./linux
dotnet publish -c Release --self-contained -r win-x64 -o ./windows  
dotnet publish -c Release --self-contained -r osx-x64 -o ./macos
```

### Usage
You must provide
- Either an account name and key, or connection string
- Container to enumerate (recursively)
- List of file extensions to operate on
- Whether to replace existing files with compressed version, or copy with a new extension

## Examples

### Linux/macOS
Replacing .css files in-place. Blobs will be replaced with compressed version and headers updated:
```bash
./asge -e .css -f myContainer -r -a myStorageAccount -k <key>
```

Copy .css and .js to a compressed version and append a .gz extension:
```bash
./asge -e .css .js -f myContainer -n .gz -a myStorageAccount -k <key>
```

Replacing .js files in-place and enabling CORS for the account:
```bash
./asge -w -e .js -f myContainer -r -a myStorageAccount -k <key>
```

### Windows
Replacing .js files in-place using a connection string:
```cmd
asge.exe -e .js -f myContainer -r -c "<connection string>"
```

### Jenkins Pipeline Example
```groovy
pipeline {
    agent any
    stages {
        stage('Compress Assets') {
            steps {
                sh './asge -e .css .js -f assets -r -c "${AZURE_STORAGE_CONNECTION_STRING}"'
            }
        }
    }
}
```

```
  -a, --account             Storage account host. [mystorage]

  -k, --key                 Storage account key.

  -c, --connectionstring    Storage account key.

  -e, --extensions          Required. Extensions to operate on. [.js, .css,
                            .dat]

  -r, --replace             (Default: False) Replace existing files in-place.

  -s, --simulate            (Default: False) Do everything except write to blob
                            store.

  -n, --newextension        Copy file with a new postfix. [.gz]

  -f, --container           Required. Container to search in.

  -w, --wildcardcors        Enable wildcard CORS for this storage account.

  -x, --cacheage            (Default: 2592000) Duration for cache control max
                            age header, in seconds.  Default 2592000 (30 days).

  --help                    Display this help screen.
```

## Current State
Only block blobs are supported.  Please ensure you have a backup of your data before running this tool against your container.
