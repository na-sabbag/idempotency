using System.Collections.Concurrent;
using System.Threading.Channels;
using webapi.Models;

namespace webapi.Services
{
    /// <summary>
    /// Mock de Mensageria com Consumer
    /// Simula uma fila de mensagens com desduplicação content-based
    /// </summary>
    public interface IMessageQueueService
    {
        Task PublishEventAsync(MessageQueueEvent @event);
        Task<IEnumerable<MessageQueueEvent>> GetPendingEventsAsync();
        Task<IEnumerable<EventProcessingResult>> GetProcessingHistoryAsync();
    }

    public class MessageQueueService : IMessageQueueService
    {
        private readonly Channel<MessageQueueEvent> _queue;
        private readonly ConcurrentBag<EventProcessingResult> _processingHistory = new();
        private readonly ILogger<MessageQueueService> _logger;

        public MessageQueueService(ILogger<MessageQueueService> logger)
        {
            _logger = logger;
            
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<MessageQueueEvent>(options);
        }

        public async Task PublishEventAsync(MessageQueueEvent @event)
        {
            await _queue.Writer.WriteAsync(@event);
            _logger.LogInformation(
                "Evento publicado na fila: {EventId} - {EventType}",
                @event.EventId, @event.EventType);
        }

        public async Task<IEnumerable<MessageQueueEvent>> GetPendingEventsAsync()
        {
            var pendingEvents = new List<MessageQueueEvent>();
            
            await foreach (var @event in _queue.Reader.ReadAllAsync())
            {
                pendingEvents.Add(@event);
                if (pendingEvents.Count >= 10) break;
            }

            return pendingEvents;
        }

        public Task<IEnumerable<EventProcessingResult>> GetProcessingHistoryAsync()
        {
            return Task.FromResult<IEnumerable<EventProcessingResult>>(_processingHistory.ToList());
        }

        public async Task<MessageQueueEvent?> DequeueEventAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (await _queue.Reader.WaitToReadAsync(cancellationToken))
                {
                    if (_queue.Reader.TryRead(out var @event))
                    {
                        return @event;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }

            return null;
        }

        public void RecordProcessingResult(EventProcessingResult result)
        {
            _processingHistory.Add(result);
            
            if (_processingHistory.Count > 100)
            {
                _processingHistory.TryTake(out _);
            }
        }
    }
}

