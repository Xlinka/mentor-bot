using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Discord
{
    // A health check to ensure the Discord bot API is connected and ready
    public class DiscordHealthCheck : IHealthCheck
    {
        private readonly IDiscordContext _context;

        public DiscordHealthCheck(IDiscordContext context)
        {
            _context = context;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            // Check if the Discord context is connected and ready
            bool isDiscordReady = _context.ConnectedAndReady;

            // Return the appropriate HealthCheckResult based on the connection status
            if (isDiscordReady)
            {
                // Return a healthy status
                return Task.FromResult(HealthCheckResult.Healthy("Discord service is ready and bound."));
            }
            else
            {
                // Return an unhealthy status
                return Task.FromResult(HealthCheckResult.Unhealthy("Discord bot API has disconnected."));
            }
        }
    }
}