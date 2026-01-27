using Microsoft.AspNetCore.Mvc;

namespace Invexaaa.Helpers
{
    public static class ItemStatusHelper
    {
        public static bool IsInactive(string? status)
        {
            return status != "Active";
        }
    }
}

