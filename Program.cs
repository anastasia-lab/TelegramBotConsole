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
                                new KeyboardButton("Сохранить файл")
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
                        case "Сохранить файл":
                            await GetSaveDocumentAsync(botClient, update);
                            break;
                        //default:
                        //    await botClient.SendTextMessageAsync(message.Chat.Id, text: "Ой, такой команды я не знаю.");
                        //    await botClient.SendTextMessageAsync(message.Chat.Id, text: "Вы можете узнать актуальный курс валют или сохранить файлы.");
                        //    break;
                    }
                }
            }
            
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
            {
               await HandleCallbackQueryAsync(botClient, update.CallbackQuery);
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
            Thread.Sleep(500);

            await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                text: $"Файл с актуальным курсом валют сохранен на Вашем компьютере в папке \"Телеграм\" на рабочем столе");

            await botClient.SendTextMessageAsync(update.Message.Chat.Id, text: $"Сегодня {DateTime.Now.ToShortDateString()}");

            string FilePath = Path.Combine($@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}", @"Telegram",
                $"Курс валют на {DateTime.Now.ToShortDateString()}.txt");

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
        public static async Task GetSaveDocumentAsync(ITelegramBotClient botClient, Update update)
        {
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Документы", callbackData:$"doc"),
                    InlineKeyboardButton.WithCallbackData(text: "Фото", callbackData:$"photo"),
                },

            });

            await botClient.SendTextMessageAsync(update.Message.Chat.Id, text: "Выберите файл", replyMarkup: inlineKeyboard);

            await ShowFilesAsync(botClient, $"https://api.telegram.org/file/bot + <{token}> + <FilePath>", update);


            if (update.Message.Document != null)
            {
                Console.WriteLine($"Дата: {DateTime.Now.ToLongTimeString()}, Документ: {update.Message.Document.FileName}, Размер: {update.Message.Document.FileSize}");
                await DownloadDocumentsAsync(update.Message.Document.FileId, update.Message.Document.FileName, update);

                return;
            }

            if (update.Message.Photo != null)
            {
                await DownloadDocumentsAsync(update.Message.Photo[update.Message.Photo.Length -1].FileId, update.Message.From.FirstName, update);
            }
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

            await ShowFilesAsync(bot, file.FilePath, update);
            //await bot.SendTextMessageAsync(message.Chat.Id, file.FileId);

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
            string[] files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);

            List<string> list = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                list.Add(files[i]);
            }
            await botClient.SendTextMessageAsync(update.Message.Chat.Id, text: "Файлы");
            await botClient.SendTextMessageAsync(update.Message.Chat.Id, text: $"{list}");
            //var inlineKeyboard = new InlineKeyboardMarkup(GetInlineKeyboard(files));

            //for (int i = 0; i < files.Length; i++)
            //{
            //    await botClient.SendTextMessageAsync(update.Message.Chat.Id, text:"Файлы", replyMarkup: inlineKeyboard);
            //}

        }

        private static InlineKeyboardButton[][] GetInlineKeyboard(string[] stringArray)
        {
            var keyboardInline = new InlineKeyboardButton[stringArray.Length][];
            var keyboardButtons = new InlineKeyboardButton[stringArray.Length];
            for (var i = 0; i < stringArray.Length; i++)
            {

                keyboardButtons[i] = new InlineKeyboardButton(stringArray[i]);
            }
            for (var j = 1; j <= stringArray.Length; j++)
            {
                keyboardInline[j - 1] = keyboardButtons.Take(1).ToArray();
                keyboardButtons = keyboardButtons.Skip(1).ToArray();
            }

            return keyboardInline;
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

            if (callbackQuery.Data.StartsWith("Документы"))
            {
                await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text: $"Вы выбрали: {callbackQuery.Data}");
            }

            if (callbackQuery.Data == "Фото")
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
