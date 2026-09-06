using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITelegramBotClient>(_ =>
{
    var token = builder.Configuration["Telegram:Token"]
        ?? throw new InvalidOperationException("Telegram bot token is not configured.");

    return new TelegramBotClient(token);
});

builder.Services.AddHostedService<TelegramBotService>();

var app = builder.Build();

app.MapGet("/", () => "Nexus is running");

app.Run();


public sealed class TelegramBotService(
    ITelegramBotClient bot,
    ILogger<TelegramBotService> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await bot.GetMe(stoppingToken);

        logger.LogInformation(
            "Bot started: @{Username} ({Id})",
            me.Username,
            me.Id);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message]
        };

        bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient bot,
        Update update,
        CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;

        if (message.Text is not { } text)
            return;

        logger.LogInformation(
            "Message from {User}: {Text}",
            message.From?.Username ?? message.From?.Id.ToString(),
            text);

        if (text.StartsWith("/start"))
        {
            await bot.SendMessage(
                message.Chat.Id,
                """
                Привет! Я Nexus 🤖

                Доступные команды:

                /start — запустить бота
                /help — помощь
                /ping — проверить работу
                /id — показать ID чата
                /echo <текст> — повторить текст
                """,
                cancellationToken: cancellationToken);

            return;
        }

        if (text == "/help")
        {
            await bot.SendMessage(
                message.Chat.Id,
                "Доступные команды: /start, /help, /ping, /id, /echo",
                cancellationToken: cancellationToken);

            return;
        }

        if (text == "/ping")
        {
            await bot.SendMessage(
                message.Chat.Id,
                "Pong! 🏓",
                cancellationToken: cancellationToken);

            return;
        }

        if (text == "/id")
        {
            await bot.SendMessage(
                message.Chat.Id,
                $"Chat ID: {message.Chat.Id}",
                cancellationToken: cancellationToken);

            return;
        }

        if (text.StartsWith("/echo "))
        {
            var value = text["/echo ".Length..];

            await bot.SendMessage(
                message.Chat.Id,
                value,
                cancellationToken: cancellationToken);

            return;
        }
    }

    private Task HandleErrorAsync(
        ITelegramBotClient bot,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Telegram bot error");

        return Task.CompletedTask;
    }
}