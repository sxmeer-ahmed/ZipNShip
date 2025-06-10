using System;

namespace ZipNShip.Core.Utils
{
    public class ShortGuidFactory
    {
        public string NewGuid()
        {
            var guid = Guid.NewGuid();
            var bytes = guid.ToByteArray();
            var base64 = Convert.ToBase64String(bytes)
                .Replace("+", "_")
                .Replace("/", "-")
                .TrimEnd('=');
            return base64;
        }
    }
}
