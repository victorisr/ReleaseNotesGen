# .NET {RUNTIME-VERSION} - {HEADER-DATE}

The .NET {RUNTIME-VERSION} and .NET SDK {LATEST-SDK} releases are available for download. The latest {ID-VERSION} release is always listed at [.NET {ID-VERSION} Releases](../README.md).

{ID-VERSION} SDKs that include {RUNTIME-VERSION} runtimes:
SECTION-ADDEDSDK

## Downloads

|           | SDK Installer                        | SDK Binaries                 | Runtime Installer                                        | Runtime Binaries                                 | ASP.NET Core Runtime           |Windows Desktop Runtime          |
| --------- | :------------------------------------------:     | :----------------------:                 | :---------------------------:                            | :-------------------------:                      | :-----------------:            | :-----------------:            |
| Windows   | [x86][dotnet-sdk-win-x86.exe] \| [x64][dotnet-sdk-win-x64.exe] \| [Arm64][dotnet-sdk-win-arm64.exe] | [x86][dotnet-sdk-win-x86.zip] \| [x64][dotnet-sdk-win-x64.zip] \|  [Arm64][dotnet-sdk-win-arm64.zip] | [x86][dotnet-runtime-win-x86.exe] \| [x64][dotnet-runtime-win-x64.exe] \| [Arm64][dotnet-runtime-win-arm64.exe] | [x86][dotnet-runtime-win-x86.zip] \| [x64][dotnet-runtime-win-x64.zip] \| [Arm64][dotnet-runtime-win-arm64.zip] | [x86][aspnetcore-runtime-win-x86.exe] \| [x64][aspnetcore-runtime-win-x64.exe] \| [Hosting Bundle][dotnet-hosting-win.exe] | [x86][windowsdesktop-runtime-win-x86.exe] \| [x64][windowsdesktop-runtime-win-x64.exe] \| [Arm64][windowsdesktop-runtime-win-arm64.exe] |
| macOS     | [x64][dotnet-sdk-osx-x64.pkg] \| [ARM64][dotnet-sdk-osx-arm64.pkg] | [x64][dotnet-sdk-osx-x64.tar.gz] \| [ARM64][dotnet-sdk-osx-arm64.tar.gz]  | [x64][dotnet-runtime-osx-x64.pkg] \| [ARM64][dotnet-runtime-osx-arm64.pkg] | [x64][dotnet-runtime-osx-x64.tar.gz] \| [ARM64][dotnet-runtime-osx-arm64.tar.gz]| [x64][aspnetcore-runtime-osx-x64.tar.gz] \| [ARM64][aspnetcore-runtime-osx-arm64.tar.gz] | - |
| Linux     |  [Snap and Package Manager](../install-linux.md)  | [x64][dotnet-sdk-linux-x64.tar.gz] \| [Arm][dotnet-sdk-linux-arm.tar.gz]  \| [Arm64][dotnet-sdk-linux-arm64.tar.gz] \| [Arm32 Alpine][dotnet-sdk-linux-musl-arm.tar.gz]  \| [x64 Alpine][dotnet-sdk-linux-musl-x64.tar.gz] | [Packages (x64)][linux-packages] | [x64][dotnet-runtime-linux-x64.tar.gz] \| [Arm][dotnet-runtime-linux-arm.tar.gz] \| [Arm64][dotnet-runtime-linux-arm64.tar.gz] \| [Arm32 Alpine][dotnet-runtime-linux-musl-arm.tar.gz] \| [Arm64 Alpine][dotnet-runtime-linux-musl-arm64.tar.gz] \| [x64 Alpine][dotnet-runtime-linux-musl-x64.tar.gz]  | [x64][aspnetcore-runtime-linux-x64.tar.gz]  \| [Arm][aspnetcore-runtime-linux-arm.tar.gz] \| [Arm64][aspnetcore-runtime-linux-arm64.tar.gz] \| [x64 Alpine][aspnetcore-runtime-linux-musl-x64.tar.gz] | - |
|  | [Checksums][checksums-sdk]                             | [Checksums][checksums-sdk]                                      | [Checksums][checksums-runtime]                             | [Checksums][checksums-runtime]  | [Checksums][checksums-runtime]  | [Checksums][checksums-runtime] |

