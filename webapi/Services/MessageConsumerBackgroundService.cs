using webapi.Models;

namespace webapi.Services
{
    /// <summary>
    /// Background Service que simula um Consumer de Mensageria
    /// Processa eventos com desduplicação Content-Based
    /// </summary>
    public class MessageConsumerBackgroundService : BackgroundService
    {
        private readonly ILogger<MessageConsumerBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public MessageConsumerBackgroundService(
            ILogger<MessageConsumerBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Message Consumer iniciado às {Time}", DateTimeOffset.Now);

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessMessagesAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagens");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("Message Consumer finalizado às {Time}", DateTimeOffset.Now);
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            
            var queueService = scope.ServiceProvider.GetRequiredService<MessageQueueService>();
            var deduplicationService = scope.ServiceProvider
                .GetRequiredService<IContentBasedDeduplicationService>();

            var @event = await queueService.DequeueEventAsync(cancellationToken);

            if (@event == null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                return;
            }

            _logger.LogInformation(
                "Processando evento: {EventId} - {EventType}",
                @event.EventId, @event.EventType);

            var contentForHash = new
            {
                EventType = @event.EventType,
                Payload = @event.Payload
            };

            var contentHash = deduplicationService.ComputeContentHash(contentForHash);

            var request = new ContentBasedRequest
            {
                EventType = @event.EventType,
                UserId = contentHash,
                Data = @event.Payload
            };

            var (isDuplicate, existingOperation) = await deduplicationService
                .ProcessOperationAsync(request);

            var result = new EventProcessingResult
            {
                EventId = @event.EventId,
                ProcessedAt = DateTime.UtcNow
            };

            if (isDuplicate)
            {
                result.Success = false;
                result.WasDuplicate = true;
                result.Message = $"Evento duplicado! Já processado em {existingOperation!.ProcessedAt}";
                
                _logger.LogWarning(
                    "Evento duplicado detectado e ignorado: {EventId}",
                    @event.EventId);
            }
            else
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

                result.Success = true;
                result.WasDuplicate = false;
                result.Message = "Evento processado com sucesso";

                _logger.LogInformation(
                    "Evento processado com sucesso: {EventId}",
                    @event.EventId);
            }

            queueService.RecordProcessingResult(result);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Message Consumer está parando...");
            await base.StopAsync(cancellationToken);
        }
    }
}

