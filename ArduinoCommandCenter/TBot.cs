using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace ArduinoCommandCenter
{
    class TBot
    {
        public static Queue<string> newMessages = new Queue<string>();
        TelegramBotClient bot;
        int adminchatid = 0;// REPLACE IT WITH YOUR OWN CHATID
        public void StartThread()
        {
            bot = new TelegramBotClient("TELEGRAM_BOT_TOKEN");
            int offset = 0;
            while (true)
            {
                try
                {
                    var updates = bot.GetUpdatesAsync(offset).Result;
                    foreach (var update in updates)
                    {
                        try
                        {
                            if (update.Message.Chat.Id == adminchatid)
                            {
                                Form1.newCommands.Enqueue(update.Message.Text);
                                bot.SendTextMessageAsync(adminchatid, "OK").Wait();
                            }
                            else
                            {

                            }
                        }
                        catch
                        {

                        }

                        offset = update.Id + 1;
                    }
                    while (newMessages.Count > 0)
                    {
                        var message = newMessages.Dequeue();
                        bot.SendTextMessageAsync(adminchatid, message).Wait();
                    }
                }
                catch
                {

                }
            }
        }
    }
}
