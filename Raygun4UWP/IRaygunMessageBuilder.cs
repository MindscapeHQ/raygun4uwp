using System;
using System.Collections;
using System.Collections.Generic;

namespace Raygun4UWP
{
  public interface IRaygunMessageBuilder
  {
    RaygunMessage Build();

    IRaygunMessageBuilder SetMachineName(string machineName);

    IRaygunMessageBuilder SetExceptionDetails(Exception exception);

    IRaygunMessageBuilder SetClientDetails();

    IRaygunMessageBuilder SetEnvironmentDetails();

    IRaygunMessageBuilder SetVersion(string version);

    IRaygunMessageBuilder SetUserCustomData(IDictionary userCustomData);

    IRaygunMessageBuilder SetTags(IList<string> tags);

    IRaygunMessageBuilder SetUser(RaygunIdentifierMessage user);

    IRaygunMessageBuilder SetTimeStamp(DateTime? currentTime);

    IRaygunMessageBuilder SetContextId(string contextId);
  }
}