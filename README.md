[![Build status](https://ci.appveyor.com/api/projects/status/5b7d5wk4pwv21htt?svg=true)](https://ci.appveyor.com/project/stefangordon/azure-storage-gzip-encoding)

# Azure Storage GZip Encoding
A utility to automatically configure [HTTP Compression](https://en.wikipedia.org/wiki/HTTP_compression) for blobs in Azure Blob storage.  Blobs can be consumed directly from a client browser or via Azure CDN.  

This tool is inspired by a code sample from David Rousset for optimizing BablyonJS Assets.

## Why
Azure storage is an excellent option for storing assets and data consumed by web applications, but it is often preferable to have this data delivered to the browser compressed.  Azure CDN can be used to provide compression and performance improvements on top of blob storage but has an upper limit of 1MB for HTTP compression.

Azure Blob Storage is capable of delivering the correct `content-encoding: gzip` headers directly, but data must be compressed in storage, and the headers must be configured correctly.  Then this content is delivered compressed either directly or through CDN.

Managing this manual compression and configuration of headers yourself would be tedious, but this tool can be run to do it for you, and happily ignores files that are already compressed.  You can run it automatically as part of a build process, or manually on an as-needed basis.

## What it does
The utility can enumerate all of the files in a container.  It then filters to files matching your provided extensions.  These files are compressed using GZip and the content-encoding and cache headers are configured on them so they are compatible with all browsers HTTP compression features.  The tool will not alter a file which is already compressed (based on inspecting the headers), so it is safe to run multiple times to catch new files.

## Getting Started
You must provide
- Either an account name and key, or connection string
- Container to enumerate (recursively)
- List of file extensions to operate on
- Whether to replace existing files with compressed version, or copy with a new extension

## Examples

Replacing .css files in-place.  Blobs will be replaced with compressed version and headers updated:
`asge.exe -e .css -f myContainer -r -a myStorageAccount -k <key>`

Copy .css and .js to a compressed version and append a .gz extension:
`asge.exe -e .css .js -f myContainer -n .gz -a myStorageAccount -k <key>`

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

  -x, --cacheage            (Default: 2592000) Duration for cache control max
                            age header, in seconds.  Default 2592000 (30 days).

  --help                    Display this help screen.
```

## Current State
Only block blobs are supported.

The tool has had only light testing at this point.  Ensure that you have backups of all your binary data before running the tool so that you can recover in the event a blob is corrupted.
