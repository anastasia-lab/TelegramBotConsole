using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;
using System.Net;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types.InputFiles;

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
                                new KeyboardButton("Узнать курс валют"),
                                new KeyboardButton("Посмотреть сохраненные файлы")
                            },

                        });

                        keyboard.ResizeKeyboard = true; // изменение размера клавиатуры
                        keyboard.OneTimeKeyboard = true; // скрывает клавиатуру, как только она будет использована
                        Thread.Sleep(500);

                        //чтобы пользователь увидел клавиатуру
                        await botClient.SendTextMessageAsync(message.Chat.Id, text: "Что хотите сделать", replyMarkup: keyboard);


                        return;
                    }

                    switch (message.Text)
                    {
                        case "Узнать курс валют":
                            await GetMessageCurrencyRateAsync(botClient, update, cancellationToken);
                            break;
                        case "Посмотреть сохраненные файлы":
                            await ShowFilesAsync(botClient, update);
                            break;
                        default:
                            //await botClient.SendTextMessageAsync(message.Chat.Id, text: "Ой, такой команды я не знаю.");
                            //await botClient.SendTextMessageAsync(message.Chat.Id, text: "Вы можете узнать актуальный курс валют или " +
                            //    "посмотреть сохраненные файлы.");
                            break;
                    }
                }
            }
            
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
            {
               await HandleCallbackQueryAsync(botClient, update.CallbackQuery);
               return;
            }

            if (update.Message.Type == Telegram.Bot.Types.Enums.MessageType.Document ||
                update.Message.Type == Telegram.Bot.Types.Enums.MessageType.Photo ||
                update.Message.Type == Telegram.Bot.Types.Enums.MessageType.Audio)
            {
                await SaveFilesAsync(botClient, update);
                return;
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
            string urlCurse = $"https://www.cbr-xml-daily.ru/daily.xml";

            WebClient client = new WebClient();
            var xml = client.DownloadString(urlCurse);
            XDocument xdoc = XDocument.Parse(xml);
            var elementCurse = xdoc.Element("ValCurs").Elements("Valute");

            string CurseDollar = elementCurse.Where(x => x.Attribute("ID").Value == "R01235").Select(x => x.Element("Value").Value).FirstOrDefault();
            string CurseEuro = elementCurse.Where(x => x.Attribute("ID").Value == "R01239").Select(x => x.Element("Value").Value).FirstOrDefault();
            string CurseTurkishLira = elementCurse.Where(x => x.Attribute("ID").Value == "R01700J").Select(x => x.Element("Value").Value).FirstOrDefault();
            string CurseBritishPoundSterling = elementCurse.Where(x => x.Attribute("ID").Value == "R01035").Select(x => x.Element("Value").Value).FirstOrDefault();

            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Курс Доллара США 💵", callbackData:$"Курс доллара: {CurseDollar}"),
                    InlineKeyboardButton.WithCallbackData(text: "Курс Евро 💶", $"Курс евро: {CurseEuro}"),
                },

                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Курс Турецкой лиры 💳", callbackData:$"Курс Турецкой лиры: {CurseTurkishLira}"),
                    InlineKeyboardButton.WithCallbackData(text: "Курс Британского фунта стерлинга 💷", $"Курс Фунта стерлинга: {CurseBritishPoundSterling}"),
                },
            });

            await botClient.SendTextMessageAsync(update.Message.Chat.Id, text:"Выберите валюту", replyMarkup: inlineKeyboard);
            //Thread.Sleep(500);

            await botClient.SendTextMessageAsync(update.Message.Chat.Id, text: $"Сегодня {DateTime.Now.ToShortDateString()}");

            if (!Directory.Exists(Path.Combine($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}", @"Telegram",@"Курс валют")))
            {
                Directory.CreateDirectory(Path.Combine($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}", @"Telegram", @"Курс валют"));
            }

            string FilePath = Path.Combine($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}", @"Telegram",
               @"Курс валют", $"Курс валют на {DateTime.Now.ToShortDateString()}.txt");


            using var fileStream = System.IO.File.Create(FilePath);

            using (StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.UTF8))
            {
                streamWriter.Write($"Курс валют на {DateTime.Now.ToShortDateString()}:\n" +
                    $"\0Курс евро {CurseEuro}\n Курс доллара: {CurseDollar}\n Курс Турецкой лиры: {CurseTurkishLira}\n" +
                    $"\0Курс Британского фунта стерлинга: {CurseBritishPoundSterling}"
                    );
            }
        }

        /// <summary>
        /// Сохранение файла на рабочем столе
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="update"></param>
        /// <returns></returns>
        public static async Task SaveFilesAsync(ITelegramBotClient botClient, Update update)
        {
            if (update.Message.Document != null)
            {
                await DownloadFilesAsync(update.Message.Document.FileId, update.Message.Document.FileName, update);
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, text:$"Файл {update.Message.Document.FileName} сохранен. Тип: {update.Message.Type}", 
                    replyToMessageId: update.Message.MessageId);
                return;
            }

            if (update.Message.Photo != null)
            {
                var fileId = botClient.GetFileAsync(update.Message.Photo.Last().FileId);
                string[] ImagePath = fileId.Result.FilePath.Split('/');
                string ImageName = ImagePath[ImagePath.Length - 1];

                await DownloadFilesAsync(update.Message.Photo.Last().FileId, ImageName, update);
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, text: $"Файл {ImageName} сохранен. Тип: {update.Message.Type}", 
                    replyToMessageId: update.Message.MessageId);
            }

            if (update.Message.Audio != null)
            {
                await DownloadFilesAsync(update.Message.Audio.FileId, update.Message.Audio.FileName, update);
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, text: $"Файл сохранен.",
                    replyToMessageId: update.Message.MessageId);
                return;
            }
        }

        /// <summary>
        /// Скачивание файла
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task DownloadFilesAsync(string fileId, string fileName, Update update)
        {
            var file = await bot.GetFileAsync(fileId);
            var message = update.Message;

            string path = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
            string subpath = @"Telegram";
            string CombainPath = Path.Combine(path, subpath, "Документы");
            if (!Directory.Exists(CombainPath))
            {
                Directory.CreateDirectory(CombainPath);
            }

            using var fileStream = System.IO.File.OpenWrite(Path.Combine(CombainPath, fileName));
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
        public static async Task ShowFilesAsync(ITelegramBotClient botClient, Update update)
        {
            string CombainPath = Path.Combine($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}", @"Telegram");
            if (!Directory.Exists(CombainPath))
            {
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, text: "Ой. Кажется, что-то пошло не то.");
            }

            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Список сохраненных курсов валют", callbackData: $"{ShowCurseFileAsync(botClient,update)}"),
                    InlineKeyboardButton.WithCallbackData(text: "Список сохраненных документов", callbackData:$""),
                },
            });

            await botClient.SendTextMessageAsync(update.Message.Chat.Id, text: "Выбирите документ", replyMarkup: inlineKeyboard);

            #region Курс валют
            
            //var directoryCurse = new DirectoryInfo(Path.Combine(CombainPath, "Курс валют"));
            //FileInfo[] CurseFiles = directoryCurse.GetFiles();

            //await botClient.SendTextMessageAsync(update.Message.Chat.Id, text: "Файлы");

            //foreach (var CurseFile in CurseFiles)
            //{
            //    await using Stream stream = System.IO.File.OpenRead(CurseFile.FullName);
            //    Message message = await botClient.SendDocumentAsync(
            //    chatId: update.Message.Chat.Id,
            //    document: new InputOnlineFile(content: stream, fileName: CurseFile.Name));
            //}
            #endregion

            #region Документы
            //var directoryDocuments = new DirectoryInfo(Path.Combine(CombainPath, "Документы"));
            //FileInfo[] DocumentFiles = directoryCurse.GetFiles();

            //foreach (var DocumentFile in DocumentFiles)
            //{
            //    await using Stream stream = System.IO.File.OpenRead(DocumentFile.FullName);
            //    Message message = await botClient.SendDocumentAsync(
            //    chatId: update.Message.Chat.Id,
            //    document: new InputOnlineFile(content: stream, fileName: DocumentFile.Name));
            //}
            #endregion
        }


        private static async Task ShowCurseFileAsync(ITelegramBotClient botClient, Update update)
        {
            string CombainPath = Path.Combine($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}", @"Telegram");
            if (!Directory.Exists(CombainPath))
            {
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, text: "Ой. Кажется, что-то пошло не то.");
            }

            var directoryCurse = new DirectoryInfo(Path.Combine(CombainPath, "Курс валют"));
            FileInfo[] CurseFiles = directoryCurse.GetFiles();

            await botClient.SendTextMessageAsync(update.Message.Chat.Id, text: "Файлы");

            foreach (var CurseFile in CurseFiles)
            {
                await using Stream stream = System.IO.File.OpenRead(CurseFile.FullName);
                Message message = await botClient.SendDocumentAsync(
                chatId: update.Message.Chat.Id,
                document: new InputOnlineFile(content: stream, fileName: CurseFile.Name));
            }
        }
        
        
        
        
        /// <summary>
        /// Обработчик нажатия inline кнопок
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="callbackQuery"></param>
        /// <returns></returns>
        public static async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            if (callbackQuery.Data.StartsWith("Курс"))
            {
                await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text: $"{callbackQuery.Data}");
            }

            if (callbackQuery.Data.StartsWith("Список сохраненных курсов валют"))
            {
                await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text: $"{callbackQuery.Data}");
            }

            if (callbackQuery.Data.StartsWith("Список сохраненных документов"))
            {
                await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text: $"{callbackQuery.Data}");
            }
            return;
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
