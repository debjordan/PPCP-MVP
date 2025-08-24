using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using PPCP.Api.Data;
using PPCP.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace PPCP.Api.Services
{
    public class MqttService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MqttService> _logger;
        private IManagedMqttClient? _mqttClient;
        private readonly string _server;
        private readonly int _port;

        public MqttService(IServiceProvider serviceProvider, ILogger<MqttService> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _server = configuration["Mqtt:Server"] ?? "localhost";
            _port = int.Parse(configuration["Mqtt:Port"] ?? "1883");
        }

        public async Task StartAsync()
        {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateManagedMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_server, _port)
                .WithClientId("ppcp-api")
                .WithCredentials(null) // até implementar as roles permanece sem validação.
                .Build();

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(mqttClientOptions)
                .Build();

            _mqttClient.ConnectedAsync += async e =>
            {
                _logger.LogInformation("Conectado ao broker MQTT");

                var topicFilters = new[]
                {
                    new MqttTopicFilterBuilder()
                        .WithTopic("factory/+/machine/+/telemetry")
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build(),
                    new MqttTopicFilterBuilder()
                        .WithTopic("factory/+/machine/+/event")
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build()
                };

                await _mqttClient.SubscribeAsync(topicFilters);
                _logger.LogInformation("Subscrito aos tópicos MQTT");
            };

            _mqttClient.DisconnectedAsync += async e =>
            {
                _logger.LogWarning("Desconectado do broker MQTT. Tentando reconectar...");
            };

            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                try
                {
                    var topic = e.ApplicationMessage.Topic;
                    var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                    _logger.LogDebug("Mensagem recebida: {Topic} - {Payload}", topic, payload);

                    if (topic.Contains("/telemetry"))
                    {
                        await ProcessarTelemetria(topic, payload);
                    }
                    else if (topic.Contains("/event"))
                    {
                        await ProcessarEvento(topic, payload);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem MQTT");
                }
            };

            try
            {
                await _mqttClient.StartAsync(managedOptions);
                _logger.LogInformation("Cliente MQTT iniciado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar o cliente MQTT");
            }
        }

        public async Task StopAsync()
        {
            if (_mqttClient != null)
            {
                await _mqttClient.StopAsync();
                _logger.LogInformation("Cliente MQTT parado");
            }
        }

        private async Task ProcessarTelemetria(string topic, string payload)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PPCPContext>();
            var ppcpService = scope.ServiceProvider.GetRequiredService<PPCPService>();

            try
            {
                // Extrair machine ID do tópico: factory/{site}/machine/{maquinaId}/telemetry
                var parts = topic.Split('/');
                if (parts.Length >= 4 && int.TryParse(parts[3], out int maquinaId))
                {
                    var data = JsonConvert.DeserializeObject<TelemetriaPayload>(payload);
                    if (data != null)
                    {
                        var telemetria = new Telemetria
                        {
                            MaquinaId = maquinaId,
                            OrdemProducaoId = data.OpId,
                            Timestamp = data.Timestamp,
                            Estado = data.State,
                            TotalCount = data.TotalCount,
                            GoodCount = data.GoodCount,
                            ScrapCount = data.ScrapCount,
                            Speed_uph = data.Speed_uph
                        };

                        context.Telemetrias.Add(telemetria);

                        // Atualizar OP se existir
                        if (data.OpId.HasValue)
                        {
                            var op = await context.OrdensProducao.FindAsync(data.OpId.Value);
                            if (op != null)
                            {
                                op.QuantidadeTotal = data.TotalCount;
                                op.QuantidadeBoa = data.GoodCount;

                                // Verificar se deve concluir a OP
                                if (op.QuantidadeBoa >= op.QuantidadePlanejada)
                                {
                                    await ppcpService.ConcluirOP(op.Id);
                                }
                                else
                                {
                                    await ppcpService.AtualizarETA(op.Id);
                                }
                            }
                        }

                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar telemetria: {Payload}", payload);
            }
        }

        private async Task ProcessarEvento(string topic, string payload)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PPCPContext>();

            try
            {
                var parts = topic.Split('/');
                if (parts.Length >= 4 && int.TryParse(parts[3], out int maquinaId))
                {
                    var data = JsonConvert.DeserializeObject<EventoPayload>(payload);
                    if (data != null)
                    {
                        var evento = new Evento
                        {
                            MaquinaId = maquinaId,
                            OrdemProducaoId = data.OpId,
                            Tipo = data.Type,
                            Motivo = data.Reason,
                            TsInicio = data.TsStart,
                            TsFim = data.TsEnd,
                            Atributos = data.Attributes != null ? JsonConvert.SerializeObject(data.Attributes) : null
                        };

                        context.Eventos.Add(evento);
                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento: {Payload}", payload);
            }
        }
    }

    // DTOs para payloads MQTT
    public class TelemetriaPayload
    {
        public DateTime Timestamp { get; set; }
        public EstadoMaquina State { get; set; }
        public int TotalCount { get; set; }
        public int GoodCount { get; set; }
        public int ScrapCount { get; set; }
        public double Speed_uph { get; set; }
        public int? OpId { get; set; }
    }

    public class EventoPayload
    {
        public DateTime TsStart { get; set; }
        public DateTime? TsEnd { get; set; }
        public TipoEvento Type { get; set; }
        public string? Reason { get; set; }
        public int? OpId { get; set; }
        public Dictionary<string, object>? Attributes { get; set; }
    }
}
