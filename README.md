Raygun4UWP
==========
[Raygun](https://raygun.com) provider for Universal Windows (UWP) applications

* [Installation](#installation)
* [Getting Started](#getting-started)
  * [Where is my app API key?](#where-is-my-app-api-key)
  * [Namespace](#namespace)
  * [Initialization](#initialization)
* [Crash Reporting](#crash-reporting)
  * [Upload your PDB files](#upload-your-pdb-files)
  * [Manually sending exceptions](#manually-sending-exceptions)
    * [Custom tags](#custom-tags)
    * [Custom data](#custom-data)
  * [The SendingCrashReport event](#the-sendingcrashreport-event)
    * [Modifying the message](#modifying-the-message)
    * [Custom exception grouping](#custom-exception-grouping)
    * [Cancelling a message](#cancelling-a-message)
  * [Strip wrapper exceptions](#strip-wrapper-exceptions)
* [Real User Monitoring](#real-user-monitoring)
  * [The ListenToNavigation attached property](#the-listentonavigation-attached-property)
  * [Manually sending RUM events](#manually-sending-rum-events)
    * [SendSessionStartEventAsync](#sendsessionstarteventasync)
    * [SendSessionTimingEventAsync](#sendsessiontimingeventasync)
    * [SendSessionEndEventAsync](#sendsessionendeventasync)
* [Common Features](#common-features)
  * [Customers](#customers)
    * [The User property](#the-user-property)
    * [The UserInfo property](#the-userinfo-property)
    * [RUM behaviour](#rum-behaviour)
  * [Application version](#application-version)
* [Building](#building)

Installation
============

Raygun4UWP is available as a NuGet package. Using your IDE of choice, or the package manager console, install the **Raygun4UWP** NuGet package into your project.
More information about the NuGet package can be found [here](https://nuget.org/packages/Raygun4UWP/).

Getting Started
===============

Where is my app API key?
------------------------
In order to send data to Raygun from your application, you'll need an API key.
When you create a new application in Raygun.com, your app API key is displayed on the instructions page.
You can also find the API key by clicking the "Application Settings" button in the side menu of the Raygun app.

Namespace
---------
All the classes you'll need to use this Raygun provider can be found in the "Raygun4UWP" namespace.

Initialization
--------------

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
Raygun4UWP can not automatically detect navigation events in your application, so take a look at the [Real User Monitoring documentation](#real-user-monitoring) below to see the options of setting this up.

Which products you enable is optional and will be based on what data you would like to send to Raygun.
You could even choose not to enable either product and just use the RaygunClient to manually send exceptions or RUM events.
Information about manually sending data can be found in the respective product documentation below.

Crash Reporting
===============

Upload your PDB files
---------------------

Crash reports that are generated from the release build of you application will not include line numbers or file names in the stack traces. Some stack trace frames may not even have class or method names.
This is because this information is not available within your application while it's running, but can be looked up from associated PDB files.

PDB files can be uploaded to Raygun, which will be used to process your crash reports and resolve missing information.
Raygun provides two ways to upload your files - an API end point which is useful for automation and manual upload from within the Raygun web app.
Read more about this in the [Raygun documentation](https://raygun.com/documentation/language-guides/dotnet/crash-reporting/uwp/#upload-your-pdb-files).

Manually sending exceptions
---------------------------

The RaygunClient has methods to manually send exception information to Raygun, which is particularly useful for sending exceptions that get caught in try/catch blocks.
Below is a simple code example of manually sending an exception.
Note that the asynchronous 'fire and forget' method is used here so that your application can immediately continue.

```csharp
try
{
  
}
catch (Exception ex)
{
  RaygunClient.Current.SendAsync(ex);
}
```

### Custom tags

The send exception method has an optional argument to send a list of string tags. These tags are useful to categorize exceptions in different ways which you can filter in Raygun.

```csharp
RaygunClient.Current.SendAsync(ex, new List<string>{"Critical", "Data"});
```

### Custom data

Another optional argument of the send exception method is a dictionary of string keys and object values.
This lets you attach custom data that you know will help investigating the exception further, such as the state of related models.

```csharp
RaygunClient.Current.SendAsync(ex, userCustomData: new Dictionary<string, object>
{
  {"ExperimentalFeaturesEnabled", true}
});
```

The SendingCrashReport event
----------------------------

Every time an exception message is about to be serialized and sent to Raygun, the `RaygunClient.SendingCrashReport` event is invoked.
This will be called regardless of if the exception is being reported manually, or automatically by the RaygunClient.
The event arguments contain both the Raygun exception message and the original Exception object.
Attaching a handler to this event can be used in a few different ways:

### Modifying the message

Any changes you make to `e.CrashReport` will be included when the report is serialized and sent to Raygun.
Since this event handler is called for both manually and automatically sent exceptions, it's a good place to put common report logic.
For example, you could use this to attach tags or global application data.

```csharp
public App()
{
  RaygunClient.Initialize("YOUR_APP_API_KEY").EnableCrashReporting().EnableRealUserMonitoring();
  RaygunClient.Current.SendingCrashReport += RaygunClient_SendingCrashReport;

  this.InitializeComponent();
  this.Suspending += OnSuspending;
}

private void RaygunClient_SendingCrashReport(object sender, RaygunSendingCrashReportEventArgs e)
{
  if (e.OriginalException.Message.Contains("Unknown error"))
  {
    // Tags
    IList<string> tags = e.CrashReport.Details.Tags ?? new List<string>();

    tags.Add("Unknown");
    tags.Add("Low priority");
    tags.Add("Not important");

    e.CrashReport.Details.Tags = tags;
  }
  
  // Custom data
  IDictionary customData = e.CrashReport.Details.UserCustomData ?? new Dictionary<string, object>();

  customData["currentState"] = MyApplicationModel.State;

  e.CrashReport.Details.UserCustomData = customData;
}
```

### Custom exception grouping

Another common use for the SendingCrashReport event handler is to control the way that Raygun groups your exceptions.
If you set the `e.CrashReport.Details.GroupingKey` property, then Raygun will use that as a grouping key when processing that report.
Any reports that have the same GroupingKey value will be grouped together.
You can include logic to only provide a GroupingKey for specific reports.
Any report that doesn't have a GroupingKey will simply be grouped by the Raygun processing pipeline in the usual way.

```csharp
public App()
{
  RaygunClient.Initialize("YOUR_APP_API_KEY").EnableCrashReporting().EnableRealUserMonitoring();
  RaygunClient.Current.SendingCrashReport += RaygunClient_SendingCrashReport;

  this.InitializeComponent();
  this.Suspending += OnSuspending;
}

private void RaygunClient_SendingCrashReport(object sender, RaygunSendingCrashReportEventArgs e)
{
  if (e.OriginalException.Message.Contains("Unknown error"))
  {
    e.CrashReport.Details.GroupingKey = "UnknownErrorsThatWeJustGroupTogether";
  }
}
```

### Cancelling a message

Setting `e.Cancel` to true within the SendingCrashReport event handler will tell the RaygunClient not to send the report to Raygun.
You could check values on the RaygunCrashReport or/and the Exception object to filter out messages that you don't want.
For example, you could cancel certain types of exceptions or reports from old devices / operating systems.

```csharp
public App()
{
  RaygunClient.Initialize("YOUR_APP_API_KEY").EnableCrashReporting().EnableRealUserMonitoring();
  RaygunClient.Current.SendingCrashReport += RaygunClient_SendingCrashReport;

  this.InitializeComponent();
  this.Suspending += OnSuspending;
}

private void RaygunClient_SendingCrashReport(object sender, RaygunSendingCrashReportEventArgs e)
{
  if (e.OriginalException.Message.Contains("Unknown error"))
  {
    e.Cancel = true;
  }
}
```

Strip wrapper exceptions
------------------------

Sometimes an exception will wrap one or more inner exceptions.
When these are sent to Raygun, all inner exceptions are included in a single exception report and will be considered during the grouping logic.
In these cases, you may find outer exceptions that you're not interested in which wrap valuable inner exceptions.
Below is an example of how you can specify which exceptions you're not interested in.
When these are reported, they'll be stripped away and the inner exceptions will be sent as individual messages to Raygun.
Note that TargetInvocationException will be stripped by default and setting the StrippedWrapperExceptions list will override the default list.

```csharp
RaygunClient.Current.Settings.StrippedWrapperExceptions = new List<Type>
{
  typeof(TargetInvocationException),
  typeof(AggregateException)
};
```

Real User Monitoring
====================

Navigation in a UWP application can be implemented in many different ways and there are no global navigation events to hook in to.
Because of this, Raygun4UWP won't be able to automatically send page-view events to Raygun. Instead, Raygun4UWP provides two
mechanisms for sending navigation events to Raygun - the ListenToNavigation attached property and the ability to manually send events.

The ListenToNavigation attached property
----------------------------------------

The RaygunClient includes an attached property called `ListenToNavigation` which currently supports the `Frame` element. This will attach event handlers
to the loading/loaded and navigating/navigated events, allowing Raygun4UWP to measure the time it takes to perform a Frame navigation and send an event to Raygun.
The name of the event will be the name of the page class type that was navigated to.

To do this in XAML, first add the Raygun4UWP namespace to the top level tag of a page that contains a Frame that you want to track:

```xml
<MainPage xmlns:raygun="using:Raygun4UWP">
...
</MainPage>
```

And then set the attached property to true on the Frame element:

```xml
<Frame raygun:RaygunClient.ListenToNavigation="True" />
```

Alternatively, here's an example of setting this up in C#:

```csharp
RaygunClient.SetListenToNavigation(frame, true);
```

Manually sending RUM events
---------------------------

RaygunClient includes methods for sending the three different types of RUM events:

### SendSessionStartEventAsync

Sends a session start event to Raygun. All subsequent events will fall under this session.
If there is currently an active session, then it will first be ended before a new one is started.

### SendSessionTimingEventAsync

A timing event is made up of a type, a name and a duration in milliseconds. The type is an enum value which can either be `ViewLoaded` or `NetworkCall`.
The name can be whatever you want - pick something that helps you identify the event when viewing the data in Raygun.
Where possible, time how long the event takes so that you can collect performence metrics.
If it's not possible, or it doesn't make sense for an event to have a duration, then you can leave it as zero.
If this method is called when there isn't currently an active session, then a new session will be started first.

### SendSessionEndEventAsync

Ends any currently open session and sends a session end event to Raygun. Any subsequent event will cause a new session to be started.
If there currently isn't an active session, then calling this method does nothing.

Common Features
===============

Customers
-------------

Both Crash Reporting and Real User Monitoring have the ability to specify customer information.
If you do not specify any customer information, then a default random GUID will be stored in the roaming app data and will be included with all payloads sent to Raygun.
This is enough to get statistics about how many customers are affected by exceptions, or how many customers are using your application over time.

There are two different ways that you can provide different customer information which are described below.
Please be aware of any company privacy policies you have when choosing what type of customer information you send to Raygun.

### The User property

If all you need to identify a customer is a single string, then you can set the `User` property.
Note that setting this to null or whitespace will cause the default random GUID descibed above to be used.
This string can be whatever you like. Below are some common suggestions.

* Identifying information such as name or email address.
* An id that doesn't reveal any information about the customer, but can be looked up in your own systems to find out who the customer is.
* Your own random string if you don't want to use the one Raygun stores in roaming app data. This may however result in unreliable customer statistics in Raygun.

### The UserInfo property

If a single string is not enough to describe the information that you want to log about a customer, then you can set the `UserInfo` property.
Below are the various properties that you can use to describe the customer. The Identifier is the only required field, which can be provided through the constructor.

**Identifier** The unique identifier you want to use to identify this customer. Suggestions for what you could set this to are listed in the User property section above.

**IsAnonymous** A flag indicating whether the customer is logged in (or identifiable) or if they are anonymous. An anonymous customer still requires an identifier.

**UUID** A device identifier. Could be used to identify customers across devices, or machines that are breaking for many customers.

**Email**, **FullName** and **FirstName** are self explanatory.

### RUM behaviour

If you have enabled Real User Monitoring on the RaygunClient, then changing the User or UserInfo properties can cause additional events to be sent to Raygun.
If the customer is not currently null and then overriden by different customer information, then a session-end event will be sent to Raygun.
This is because a session can only have a single customer, so changing the customer represents a logout/login scenario that ends the current session for a new one to begin.

Application version
-------------------

By default, each exception report and RUM event will include the version of your application package.
If you need to provide your own custom version value, you can do so by setting the ApplicationVersion property of the RaygunClient (in the format x.x.x.x where x is a positive integer).

```csharp
RaygunClient.Current.ApplicationVersion = "2.5.1.0";
```

Building
========
This repository includes a build.bat script for anyone who wants to build and package this provider.
The build script uses [psake](https://github.com/psake/psake) which in turn uses [vssetup.powershell](https://github.com/microsoft/vssetup.powershell) to find and run MSBuild.
The build script will automatically attempt to install these, so the first time you run the script, you may come across the following prompts.
You'll need to respond Y to all of these in order to continue the build.

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