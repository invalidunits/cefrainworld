using System;

namespace CEFRainworld
{
    public static class CEFRainworldExceptions
    {
        public static InvalidOperationException NotInitializedException(string what = "")
        {
            if (string.IsNullOrWhiteSpace(what)) return new InvalidOperationException("CEFRainworld is not initialized.");
            return new InvalidOperationException($"{what} is not initialized.");
        }

    }
}
