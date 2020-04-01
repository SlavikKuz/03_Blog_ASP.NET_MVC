﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TeleDronBot.Base.BaseClass;
using TeleDronBot.Bot;
using TeleDronBot.DTO;
using TeleDronBot.PilotCommands.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleDronBot.PilotCommands
{
    class ShowUsersCommand : BaseCommand
    {
        public ShowUsersCommand(TelegramBotClient client, MainProvider provider) : base(client, provider) { }

        public async Task Response(MessageEventArgs messageObject)
        {
            long chatid = messageObject.Message.Chat.Id;
            int currStep = await provider.userService.GetCurrentActionStep(chatid);
            string messageText = messageObject.Message.Text;
            int countUser;

            if (currStep == 1)
            {
                await client.SendTextMessageAsync(chatid, "Select option", 0, false, false, 0, KeyboardHandler.ShowPartners());
                await provider.userService.ChangeAction(chatid, "Pilots near", ++currStep);
                return;
            }
            if (currStep == 2)
            {
                if (messageText == "All pilots")
                {
                    await provider.userService.ChangeAction(chatid, "Pilots near", ++currStep);
                    await client.SendTextMessageAsync(chatid, "Select option", 0, false, false, 0, KeyboardHandler.ShowPartnersPilot());
                    return;
                }
                else
                {
                    await client.SendTextMessageAsync(chatid, "Wrong command");
                }
            }
            if (currStep == 3)
            {
                if (messageText == "Geolocation" || messageText == "By region")
                {
                    countUser = await provider.showUserService.CountUsersAsync();
                    if (messageText == "Geolocation")
                    {

                        return;
                    }
                    if (messageText == "By region")
                    {
                        if (countUser > 1)
                        {
                            GenerateButtons buttons = new GenerateButtons(client, provider);
                            ReplyKeyboardMarkup keyboard = await buttons.GenerateKeyBoards();
                            await client.SendTextMessageAsync(chatid, "Select region", 0, false, false, 0, keyboard);
                            await provider.userService.ChangeAction(chatid, "Partners near", ++currStep);
                            return;
                        }
                        else if (countUser == 1)
                        {
                            await client.SendTextMessageAsync(chatid, "You are the only pilot", 0, false, false, 0, KeyboardHandler.Markup_Back_From_First_Action());
                            return;
                        }
                    }
                }
                else
                {
                    await client.SendTextMessageAsync(chatid, "Wrong option");
                    await client.SendTextMessageAsync(chatid, "Try again");
                }
            }
            if (currStep == 4)
            {
                List<string> regions = await provider.regionService.GetAllRegions();
                if (regions.Contains(messageText))
                {
                    UserDTO user = await provider.showUserService.GetFirstUserForCommand(chatid, messageText);
                    await provider.showUserService.ChangeMessageId(chatid, messageObject.Message.MessageId);
                    string message = $"Pilot:{user.FIO} \n" +
                    $"tel:{user.Phone}";
                    await client.SendTextMessageAsync(chatid, message);
                    return;
                }
                else
                {
                    await client.SendTextMessageAsync(chatid, "Wrong region");
                }
            }
        }
    }
}
