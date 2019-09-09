using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Raygun4UWP.Tests
{
  [TestClass]
  public class RUMServiceTests
  {
    private RUMService _rumService;

    private RaygunUserInfo _customUserInfo;

    [TestInitialize]
    public void SetUp()
    {
      RaygunSettings settings = new RaygunSettings("");

      _rumService = new RUMService(settings);
      _rumService.Enable();

      _customUserInfo = new RaygunUserInfo("Ronald_Raygun");
    }

    [TestCleanup]
    public void TearDown()
    {
      _rumService.Disable();
    }

    #region SendSessionStartEventAsync tests

    [TestMethod]
    public void SendSessionStartEventAsync_FirstSession_MessageHasSessionId()
    {
      RaygunRUMMessage sessionStartMessage = _rumService.SendSessionStartEventAsync().Result;
      
      Assert.AreEqual(1, sessionStartMessage.EventData.Length);
      RaygunRUMEventInfo eventInfo = sessionStartMessage.EventData[0];
      Assert.IsNotNull(eventInfo.SessionId);
    }

    [TestMethod]
    public void SendSessionStartEventAsync_SecondSession_MessageHasDifferentSessionId()
    {
      RaygunRUMMessage firstSessionStartMessage = _rumService.SendSessionStartEventAsync().Result;
      
      RaygunRUMEventInfo eventInfo = firstSessionStartMessage.EventData[0];
      string sessionId = eventInfo.SessionId;

      RaygunRUMMessage secondSessionStartMessage = _rumService.SendSessionStartEventAsync().Result;

      Assert.AreEqual(1, secondSessionStartMessage.EventData.Length);
      eventInfo = secondSessionStartMessage.EventData[0];
      Assert.IsNotNull(eventInfo.SessionId);
      Assert.AreNotEqual(sessionId, eventInfo.SessionId);
    }

    [TestMethod]
    public void SendSessionStartEventAsync_FirstSession_MessageHasCorrectState()
    {
      RaygunRUMMessage sessionStartMessage = _rumService.SendSessionStartEventAsync().Result;

      Assert.AreEqual(1, sessionStartMessage.EventData.Length);
      RaygunRUMEventInfo eventInfo = sessionStartMessage.EventData[0];
      Assert.AreEqual(RaygunRUMEventType.SessionStart, eventInfo.Type);
      Assert.AreEqual(DefaultUserService.DefaultUser, eventInfo.User);
      Assert.AreEqual(EnvironmentService.PackageVersion, eventInfo.Version);
      Assert.IsNull(eventInfo.Data);
    }

    [TestMethod]
    public void SendSessionStartEventAsync_CustomApplicationVersion_MessageHasSpecifiedApplicationVersion()
    {
      string applicationVersion = "custom-version";
      _rumService.ApplicationVersion = applicationVersion;

      RaygunRUMMessage sessionStartMessage = _rumService.SendSessionStartEventAsync().Result;

      Assert.AreEqual(1, sessionStartMessage.EventData.Length);
      RaygunRUMEventInfo eventInfo = sessionStartMessage.EventData[0];
      Assert.AreEqual(applicationVersion, eventInfo.Version);
    }

    [TestMethod]
    public void SendSessionStartEventAsync_CustomUserInfo_MessageHasSpecifiedUserInfo()
    {
      _rumService.UserInfo = _customUserInfo;

      RaygunRUMMessage sessionStartMessage = _rumService.SendSessionStartEventAsync().Result;

      Assert.AreEqual(1, sessionStartMessage.EventData.Length);
      RaygunRUMEventInfo eventInfo = sessionStartMessage.EventData[0];
      Assert.AreEqual(_customUserInfo, eventInfo.User);
    }

    #endregion // SendSessionStartEventAsync tests

    #region SendSessionEndEventAsync tests

    [TestMethod]
    public void SendSessionEndEventAsync_NoActiveSession_ReturnsNull()
    {
      // First, ending the session that is started via the Enable method call in the test setup
      _rumService.SendSessionEndEventAsync().Wait();

      RaygunRUMMessage sessionEndMessage = _rumService.SendSessionEndEventAsync().Result;

      Assert.IsNull(sessionEndMessage);
    }

    [TestMethod]
    public void SendSessionEndEventAsync_ActiveSession_MessageHasSameSessionId()
    {
      RaygunRUMMessage sessionStartMessage = _rumService.SendSessionStartEventAsync().Result;

      RaygunRUMEventInfo eventInfo = sessionStartMessage.EventData[0];
      string sessionId = eventInfo.SessionId;

      RaygunRUMMessage sessionEndMessage = _rumService.SendSessionEndEventAsync().Result;
      Assert.AreEqual(1, sessionEndMessage.EventData.Length);
      eventInfo = sessionEndMessage.EventData[0];
      Assert.IsNotNull(eventInfo.SessionId);
      Assert.AreEqual(sessionId, eventInfo.SessionId);
    }

    [TestMethod]
    public void SendSessionEndEventAsync_ActiveSession_MessageHasCorrectState()
    {
      _rumService.SendSessionStartEventAsync().Wait();
      
      RaygunRUMMessage sessionEndMessage = _rumService.SendSessionEndEventAsync().Result;

      Assert.AreEqual(1, sessionEndMessage.EventData.Length);
      RaygunRUMEventInfo eventInfo = sessionEndMessage.EventData[0];
      Assert.AreEqual(RaygunRUMEventType.SessionEnd, eventInfo.Type);
      Assert.AreEqual(DefaultUserService.DefaultUser, eventInfo.User);
      Assert.AreEqual(EnvironmentService.PackageVersion, eventInfo.Version);
      Assert.IsNull(eventInfo.Data);
    }

    [TestMethod]
    public void SendSessionEndEventAsync_CustomApplicationVersion_MessageHasSpecifiedApplicationVersion()
    {
      string applicationVersion = "custom-version";
      _rumService.ApplicationVersion = applicationVersion;
      _rumService.SendSessionStartEventAsync().Wait();
      
      RaygunRUMMessage sessionEndMessage = _rumService.SendSessionEndEventAsync().Result;

      Assert.AreEqual(1, sessionEndMessage.EventData.Length);
      RaygunRUMEventInfo eventInfo = sessionEndMessage.EventData[0];
      Assert.AreEqual(applicationVersion, eventInfo.Version);
    }

    [TestMethod]
    public void SendSessionEndEventAsync_CustomUserInfo_MessageHasSpecifiedUserInfo()
    {
      _rumService.UserInfo = _customUserInfo;
      _rumService.SendSessionStartEventAsync().Wait();

      RaygunRUMMessage sessionEndMessage = _rumService.SendSessionEndEventAsync().Result;

      Assert.AreEqual(1, sessionEndMessage.EventData.Length);
      RaygunRUMEventInfo eventInfo = sessionEndMessage.EventData[0];
      Assert.AreEqual(_customUserInfo, eventInfo.User);
    }

    #endregion // SendSessionEndEventAsync tests

    #region SendSessionTimingEventAsync tests

    [TestMethod]
    public void SendSessionTimingEventAsync_NoActiveSession_MessageHasNewSessionId()
    {
      RaygunRUMMessage sessionTimingMessage = _rumService.SendSessionTimingEventAsync(RaygunRUMEventTimingType.ViewLoaded, "page", 1000).Result;

      Assert.AreEqual(1, sessionTimingMessage.EventData.Length);
      RaygunRUMEventInfo eventInfo = sessionTimingMessage.EventData[0];
      Assert.IsNotNull(eventInfo.SessionId);
    }

    [TestMethod]
    public void SendSessionTimingEventAsync_ActiveSession_MessageHasSameSessionId()
    {
      RaygunRUMMessage sessionStartMessage = _rumService.SendSessionStartEventAsync().Result;

      RaygunRUMEventInfo eventInfo = sessionStartMessage.EventData[0];
      string sessionId = eventInfo.SessionId;

      RaygunRUMMessage sessionTimingMessage = _rumService.SendSessionTimingEventAsync(RaygunRUMEventTimingType.ViewLoaded, "page", 1000).Result;

      Assert.AreEqual(1, sessionTimingMessage.EventData.Length);
      eventInfo = sessionTimingMessage.EventData[0];
      Assert.IsNotNull(eventInfo.SessionId);
      Assert.AreEqual(sessionId, eventInfo.SessionId);
    }

    [TestMethod]
    public void SendSessionTimingEventAsync_NoActiveSession_MessageHasCorrectState()
    {
      RaygunRUMMessage sessionTimingMessage = _rumService.SendSessionTimingEventAsync(RaygunRUMEventTimingType.ViewLoaded, "page", 1000).Result;

      Assert.AreEqual(1, sessionTimingMessage.EventData.Length);
      RaygunRUMEventInfo eventInfo = sessionTimingMessage.EventData[0];
      Assert.AreEqual(RaygunRUMEventType.Timing, eventInfo.Type);
      Assert.AreEqual(DefaultUserService.DefaultUser, eventInfo.User);
      Assert.AreEqual(EnvironmentService.PackageVersion, eventInfo.Version);
      Assert.IsNotNull(eventInfo.Data);
    }

    [TestMethod]
    public void SendSessionTimingEventAsync_NoActiveSession_MessageDataHasGivenTypeAndNameAndDuration()
    {
      RaygunRUMEventTimingType type = RaygunRUMEventTimingType.ViewLoaded;
      string name = "page";
      long duration = 1000;

      RaygunRUMMessage sessionTimingMessage = _rumService.SendSessionTimingEventAsync(type, name, duration).Result;

      Assert.AreEqual(1, sessionTimingMessage.EventData.Length);
      RaygunRUMEventInfo eventInfo = sessionTimingMessage.EventData[0];

      RaygunRUMTimingData[] timingData = JsonConvert.DeserializeObject<RaygunRUMTimingData[]>(eventInfo.Data, HttpService.SERIALIZATION_SETTINGS);
      Assert.AreEqual(1, timingData.Length);
      Assert.AreEqual(type, timingData[0].Timing.Type);
      Assert.AreEqual(name, timingData[0].Name);
      Assert.AreEqual(duration, timingData[0].Timing.Duration);
    }

    [TestMethod]
    public void SendSessionTimingEventAsync_CustomApplicationVersion_MessageHasSpecifiedApplicationVersion()
    {
      string applicationVersion = "custom-version";
      _rumService.ApplicationVersion = applicationVersion;

      RaygunRUMMessage sessionTimingMessage = _rumService.SendSessionTimingEventAsync(RaygunRUMEventTimingType.ViewLoaded, "page", 1000).Result;

      Assert.AreEqual(1, sessionTimingMessage.EventData.Length);
      RaygunRUMEventInfo eventInfo = sessionTimingMessage.EventData[0];
      Assert.AreEqual(applicationVersion, eventInfo.Version);
    }

    [TestMethod]
    public void SendSessionTimingEventAsync_CustomUserInfo_MessageHasSpecifiedUserInfo()
    {
      _rumService.UserInfo = _customUserInfo;

      RaygunRUMMessage sessionTimingMessage = _rumService.SendSessionTimingEventAsync(RaygunRUMEventTimingType.ViewLoaded, "page", 1000).Result;

      Assert.AreEqual(1, sessionTimingMessage.EventData.Length);
      RaygunRUMEventInfo eventInfo = sessionTimingMessage.EventData[0];
      Assert.AreEqual(_customUserInfo, eventInfo.User);
    }

    [TestMethod]
    public void SendSessionTimingEventAsync_UserInfoChangedFromNullToCustomUserInfo_MessageHasSameSessionId()
    {
      RaygunRUMMessage sessionStartMessage = _rumService.SendSessionStartEventAsync().Result;

      RaygunRUMEventInfo eventInfo = sessionStartMessage.EventData[0];
      string sessionId = eventInfo.SessionId;

      _rumService.UserInfo = _customUserInfo;

      RaygunRUMMessage sessionTimingMessage = _rumService.SendSessionTimingEventAsync(RaygunRUMEventTimingType.ViewLoaded, "page", 1000).Result;

      Assert.AreEqual(1, sessionTimingMessage.EventData.Length);
      eventInfo = sessionTimingMessage.EventData[0];
      Assert.AreEqual(sessionId, eventInfo.SessionId);
    }

    [TestMethod]
    public void SendSessionTimingEventAsync_UserInfoChangedFromCustomUserInfoToNull_MessageHasDifferentSessionId()
    {
      _rumService.UserInfo = _customUserInfo;

      RaygunRUMMessage sessionStartMessage = _rumService.SendSessionStartEventAsync().Result;

      RaygunRUMEventInfo eventInfo = sessionStartMessage.EventData[0];
      string sessionId = eventInfo.SessionId;

      _rumService.UserInfo = null;

      RaygunRUMMessage sessionTimingMessage = _rumService.SendSessionTimingEventAsync(RaygunRUMEventTimingType.ViewLoaded, "page", 1000).Result;

      Assert.AreEqual(1, sessionTimingMessage.EventData.Length);
      eventInfo = sessionTimingMessage.EventData[0];
      Assert.AreNotEqual(sessionId, eventInfo.SessionId);
    }

    [TestMethod]
    public void SendSessionTimingEventAsync_UserInfoChangedFromCustomUserInfoToDifferentCustomUserInfo_MessageHasDifferentSessionId()
    {
      _rumService.UserInfo = _customUserInfo;

      RaygunRUMMessage sessionStartMessage = _rumService.SendSessionStartEventAsync().Result;

      RaygunRUMEventInfo eventInfo = sessionStartMessage.EventData[0];
      string sessionId = eventInfo.SessionId;

      _rumService.UserInfo = new RaygunUserInfo("Different_User");

      RaygunRUMMessage sessionTimingMessage = _rumService.SendSessionTimingEventAsync(RaygunRUMEventTimingType.ViewLoaded, "page", 1000).Result;

      Assert.AreEqual(1, sessionTimingMessage.EventData.Length);
      eventInfo = sessionTimingMessage.EventData[0];
      Assert.AreNotEqual(sessionId, eventInfo.SessionId);
    }

    #endregion // SendSessionTimingEventAsync tests
  }
}
