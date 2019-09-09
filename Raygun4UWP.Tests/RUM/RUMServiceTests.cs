using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Raygun4UWP.Tests
{
  [TestClass]
  public class RUMServiceTests
  {
    private RUMService _rumService;

    [TestInitialize]
    public void SetUp()
    {
      RaygunSettings settings = new RaygunSettings("");

      _rumService = new RUMService(settings);
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

    #endregion // SendSessionStartEventAsync tests
  }
}
