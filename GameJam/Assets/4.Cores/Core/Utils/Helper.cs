using System;

namespace Utils
{
    public static class Helper
    {
        public static string GenerateRandomId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
