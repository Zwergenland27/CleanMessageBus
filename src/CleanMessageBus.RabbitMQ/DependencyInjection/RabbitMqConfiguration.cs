using RabbitMQ.Client;

namespace CleanMessageBus.RabbitMQ.DependencyInjection;

/// <summary>
/// Configuration for RabbitMQ as message bus
/// </summary>
public class RabbitMqConfiguration
{
    internal string Host { get; private set;  } = "localhost";
    internal string Username { get; private set; } = "guest";
    internal string Password { get; private set; } = "guest";

    internal SslOption SslOptions { get; private set; } = new();

    /// <summary>
    /// Set the hostname of the rabbitmq broker instance to connect to
    /// </summary>
    /// <remarks>
    /// Defaults to "localhost"
    /// </remarks>
    /// <param name="host">hostname</param>
    public RabbitMqConfiguration WithHostname(string host)
    {
        Host = host;
        return this;
    }

    /// <summary>
    /// Set the credentials of the rabbitmq broker instance
    /// </summary>
    /// <remarks>
    /// Defaults to "guest" and "guest"
    /// </remarks>
    /// <param name="username">username of user</param>
    /// <param name="password">password of user</param>
    public RabbitMqConfiguration WithCredentials(string username, string password)
    {
        Username = username;
        Password = password;
        return this;
    }

    /// <summary>
    /// Configures the connection to use ssl to connect to the broker instance
    /// </summary>
    /// <remarks>
    /// Servername will be set to the hostname, so a call to <see cref="WithHostname"/> is required
    /// </remarks>
    /// <returns></returns>
    public RabbitMqConfiguration UseSsl()
    {
        SslOptions = new SslOption
        {
            ServerName = Host,
            Enabled = true
        };
        return this;
    }
}