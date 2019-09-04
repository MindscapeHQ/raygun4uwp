Raygun4UWP
==========
[Raygun](https://raygun.com) provider for Universal Windows (UWP) applications

Installation
============

Raygun4UWP is available as a NuGet package. Using your IDE of choice, or the package manager console, install the **Raygun4UWP** NuGet package into your project. More information about the NuGet package can be found [here](https://nuget.org/packages/Raygun4UWP/).

Where is my app API key?
========================
In order to send data to Raygun from your application, you'll need an API key.
When you create a new application in Raygun.com, your app API key is displayed at the top of the instructions page.
You can also find the API key by clicking the "Application Settings" button in the side menu of the Raygun app.

Namespace
=========
All the classes you'll need to use this Raygun provider can be found in the Raygun4UWP namespace.

Getting Started
===============

The most basic setup of Raygun4UWP can be achieved with a single line of code. Place this within the App.xaml.cs constructor.

```csharp
RaygunClient.Initialize("YOUR_APP_API_KEY").EnableCrashReporting().EnableRealUserMonitoring();
```

Here is a break down of what this does:

**Initialize** creates a new RaygunClient and sets it on the static RaygunClient.Current property.
This is so you can access the RaygunClient instance anywhere in your code to adjust settings, or manually send data to Raygun.

**EnableCrashReporting** will cause the RaygunClient to automatically listen to all unhandled exceptions that your application experiences,
and send them off to your Raygun account.

**EnableRealUserMonitoring** will cause the RaygunClient to listen to the app suspending and resuming events to automatically
send session start and end events to Raygun. A session start event will also be sent at this stage.

Which products you enable is optional and will be based on what data you would like to send to Raygun.
You could even choose not to enable either product and just use the RaygunClient to manually send exceptions or RUM events.
Information about manually sending data can be found in the respective product documentation below.

Real User Monitoring
====================

Navigation in a UWP application can be implemented in many different ways and there are no global navigation events to hook in to.
Because of this, Raygun4UWP won't be able to automatically send page-view events to Raygun. Instead, Raygun4UWP provides two
mechanisms for sending navigation events to Raygun - the ListenToNavigation attached peroprty and the ability to manually send events.

ListenToNavigation attached property
------------------------------------

The RaygunClient includes an attached property called ListenToNavigation which currently supports the Frame element. This will attach event handlers
to the navigating and navigated events, allowing Raygun4UWP to measure the time it takes to perform a Frame navigation, and send an event to Raygun.
The name of the event will be the Type name of the page that was navigated to.

To do this in XAML, first add the namespace to the top level tag:

```xml
xmlns:raygun="using:Raygun4UWP"
```

And then set the attached property to true on the Frame tag:

```xml
raygun:RaygunClient.ListenToNavigation="True"
```

Alternatively, here's an example of setting this up in CSharp:

```csharp
RaygunClient.SetListenToNavigation(frame, true);
```

Manually sending RUM events
---------------------------

RaygunClient includes methods for sending the 3 different types of RUM events:

### SendSessionStartEventAsync

Sends a session start event to Raygun. All subsequent events will fall under this session.
If there is currently an active session, then it will first be ended before a new one is started.

### SendSessionTimingEventAsync

A timing event is made up of a type, a name and a duration in milliseconds. The type is an enum value which can either be ViewLoaded or NetworkCall.
The name can be whatever you want - pick something that helps you identify the event when viewing the data in Raygun.
Where possible, time how long the event takes so that you can collect performence metrics.
If it's not possible, or it doesn't make sense for an event to have a duration, then you can leave it as 0.
If this method is called when there isn't currently an active session, then a new session will be started first.

### SendSessionEndEventAsync

Ends any currently open session and sends a session end event to Raygun. Any subsequent event will cause a new session to be started.
If there currently isn't an active session, then calling this method does nothing.

User Tracking
=============

Both Crash Reporting and Real User Monitoring have the ability to specify user information.
If you do not specify any user information, then a default random GUID will be stored in the roaming app data and will be included with all payloads sent to Raygun.
This is enough to get statistics about how many unique users are affected by exceptions, or how many unique users are using your application over time.

There are 2 different ways that you can provide different user information which are described below.
Please be aware of any company privacy policies you have when choosing what type of user information you send to Raygun.

### The User property

If all you need to identify a user is a single string, then you can set the ```User``` property.
Note that setting this to null or whitespace will cause the default random GUID descibed above will be used.
This string can be whatever you like. Below are some common suggestions.

* Identifying information such as name or email address
* An id that doesn't reveal any information about the user, but can be looked up in your own systems to find out who the user it.
* Your own random string if you don't want to use the one Raygun stores in roaming app data. This may however result in unreliable user statistic in Raygun.

### The UserInfo property

If a single string is not enough to describe the information that you want to log about a user, then you can use the UserInfo property.
Below are the various properties that you can use to describe the user. The Identifier is the only required field, which can be provided through the constructor.

```Identifier``` The unique identifier you want to use to identify this user. Suggestions for what you could set this to are lister in the User property section above.

```IsAnonymous``` A flag indicating whether the user is logged in (or identifiable) or if they are anonymous. An anonymous user can still have a unique identifier.

```Email``` The user's email address. If you use email addresses to identify your users, feel free to set the identifier to their email and leave this blank, as we will use the identifier as the email address if it looks like one, and no email address is not specified.

```FullName``` The user's full name.

```FirstName``` The user's first (or preferred) name.

```UUID``` A device identifier. Could be used to identify users across devices, or machines that are breaking for many users.

### RUM behaviour

If you have enabled Real User Monitoring on the RaygunClient, then changing the User or UserInfo properties can cause additional events to be sent to Raygun.
If the user is not null, and then overriden by different user information, then a session-end event will be sent to Raygun.
This is because a session can only have a single user, so changing the user represents a logout/login scenario that ends the current session for a new one to begin.

Building
========
This repository includes a build.bat script for anyone who wants to build and package this provider. The build script uses [psake](https://github.com/psake/psake) which in turn uses [vssetup.powershell](https://github.com/microsoft/vssetup.powershell) to find and run MSBuild. The build script will automatically attempt to install these, so the first time you run the script, you may come across the following prompts. You'll need to respond Y to all of these in order to continue the build.

```
NuGet provider is required to continue
PowerShellGet requires NuGet provider version '2.8.5.201' or newer to interact with NuGet-based repositories. The NuGet
 provider must be available in 'C:\Program Files\PackageManagement\ProviderAssemblies' or
'C:\Users\You\AppData\Local\PackageManagement\ProviderAssemblies'. You can also install the NuGet provider by running
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