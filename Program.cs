using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    // память бота
    private static readonly ConcurrentDictionary<long, string> _userSigns = new();

    static async Task Main(string[] args)
    {
        string token = "8352508936:AAHOMDOBJ7iaokzLZMIymj3mW1cG-EVsKjo";
        
        var botClient = new TelegramBotClient(token);
        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        Console.WriteLine("Бот для предсказаний запущен! Нажмите Enter для выхода.");
        Console.ReadLine();
        cts.Cancel();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message) return;
        if (message.Text is not { } messageText) return;

        long chatId = message.Chat.Id;
        string text = messageText.Trim();

        // старт в телеге / сброс
        if (text == "/start")
        {
            _userSigns.TryRemove(chatId, out _); // Сбрасываем старый знак
            await bot.SendMessage(
                chatId, 
                "👋 Привет! Добро пожаловать в астрологический бот.\n\nПожалуйста, **напишите свой знак зодиака** (например: *Овен*, *Телец*, *Лев* и т.д.):", 
                parseMode: ParseMode.Markdown, 
                cancellationToken: cancellationToken
            );
            return;
        }

        // регистрация знака
        if (!_userSigns.ContainsKey(chatId))
        {
            string normalizedSign = NormalizeZodiac(text);

            if (normalizedSign != null)
            {
                // Знак распознан правильно, сохраняем его
                _userSigns[chatId] = normalizedSign;

                var menuKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "✨ Общий гороскоп", "💼 Карьера и учеба" },
                    new KeyboardButton[] { "❤️ Любовь и отношения", "🔄 Сменить знак" }
                }) { ResizeKeyboard = true };

                await bot.SendMessage(
                    chatId, 
                    $"Отлично! Ваш знак зодиака определен: **{normalizedSign}** 🌟\nТеперь выберите категорию предсказания с помощью кнопок ниже:", 
                    parseMode: ParseMode.Markdown, 
                    replyMarkup: menuKeyboard, 
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                // Неправильный ввод
                await bot.SendMessage(
                    chatId, 
                    "⚠️ Я не смог распознать такой знак зодиака.\nПожалуйста, проверьте правильность написания и введите ваш знак на русском языке (Овен, Телец, Близнецы, Рак, Лев, Дева, Весы, Скорпион, Стрелец, Козерог, Водолей, Рыбы):", 
                    cancellationToken: cancellationToken
                );
            }
            return;
        }

        // Если знак уже задан — обрабатываем нажатия на кнопки предсказаний
        string userSign = _userSigns[chatId];
        string replyText;

        switch (text)
        {
            case "🔄 Сменить знак":
                _userSigns.TryRemove(chatId, out _);
                // Убираем клавиатуру меню
                var removeKeyboard = new ReplyKeyboardRemove();
                await bot.SendMessage(chatId, "Хорошо, давайте начнем сначала. Введите ваш знак зодиака на русском языке:", replyMarkup: removeKeyboard, cancellationToken: cancellationToken);
                return;

            case "✨ Общий гороскоп":
                replyText = $"🔮 **Общий гороскоп для знака {userSign}:**\nСегодня звезды советуют вам прислушаться к интуиции. Удачный день для решения накопившихся вопросов.";
                break;

            case "💼 Карьера и учеба":
                replyText = $"💼 **Гороскоп карьеры/учебы для знака {userSign}:**\nВаши труды окупятся! Ожидайте похвалы от руководства или успешной сдачи учебных заданий.";
                break;

            case "❤️ Любовь и отношения":
                replyText = $"❤️ **Любовный гороскоп для знака {userSign}:**\nПрекрасный вечер для романтики и общения с близкими. Проявите инициативу, и вы будете приятной удивлены.";
                break;

            default:
                replyText = $"Вы выбрали неизвестную команду. Пожалуйста, используйте кнопки меню для предсказаний для знака **{userSign}**.";
                break;
        }

        await bot.SendMessage(chatId, replyText, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
    }

    // Метод проверки и нормализации введенного знака
    private static string NormalizeZodiac(string input)
    {
        // Приводим к нижнему регистру 
        string lower = input.ToLower();

        if (lower.Contains("овен")) return "♈ Овен";
        if (lower.Contains("телец")) return "♉ Телец";
        if (lower.Contains("близнец")) return "♊ Близнецы";
        if (lower.Contains("рак")) return "♋ Рак";
        if (lower.Contains("лев")) return "♌ Лев";
        if (lower.Contains("дев")) return "♍ Дева";
        if (lower.Contains("вес")) return "♎ Весы";
        if (lower.Contains("скорпион")) return "♏ Скорпион";
        if (lower.Contains("стрелец")) return "♐ Стрелец";
        if (lower.Contains("козерог")) return "♑ Козерог";
        if (lower.Contains("водолей")) return "♒ Водолей";
        if (lower.Contains("рыб")) return "♓ Рыбы";

        return null; // Если не найдено ни одно совпадение
    }

    private static Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}