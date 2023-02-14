using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Net;

namespace MentorBot
{
    // A custom attribute that implements rate limiting.
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ThrottleAttribute : ActionFilterAttribute
    {
        // The name of the rate limit.
        public string Name { get; set; } = string.Empty;

        // The message to be returned if the rate limit has been exceeded.
        public string Message { get; set; } = "You may only perform this action every {0} seconds.";


        // The number of seconds between each allowed request.
        private readonly int _seconds;

        // A cache to store the rate limit.
        private static MemoryCache Cache { get; } = new MemoryCache(new MemoryCacheOptions());

        // Initializes a new instance of the <see cref="ThrottleAttribute"/> class.
        public ThrottleAttribute(int seconds)
        {
            _seconds = seconds;
        }


        // This method is called before an action is executed.

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Create the cache keys for this request
            var key = string.Concat(Name, "-", context.HttpContext.Connection.RemoteIpAddress);
            var key2 = string.Concat(Name, "-", context.HttpContext.Connection.RemoteIpAddress, "-2");

            // Check if the first key exists in the cache
            if (!Cache.TryGetValue(key, out bool _))
            {
                // If the first key does not exist, add it to the cache with an expiration time of _seconds
                var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(_seconds));
                Cache.Set(key, true, cacheEntryOptions);
            }
            else if (!Cache.TryGetValue(key2, out bool _))
            {
                // If the first key exists, check if the second key exists in the cache
                // If the second key does not exist, add it to the cache with an expiration time of _seconds
                var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(_seconds));
                Cache.Set(key2, true, cacheEntryOptions);
            }
            else
            {
                // If both keys exist, return a conflict response with a custom message
                context.Result = new ObjectResult(string.Format(Message, _seconds))
                {
                    StatusCode = (int)HttpStatusCode.Conflict
                };
            }
        }
    }
}