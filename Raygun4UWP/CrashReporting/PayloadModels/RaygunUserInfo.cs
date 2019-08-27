namespace Raygun4UWP
{
  public class RaygunUserInfo
  {
    public RaygunUserInfo(string user)
    {
      Identifier = user;
    }

    public string Identifier { get; set; }

    public bool IsAnonymous { get; set; }

    public string Email { get; set; }

    public string FullName { get; set; }

    public string FirstName { get; set; }

    public string UUID { get; set; }
  }
}