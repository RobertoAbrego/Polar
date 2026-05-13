using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Polar.Services
{
    public class MisionDiariaHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MisionDiariaHostedService> _logger;

        public MisionDiariaHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<MisionDiariaHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var generator = scope.ServiceProvider.GetRequiredService<MisionGenerationService>();
                    if (!await generator.HasAnyMissionAsync(stoppingToken))
                    {
                        await generator.GenerateDailyMissionsAsync(stoppingToken);
                    }
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delay = GetDelayUntilNextMidnight();
                    await Task.Delay(delay, stoppingToken);

                    if (stoppingToken.IsCancellationRequested)
                        break;

                    using var scope = _scopeFactory.CreateScope();
                    var generator = scope.ServiceProvider.GetRequiredService<MisionGenerationService>();

                    await generator.GenerateDailyMissionsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Detención normal del host.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en la generación automática de misiones.");
            }
        }

        private static TimeSpan GetDelayUntilNextMidnight()
        {
            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays(1);
            return nextMidnight - now;
        }
    }
}