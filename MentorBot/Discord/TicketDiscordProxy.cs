using MentorBot.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Discord
{
    // This class is responsible for managing the TicketDiscordProxyHost service, which watches for updates
    // to tickets and sends notifications to the relevant Discord channel.
    public class TicketDiscordProxyHost : IHostedService
    {
        private readonly ITicketNotifier _notifier;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TicketDiscordProxyHost> _logger;
        private readonly CancellationTokenSource _cancelSource = new();

        private IDisposable? _watchToken;

        public TicketDiscordProxyHost(IServiceProvider serviceProvider, ITicketNotifier notifier, ILogger<TicketDiscordProxyHost> logger)
        {
            _serviceProvider = serviceProvider;
            _notifier = notifier;
            _logger = logger;
        }

        // Start the service, and begin watching for ticket updates.
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Interlocked.Exchange(ref _watchToken, _notifier.WatchTicketsUpdated(TicketUpdated))?.Dispose();
            return Task.CompletedTask;
        }

        // Handle a ticket update, and send a notification to Discord.
        public async void TicketUpdated(Ticket ticket)
        {
            try
            {
                await UpdateTicketInternal(ticket, _cancelSource.Token);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Error while updating an internal ticket.");
            }
            // This warning is not always true.
#pragma warning disable CS1058 // A previous catch clause already catches all exceptions
            catch
#pragma warning restore CS1058 // A previous catch clause already catches all exceptions
            {
                _logger.LogWarning("Abstract error while updating internal ticket, this is bad.");
            }
        }

        // Update the internal representation of the given ticket, and send a notification to Discord.
        private async ValueTask UpdateTicketInternal(Ticket ticket, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ITicketDiscordProxy>().RectifyTicket(ticket, cancellationToken);
        }

        // Stop the service, and cancel any ongoing operations.
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Interlocked.Exchange(ref _watchToken, null)?.Dispose();
            _cancelSource.Cancel();
            return Task.CompletedTask;
        }
    }

    // Interface for the TicketDiscordProxy service, which is responsible for managing the
    // connection between tickets and Discord messages.
    public interface ITicketDiscordProxy
    {
        ValueTask RectifyTicket(Ticket ticket, CancellationToken cancellationToken = default);
    }

    // Implementation of the TicketDiscordProxy service, which updates a Discord message with
    // information from the given ticket, or creates a new message if one does not already exist.
    public class TicketDiscordProxy : ITicketDiscordProxy
    {
        private readonly IDiscordContext _discCtx;
        private readonly ITicketContext _tickCtx;

        public TicketDiscordProxy(IDiscordContext discCtx, ITicketContext tickCtx)
        {
            _discCtx = discCtx;
            _tickCtx = tickCtx;
        }

        // Update the Discord message associated with the given ticket.
        public async ValueTask RectifyTicket(Ticket ticket, CancellationToken cancellationToken = default)
        {
            if (ticket.DiscordId != null)
            {
                // Update the existing message.
                await _discCtx.UpdateMessageAsync(ticket.DiscordId.Value, ticket.ToEmbed(), cancellationToken);
            }
            else
            {
                var message = await _discCtx.SendMessageAsync(ticket.ToEmbed(), cancellationToken);
                if (message == null)
                {
                    return;
                }
                ticket.DiscordId = message.Id;
                await _tickCtx.AssignDiscordIdAsync(ticket.Id, message.Id, cancellationToken);
            }
        }
    }
}