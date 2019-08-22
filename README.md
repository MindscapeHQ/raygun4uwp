Raygun4UWP
==========
[Raygun](https://raygun.com) provider for Universal Windows (UWP) applications

Installation
============

Raygun4UWP is available as a NuGet package. Using your IDE of choice, or the package manager console, install the **Raygun4UWP** NuGet package into your project. More information about the NuGet package can be found [here](https://nuget.org/packages/Raygun4UWP/).

Getting Started
===============

The most basic setup of Raygun4UWP can be achieved with a single line of code. Place this within the App.xaml.cs constructor.

```RaygunClient.Initialize("YOUR_APP_API_KEY").EnableCrashReporting();```

This will cause the RaygunClient to automatically listen to all unhandled exceptions that you application experiences,
and send them off to your Raygun account. Additionally, an instance of the RaygunClient will be set on the static
RaygunClient.Current property. This is so you can access the RaygunClient instance anywhere in your code to adjust
settings, or manually send data to Raygun.

Where is my app API key?
========================
In order to send data to Raygun for this application, you'll need an API key.
When you create a new application in Raygun.com, your app API key is displayed at the top of the instructions page.
You can also find the API key by clicking the "Application Settings" button in the side bar of the Raygun dashboard.

Namespace
=========
All the classes you'll need to use this Raygun provider can be found in the Raygun4UWP namespace.

Building
========
This repository includes a build.bat script for anyone who wants to build and package this provider. The build script uses [psake](https://github.com/psake/psake) which in turn uses [vssetup.powershell](https://github.com/microsoft/vssetup.powershell) to find and run MSBuild. The build script will automatically attempt to install these, so the first time you run the script, you may come across the following prompts. You'll need to respond Y to all of these in order to continue the build.

```
NuGet provider is required to continue
PowerShellGet requires NuGet provider version '2.8.5.201' or newer to interact with NuGet-based repositories. The NuGet
 provider must be available in 'C:\Program Files\PackageManagement\ProviderAssemblies' or
'C:\Users\Quant\AppData\Local\PackageManagement\ProviderAssemblies'. You can also install the NuGet provider by running
 'Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force'. Do you want PowerShellGet to install and
import the NuGet provider now?
[Y] Yes  [N] No  [S] Suspend  [?] Help (default is "Y"):
```

```
Untrusted repository
You are installing the modules from an untrusted repository. If you trust this repository, change its
InstallationPolicy value by running the Set-PSRepository cmdlet. Are you sure you want to install the modules from
'PSGallery'?
[Y] Yes  [A] Yes to All  [N] No  [L] No to All  [S] Suspend  [?] Help (default is "N"):
```