1. Includes the .NET Runtime and ASP.NET Core Runtime
2. For hosting stand-alone apps on Windows Servers. Includes the ASP.NET Core Module for IIS and can be installed separately on servers without installing .NET Runtime.

The .NET SDK includes a matching updated .NET Runtime. Downloading the Runtime or ASP.NET Core packages is not needed when installing the SDK.

You can check your .NET SDK version by running the following command. The example version shown is for this release.

```console
$ dotnet --version
{LATEST-SDK}
```

## Docker Images

The [.NET Docker images](https://hub.docker.com/_/microsoft-dotnet) have been updated for this release. The [.NET Docker samples](https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md) show various ways to use .NET and Docker together. You can use the following command to try running the latest .NET {ID-VERSION} release in containers:

```console
docker run --rm mcr.microsoft.com/dotnet/samples
```

The following repos have been updated.

* [dotnet/sdk](https://github.com/dotnet/dotnet-docker/blob/main/README.sdk.md): .NET SDK
* [dotnet/aspnet](https://github.com/dotnet/dotnet-docker/blob/main/README.aspnet.md): ASP.NET Core Runtime
* [dotnet/runtime](https://github.com/dotnet/dotnet-docker/blob/main/README.runtime.md): .NET Runtime
* [dotnet/runtime-deps](https://github.com/dotnet/dotnet-docker/blob/main/README.runtime.md): .NET Runtime Dependencies
* [dotnet/monitor](https://github.com/dotnet/dotnet-docker/blob/main/README.monitor.md): .NET Monitor
* [dotnet/monitor/base](https://github.com/dotnet/dotnet-docker/blob/main/README.monitor-base.md): .NET Monitor Base
* [dotnet/aspire-dashboard](https://github.com/dotnet/dotnet-docker/blob/main/README.aspire-dashboard.md): .NET Aspire Dashboard
* [dotnet/samples](https://github.com/dotnet/dotnet-docker/blob/main/README.samples.md): .NET Samples

## Notable Changes

 [.NET {ID-VERSION} {BLOG-DATE} Blog][dotnet-blog]

 SECTION-MSRC
MSRC DETAILS TO BE INSERTED HERE

## Visual Studio Compatibility

You need [Visual Studio {VS-VERSION}](https://visualstudio.microsoft.com) or later to use .NET {ID-VERSION} on Windows. While not officially supported, we’ve also enabled rudimentary support for .NET {ID-VERSION} in Visual Studio for Mac. Users have to enable a preview feature in Preferences to enable the IDE to discover and use the .NET {ID-VERSION} SDK for creating, loading, building, and debugging projects. The [C# extension](https://code.visualstudio.com/docs/languages/dotnet) for [Visual Studio Code](https://code.visualstudio.com/) supports .NET {ID-VERSION} and C# {CSHARPSDK-VERSION}.

## Feedback

Your feedback is important and appreciated. We've created an issue at [dotnet/core #xxxx](https://github.com/dotnet/core/issues/xxxx) for your questions and comments.
SECTION-SDKS

[checksums-runtime]: https://builds.dotnet.microsoft.com/dotnet/checksums/{RUNTIME-VERSION}-sha.txt
[checksums-sdk]: https://builds.dotnet.microsoft.com/dotnet/checksums/{RUNTIME-VERSION}-sha.txt

[dotnet-blog]: https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-{BLOGPOST-DATE}-servicing-updates/

[linux-packages]: ../install-linux.md

## Packages updated in this release

SECTION-PACKAGES
PACKAGES TO BE INSERTED HERE

SECTION-RUNTIME

SECTION-WINDOWSDESKTOP

SECTION-ASP

SECTION-LATESTSDK
