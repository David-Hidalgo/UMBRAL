namespace ScoringService.Infrastructure.Messaging
{
    public sealed class RabbitMqOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string Username { get; set; } = "guest";
        public string Password { get; set; } = "guest";

        /// <summary>Exchange del que se consumen los eventos de sesión (publicados por liveSessionService).</summary>
        public string ExchangeName { get; set; } = "umbral.livesessions";

        /// <summary>Exchange propio al que scoringService publica sus eventos de integración (RF-14).</summary>
        public string ScoringExchangeName { get; set; } = "umbral.scoring";
    }
}
