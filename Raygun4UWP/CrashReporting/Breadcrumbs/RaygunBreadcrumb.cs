using System;
using System.Collections.Generic;

namespace Raygun4UWP
{
    /// <summary>
    /// Contains Breadcrumb data.
    /// 
    /// Simplified implementation based on the Raygun4Net NetCore and Raygun4Net4 libraries.
    /// </summary>
    public class RaygunBreadcrumb
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public string Message { get; set; }

        public string Category { get; set; }

        public IDictionary<string, object> CustomData { get => _customData ?? (_customData = new Dictionary<string, object>()); set => _customData = value; }
        private IDictionary<string, object> _customData;

        public long Timestamp { get; set; } = (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
    }
}