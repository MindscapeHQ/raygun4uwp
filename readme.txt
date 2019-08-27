Raygun4UWP - Raygun provider for Universal Windows (UWP) applications
=====================================================================

Getting Started
===============

The most basic setup of Raygun4UWP can be achieved with a single line of code.
Place this within the App.xaml.cs constructor.

RaygunClient.Initialize("YOUR_APP_API_KEY").EnableCrashReporting();

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