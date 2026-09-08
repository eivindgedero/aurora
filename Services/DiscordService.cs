using Discord;
using Discord.Net;
using Discord.Rest;

namespace aurora.Services;

public class DiscordService
{
    private readonly DiscordRestClient _client;
    private readonly ILogger<DiscordService> _logger;

    private readonly string _token;
    private readonly ulong _userId;

    public DiscordService(
        IConfiguration configuration,
        ILogger<DiscordService> logger
    )
    {
        _logger = logger;
        _client = new DiscordRestClient();

        _token = configuration["Discord:Token"]
            ?? throw new InvalidOperationException(
                "Discord token is missing"
            );

        var userId = configuration["Discord:UserId"]
            ?? throw new InvalidOperationException(
                "Discord user ID is missing"
            );

        if (!ulong.TryParse(userId, out _userId))
        {
            throw new InvalidOperationException(
                "Discord user ID is invalid"
            );
        }
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Logging into Discord");

        await _client.LoginAsync(
            TokenType.Bot,
            _token
        );

        _logger.LogInformation("Logged into Discord");
    }

    public async Task SendNotificationAsync(string message)
    {
        try
        {
            var user = await _client.GetUserAsync(_userId);

            if (user is null)
            {
                _logger.LogWarning(
                    "Discord user {UserId} was not found",
                    _userId
                );

                return;
            }

            var dmChannel = await user.CreateDMChannelAsync();

            await dmChannel.SendMessageAsync(
                message,
                flags: MessageFlags.SuppressEmbeds
            );

            _logger.LogInformation(
                "Sent Discord notification to {Username}",
                user.Username
            );
        }
        catch (HttpException exception)
        {
            _logger.LogError(
                exception,
                "Failed to send Discord notification to user {UserId}",
                _userId
            );
        }
    }
}