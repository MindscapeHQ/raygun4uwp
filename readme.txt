Raygun4UWP - Raygun provider for Universal Windows (UWP) applications

========================================================================================================================================================
GETTING STARTED
========================================================================================================================================================

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

______________________________________________________________________________________________

RaygunClient.Initialize("YOUR_APP_API_KEY").EnableCrashReporting().EnableRealUserMonitoring();
______________________________________________________________________________________________

Here is a break down of what this does:

"Initialize" creates a new RaygunClient and sets it on the static RaygunClient.Current property.
This is so you can access the RaygunClient instance anywhere in your code to adjust settings, or manually send data to Raygun.

"EnableCrashReporting" will cause the RaygunClient to automatically listen to all unhandled exceptions that your application experiences,
and send them off to your Raygun account.

"EnableRealUserMonitoring" will cause the RaygunClient to listen to the app suspending and resuming events to automatically
send session start and end events to Raygun. A session start event will also be sent at this stage.
Raygun4UWP can not automatically detect navigation events in your application, so take a look at the Real User Monitoring documentation below to see the options of setting this up.

Which products you enable is optional and will be based on what data you would like to send to Raygun.
You could even choose not to enable either product and just use the RaygunClient to manually send exceptions or RUM events.
Information about manually sending data can be found in the respective product documentation below.

========================================================================================================================================================
CRASH REPORTING
========================================================================================================================================================

Manually sending exceptions
---------------------------

The RaygunClient has methods to manually send exception information to Raygun, which is particularly useful for sending exceptions that get caught in try/catch blocks.
Below is a simple code example of manually sending an exception.
Note that the asynchronous 'fire and forget' method is used here so that your application can immediately continue.

__________________________________

try
{
  
}
catch (Exception e)
{
  new RaygunClient().SendAsync(e);
}
__________________________________

-- Custom tags --

The send exception method has an optional argument to send a list of string tags. These tags are useful to categorize exceptions in different ways which you can filter in Raygun.

-- Custom data --

Another optional argument of the send exception method is a dictionary of string keys and object values.
This lets you attch custom data that you know will help investigating the exception further, such as the state of related models.

The SendingCrashReport event
----------------------------

Every time an exception message is about to be serialized and sent to Raygun, the "RaygunClient.SendingCrashReport" event is invoked.
This will be called regardless of if the exception is being reported manually, or automatically by the RaygunClient.
The event arguments contain both the Raygun exception message and the original Exception object.
Attaching a handler to this event can be used in a few different ways:

-- Modifying the message --

Any changes you make to "e.CrashReport" will be included when the report is serialized and sent to Raygun.
Since this event handler is called for both manually and automatically sent exceptions, it's a good place to put common report logic.
For example, you could use this to attach tags or global application data.

________________________________________________________________________________________________

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
________________________________________________________________________________________________

-- Custom exception grouping --

Another common use for the SendingCrashReport event handler is to control the way that Raygun groups your exceptions.
If you set the "e.CrashReport.Details.GroupingKey" property, then Raygun will use that as a grouping key when processing that report.
Any reports that have the same GroupingKey value will be grouped together.
You can include logic to only provide a GroupingKey for specific reports.
Any report that doesn't have a GroupingKey will simply be grouped by the Raygun processing pipeline in the usual way.

________________________________________________________________________________________________

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
________________________________________________________________________________________________

-- Cancelling a message --

Setting "e.Cancel" to true within the SendingCrashReport event handler will tell the RaygunClient not to send the report to Raygun.
You could check values on the RaygunCrashReport or/and the Exception object to filter out messages that you don't want.
For example, you could cancel certain types of exceptions or reports from old devices / operating systems.

________________________________________________________________________________________________

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
________________________________________________________________________________________________

Strip wrapper exceptions
------------------------

Sometimes an exception will wrap one or more inner exceptions.
When these are sent to Raygun, all inner exceptions are included in a single exception report and will be considered during the grouping logic.
In these cases, you may find outer exceptions that you're not interested in which wrap valuable inner exceptions.
Below is an example of how you can specify which exceptions you're not interested in.
When these are reported, they'll be stripped away and the inner exceptions will be sent as individual messages to Raygun.
Note that TargetInvocationException will be stripped by default and setting the StrippedWrapperExceptions list will override the default list.

________________________________________________________________________

RaygunClient.Current.Settings.StrippedWrapperExceptions = new List<Type>
{
  typeof(TargetInvocationException),
  typeof(AggregateException)
};
________________________________________________________________________

