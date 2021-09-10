using System;

namespace FacebookToRSS
{
    public class FacebookException : Exception
    {
        public FacebookException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
