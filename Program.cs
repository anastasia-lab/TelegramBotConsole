using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;

namespace TelegramBotConsole
{
    class Program
    {
        private static string token { get; set; } = "5615146983:AAHGyTlQUy0M71opjA3K5vgSVY7iNB9kwzI";
        static ITelegramBotClient bot = new TelegramBotClient(token);
        
        /// <summary>
        /// обработка обновлений сообщений
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="update"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
         
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                var FirstNameUser = update.Message.From.FirstName;

                if (message.Text != null)
                {
                    if (message.Text.ToLower() == "/start")
                    {
                        await botClient.SendTextMessageAsync(message.Chat, $"Добро пожаловать в бот, {FirstNameUser}!");

                        var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new []
                            {
                                new KeyboardButton("Курс валют"),
                                 new KeyboardButton("Сохранить файл")
                            },

                        });

                        keyboard.ResizeKeyboard = true; // изменение размера клавиатуры

                        Thread.Sleep(1000);

                        //чтобы пользователь увидел клавиатуру
                        await botClient.SendTextMessageAsync(message.Chat.Id, text: "Что хотите сделать", replyMarkup: keyboard);


                        return;
                    }
                    //await botClient.SendTextMessageAsync(message.Chat, "Хаю-Хай");

                    switch (message.Text)
                    {
                        case "Курс валют":
                            await GetMessageCurrencyRateAsync(botClient, update, cancellationToken);
                            break;
                        case "Сохранить файл":
                            await GetSaveDocumentAsync(botClient, update);
                            break;
                    }
                }

                if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                {
                    await HandleCallbackQueryAsync(botClient, update.CallbackQuery);
                    return;
                }
            }
        }


        /// <summary>
        /// Получение информации об актуальном курсе валют
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="update"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task GetMessageCurrencyRateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {

        }

        /// <summary>
        /// Сохранение файла на рабочем столе
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="update"></param>
        /// <returns></returns>
        public static async Task GetSaveDocumentAsync(ITelegramBotClient botClient, Update update)
        {
            var document = update.Message.Document;
            if (document != null)
            {
                Console.WriteLine($"Дата: {DateTime.Now.ToLongTimeString()}, Документ: {update.Message.Document.FileName}, Размер: {update.Message.Document.FileSize}");
                await DownloadDocumentsAsync(update.Message.Document.FileId, update.Message.Document.FileName, update);
                return;
            }
            //else if (update.Message.Photo != null)
            //{
                
            //    return;
            //}
        }

        /// <summary>
        /// Скачивание файла
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task DownloadDocumentsAsync(string fileId, string fileName, Update update)
        {
            var file = await bot.GetFileAsync(fileId);
            var message = update.Message;

            string path = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}";
            string subpath = @"Telegram";
            string CombainPath = Path.Combine(path,subpath);
            if (!Directory.Exists(CombainPath))
            {
                Directory.CreateDirectory(CombainPath);
            }


            //var fileUrlPath = $"https://api.telegram.org/file/bot{token}/{file.FilePath}";
            //await bot.SendTextMessageAsync(message.Chat.Id, file.FileId);

            await ShowFilesAsync(bot, $"https://api.telegram.org/file/bot + {token} + / +{file.FilePath}", update);

            using var fileStream = System.IO.File.OpenWrite(CombainPath + fileName);
            await bot.DownloadFileAsync(file.FilePath, fileStream);
            fileStream.Close();


        }

        /// <summary>
        /// Список имеющихся файлов для загрузки
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="path"></param>
        /// <param name="update"></param>
        /// <returns></returns>
        public static async Task ShowFilesAsync(ITelegramBotClient botClient, string path, Update update)
        {
            string[] fileArray = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);

            for (int i = 0; i < fileArray.Length; i++)
            {
                var lineKeyBoard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(fileArray[i], callbackData: fileArray[i])
                    }
                });

                await botClient.SendTextMessageAsync(update.Message.Chat.Id, text: "...", replyMarkup: lineKeyBoard);
            }

        }

        public static async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
                //if (callbackQuery.Data == )
                //    await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text: $"{callbackQuery.Data}");
            
        }

        /// <summary>
        /// обработчик ошибок
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="exception"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Ошибка API телеграмма:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => Newtonsoft.Json.JsonConvert.SerializeObject(exception) //любая другая ошибка
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;

            //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }


        static void Main(string[] args)
        {
            Console.WriteLine("Запущен бот " + $"\"{ bot.GetMeAsync().Result.FirstName}\"");

            var cts = new CancellationTokenSource(); //токен отмены
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions //получение обновлений
            {
                AllowedUpdates = { }, // receive all update types
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();
      
        }
    }
}