Application version
-------------------

By default, each exception report will include the version of your application package.
If you need to provide your own custom version value, you can do so by setting the ApplicationVersion property of the RaygunClient (in the format x.x.x.x where x is a positive integer).

____________________________________________________

RaygunClient.Current.ApplicationVersion = "2.5.1.0";
____________________________________________________


========================================================================================================================================================
REAL USER MONITORING
========================================================================================================================================================

Navigation in a UWP application can be implemented in many different ways and there are no global navigation events to hook in to.
Because of this, Raygun4UWP won't be able to automatically send page-view events to Raygun. Instead, Raygun4UWP provides two
mechanisms for sending navigation events to Raygun - the ListenToNavigation attached property and the ability to manually send events.

The ListenToNavigation attached property
----------------------------------------

The RaygunClient includes an attached property called "ListenToNavigation" which currently supports the Frame element. This will attach event handlers
to the navigating and navigated events, allowing Raygun4UWP to measure the time it takes to perform a Frame navigation and send an event to Raygun.
The name of the event will be the Type name of the page that was navigated to.

To do this in XAML, first add the Raygun4UWP namespace to the top level tag of a page that contains a Frame that you want to track:

__________________________________________

<MainPage xmlns:raygun="using:Raygun4UWP">
...
</MainPage>
__________________________________________


And then set the attached property to true on the Frame element:

_______________________________________________________

<Frame raygun:RaygunClient.ListenToNavigation="True" />
_______________________________________________________


Alternatively, here's an example of setting this up in C#:

________________________________________________

RaygunClient.SetListenToNavigation(frame, true);
________________________________________________

Manually sending RUM events
---------------------------

RaygunClient includes methods for sending the three different types of RUM events:

-- SendSessionStartEventAsync --

Sends a session start event to Raygun. All subsequent events will fall under this session.
If there is currently an active session, then it will first be ended before a new one is started.

-- SendSessionTimingEventAsync --

A timing event is made up of a type, a name and a duration in milliseconds. The type is an enum value which can either be "ViewLoaded" or "NetworkCall".
The name can be whatever you want - pick something that helps you identify the event when viewing the data in Raygun.
Where possible, time how long the event takes so that you can collect performence metrics.
If it's not possible, or it doesn't make sense for an event to have a duration, then you can leave it as zero.
If this method is called when there isn't currently an active session, then a new session will be started first.

-- SendSessionEndEventAsync --

Ends any currently open session and sends a session end event to Raygun. Any subsequent event will cause a new session to be started.
If there currently isn't an active session, then calling this method does nothing.

========================================================================================================================================================
USER TRACKING
========================================================================================================================================================

Both Crash Reporting and Real User Monitoring have the ability to specify user information.
If you do not specify any user information, then a default random GUID will be stored in the roaming app data and will be included with all payloads sent to Raygun.
This is enough to get statistics about how many unique users are affected by exceptions, or how many unique users are using your application over time.

There are two different ways that you can provide different user information which are described below.
Please be aware of any company privacy policies you have when choosing what type of user information you send to Raygun.

The User property
-----------------

If all you need to identify a user is a single string, then you can set the "User" property.
Note that setting this to null or whitespace will cause the default random GUID descibed above will be used.
This string can be whatever you like. Below are some common suggestions.

* Identifying information such as name or email address
* An id that doesn't reveal any information about the user, but can be looked up in your own systems to find out who the user it.
* Your own random string if you don't want to use the one Raygun stores in roaming app data. This may however result in unreliable user statistics in Raygun.

The UserInfo property
---------------------

If a single string is not enough to describe the information that you want to log about a user, then you can set the "UserInfo" property.
Below are the various properties that you can use to describe the user. The Identifier is the only required field, which can be provided through the constructor.

"Identifier" The unique identifier you want to use to identify this user. Suggestions for what you could set this to are listed in the User property section above.

"IsAnonymous" A flag indicating whether the user is logged in (or identifiable) or if they are anonymous. An anonymous user still requires an identifier.

"UUID" A device identifier. Could be used to identify users across devices, or machines that are breaking for many users.

"Email", "FullName" and "FirstName" are self explanatory.

RUM behaviour
-------------

If you have enabled Real User Monitoring on the RaygunClient, then changing the User or UserInfo properties can cause additional events to be sent to Raygun.
If the user is not currently null and then overriden by different user information, then a session-end event will be sent to Raygun.
This is because a session can only have a single user, so changing the user represents a logout/login scenario that ends the current session for a new one to begin.
