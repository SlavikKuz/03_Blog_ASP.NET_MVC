﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TeleDronBot.Base.BaseClass;
using TeleDronBot.Bot.CommonHandler;
using TeleDronBot.BusinessCommand;
using TeleDronBot.Chat;
using TeleDronBot.Commons;
using TeleDronBot.DTO;
using TeleDronBot.Geolocation;
using TeleDronBot.Interfaces.Bot;
using TeleDronBot.Logs;
using TeleDronBot.PilotCommands;
using TeleDronBot.PilotCommands.Callbacks;
using TeleDronBot.Repository;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleDronBot.Bot
{
    class MessageHandler : IMessageHandler
    {
        long chatid;
        TelegramBotClient client;
        MainProvider provider;
        CreateBuisnessTask buisnessAction;
        RegistrationPilotCommand registrationPilotsCommand;
        ShowOrders showOrders;

        StopChat stopChat;

        ShowUsersCommand showUserCommand;

        public MessageHandler(TelegramBotClient client, MainProvider provider)
        {
            this.client = client;
            this.provider = provider;
            buisnessAction = new CreateBuisnessTask(provider, client);
            registrationPilotsCommand = new RegistrationPilotCommand(client, provider);
            showOrders = new ShowOrders(client, provider);
            showUserCommand = new ShowUsersCommand(client, provider);

            stopChat = new StopChat(client, provider);
        }

        #region BusinessRegistration
        
        private async Task CommandHandler_BuisnessRegistrationKorporativ(long chatid, string message, MessageEventArgs messageObject)
        {
            int currentStep = await provider.userService.GetCurrentActionStep(chatid);
            UserDTO user = await provider.userService.FindById(chatid);
            DronDTO dron = new DronDTO();

            if (currentStep == 1)
            {
                user.FIO = message;
                await provider.userService.Update(user);
                await provider.userService.ChangeAction(chatid, "Corporate client registration", ++currentStep);
                await client.SendTextMessageAsync(chatid, "Enter your telephone number");
                return;
            }

            if (currentStep == 2)
            {
                if (RegularExpression.IsTelephoneCorrect(message))
                {
                    user.Phone = message;
                    user.BusinessPrivilage = 1;
                    await provider.userService.Update(user);
                    await client.SendTextMessageAsync(chatid, "Registered", 0, false, false, 0, KeyboardHandler.Markup_BuisnessmanMenu());
                    await provider.managerPush.SendMessage(client, chatid);
                    return;
                }
                else
                {
                    await client.SendTextMessageAsync(chatid, "Wrong telephone number");
                }
            }
        }
        #endregion

        private async Task CommandHandler_Start(long chatid)
        {
            await client.SendTextMessageAsync(chatid, "Sample text", 0, false, false, 0, KeyboardHandler.Markup_Start_AfterChange());
        }


        public async Task BaseHandlerMessage(MessageEventArgs message, string text)
        {
            Console.WriteLine($"Sent : {chatid}\nText:{message.Message.Text}\n");
            chatid = message.Message.Chat.Id;
            string messageText = message.Message.Text;
            string action = await provider.userService.GetCurrentActionName(chatid);
            UserDTO user = await provider.userService.FindById(chatid);

            await provider.userService.AuthenticateUser(chatid);

            //await UserLogs.WriteLog(chatid, messageText);

            bool isRegistration = await provider.userService.IsUserRegistration(chatid);

            if (messageText == "End dialog")
            {
                await stopChat.Request(chatid);
                return;
            }

            if (await provider.hubService.IsChatActive(chatid))
            {
                long[] arrChatid = await provider.hubService.GetChatId(chatid);

                long chatIdRecive = arrChatid[0] == chatid ? arrChatid[1] : arrChatid[0];
                await client.SendTextMessageAsync(chatIdRecive, messageText);

                return;
            }

            if (messageText == "Back")
            {
                await provider.userService.ChangeAction(chatid, "NULL", 0);
                await CommandHandler_Start(chatid);
                return;
            }

            if (messageText == "/start")
            {
                await CommandHandler_Start(chatid);
                return;
            }

            if (CommandList.RegistrationPilotCommandList().Contains(messageText) && user.PilotPrivilage != 0)
            {
                await client.SendTextMessageAsync(chatid, "You are already registered", 0, false, false, 0, KeyboardHandler.ChangeKeyBoardPilot(user.PilotPrivilage));
                return;
            }

            if (CommandList.RegistrationBuisnessCommandList().Contains(messageText) && user.BusinessPrivilage != 0)
            {
                await client.SendTextMessageAsync(chatid, "You are already registered", 0, false, false, 0, KeyboardHandler.Markup_BuisnessmanMenu());
                return;
            }

            if (messageText == "Flight right now")
            {

            }

            if (messageText == "Pilot")
            {
                if (user.PilotPrivilage == 0)
                {
                    await client.SendTextMessageAsync(chatid, "You logged in as a pilot"
                        , 0, false, false, 0, KeyboardHandler.Markup_Start_Pilot_Mode());
                    return;
                }
                else
                {
                    await client.SendTextMessageAsync(chatid, "You logged in as a pilot",
                        0, false, false, 0, KeyboardHandler.ChangeKeyBoardPilot(user.PilotPrivilage));
                    return;
                }
            }

            if (messageText == "Pilots near")
            {
                await provider.userService.ChangeAction(chatid, "Pilots near", 1);
                await showUserCommand.Response(message);
                return;
            }

            if (messageText == "Full access paid")
            {
                await client.SendTextMessageAsync(chatid, "There are several options for registration", 
                    0, false, false, 0, KeyboardHandler.Markup_Start_Pilot_Payment_Mode());
            }

            if (messageText == "Limited access free")
            {
                await client.SendTextMessageAsync(chatid, "There are several options for registration",
                    0, false, false, 0, KeyboardHandler.Markup_Start_Pilot_UnBuyer_Mode());
                return;
            }

            #region Paid registration for pilots

            if (messageText == "With insurance")
            {
                await provider.userService.ChangeAction(chatid, "With insurance", 1);
                await client.SendTextMessageAsync(chatid, "Enter your name:", 
                    0, false, false, 0, KeyboardHandler.Markup_Back_From_First_Action());
                return;
            }

            if (messageText == "W/o insurance")
            {
                await provider.userService.ChangeAction(chatid, "W/o insurance", 1);
                await client.SendTextMessageAsync(chatid, "Enter your name", 
                    0, false, false, 0, KeyboardHandler.Markup_Back_From_First_Action());
                return;
            }

            if (messageText == "Paid registration with insurance")
            {
                await provider.userService.ChangeAction(chatid, "Paid registration with insurance", 1);
                await client.SendTextMessageAsync(chatid, "Enter your name", 
                    0, false, false, 0, KeyboardHandler.Markup_Back_From_First_Action());
                return;
            }
            
            if (messageText == "Paid registration w/o insurance")
            {
                await provider.userService.ChangeAction(chatid, "Paid registration w/o insurance", 1);
                await client.SendTextMessageAsync(chatid, "Enter your name", 
                    0, false, false, 0, KeyboardHandler.Markup_Back_From_First_Action());
                return;
            }

            #endregion
            #region Free registration for pilots

            #endregion

            #region Corporate registration

            if (messageText == "Corporate")
            {
                await provider.userService.ChangeAction(chatid, "Corporate registration", 1);
                await client.SendTextMessageAsync(chatid, "Enter your name", 0, false, false, 0, KeyboardHandler.Markup_Back_From_First_Action());

                return;
            }
            if (messageText == "Private")
            {
                await provider.userService.ChangeAction(chatid, "Corporate registration", 1);
                await client.SendTextMessageAsync(chatid, "Enter your name", 0, false, false, 0, KeyboardHandler.Markup_Back_From_First_Action());
            }
            #endregion

            if (messageText == "Client")
            {
                if (user.BusinessPrivilage == 0)
                {
                    await client.SendTextMessageAsync(chatid, "There are several options for registration.",
                        0, false, false, 0, KeyboardHandler.Markup_Start_Buisness_Mode());
                }
                else
                {
                    await client.SendTextMessageAsync(chatid, "You are already registered.",
                        0, false, false, 0, KeyboardHandler.Markup_BuisnessmanMenu());
                }
                return;
            }

            if (messageText == "New task")
            {
                await provider.userService.ChangeAction(chatid, "Create new task", 1);
                await client.SendTextMessageAsync(chatid, "Enter region");
            }

            if (messageText == "Orders")
            {
                await showOrders.ShowAllOrders(chatid, message);
            }

            if (messageText == "Your Orders")
            {
                await showOrders.ShowAllOrders(chatid, message, true);
            }

            if (action != null)
            {
                if (action == "Pilots near")
                {
                    await showUserCommand.Response(message);
                }
                if (action == "Paid registration with insurance")
                {
                    await registrationPilotsCommand.CommandHandler_PaidRegistrationWithInsurance(user, messageText, message);
                    return;
                }
                if (action == "Paid registration w/o insurance")
                {
                    await registrationPilotsCommand.CommandHandler_PaidRegistrationWithoutInsurance(user, messageText, message); return;
                }
                if (action == "With insurance")
                {
                    await registrationPilotsCommand.CommandHandler_PaidRegistrationWithInsurance(user, messageText, message);
                    return;
                }
                if (action == "W/o insurance")
                {
                    await registrationPilotsCommand.CommandHandler_PaidRegistrationWithoutInsurance(user, messageText, message);
                    return;
                }
                if (action == "Coprorate client")
                {
                    await CommandHandler_BuisnessRegistrationKorporativ(chatid, messageText, message);
                    return;
                }
                if (action == "Create task")
                {
                    await buisnessAction.CreateTask(chatid, messageText, message);
                    return;
                }
            }
        }
    }
}
