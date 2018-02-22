﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Net.WebSocket;
using EmojiButler.Commands;
using EmojiButler.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmojiButler
{
    public class EmojiButler
    {
        public static DiscordClient client;
        public static DiscordEmojiClient deClient;
        public static CommandsNextModule commands;
        public static Configuration configuration;
        static InteractivityModule interactivity;

        static void Main(string[] args) => MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            configuration = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText("config.json"));
            deClient = new DiscordEmojiClient();

            CancellationToken token = new CancellationTokenSource().Token;
            new Task(() => deClient.RefreshEmoji(), token, TaskCreationOptions.LongRunning).Start();

            client = new DiscordClient(new DiscordConfiguration
            {
                #if DEBUG
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug,
                #endif
                Token = configuration.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            });

            commands = client.UseCommandsNext(new CommandsNextConfiguration
            {
                EnableDefaultHelp = false,
                StringPrefix = configuration.Prefix
            });

            client.SetWebSocketClient<WebSocket4NetCoreClient>();
            interactivity = client.UseInteractivity(new InteractivityConfiguration());

            commands.RegisterCommands<EmojiCommands>();
            commands.RegisterCommands<GeneralCommands>();

            commands.CommandErrored += async (CommandErrorEventArgs e) => 
            {
                if (e.Exception is ArgumentException arg)
                {
                    // lol
                    if (arg.Message.StartsWith("Max message length"))
                        await e.Context.RespondAsync("I was able to generate a response, but it was a wayyy too long for Discord...");
                    else
                        await e.Context.RespondAsync($"Not enough arguments were supplied to this command.\nUsage: ``{Util.GenerateUsage(e.Command)}``");
                }
                else if (e.Exception is ChecksFailedException ex)
                {
                    string msg = "Checks for this command have failed: ";
                    foreach (CheckBaseAttribute a in ex.FailedChecks)
                    {
                        if (a is RequirePermissionsAttribute)
                            msg += "\nMissing Permissions";

                        else if (a is CooldownAttribute cd)
                            msg += $"\nCooldown, {(int)cd.GetRemainingCooldown(e.Context).TotalSeconds}s left";
                    }
                    await e.Context.RespondAsync(msg);
                }
                else if (e.Exception is InvalidOperationException)
                    await e.Context.RespondAsync("This command is not available for use in DMs.");
                else if (e.Exception is CommandNotFoundException)
                {
                    if (e.Context.Guild == null)
                        await e.Context.RespondAsync("That's an invalid command.");
                }
                else
                {
                    await e.Context.RespondAsync($"An error has occurred. Please report this with ``{configuration.Prefix}reportissue <details>``.");
                    client.DebugLogger.LogMessage(LogLevel.Critical, "Error", e.Exception.ToString(), DateTime.Now);
                }
            };

            await client.ConnectAsync();
            client.Ready += async (ReadyEventArgs a) =>
            {
                await client.UpdateStatusAsync(new DiscordGame($"{configuration.Prefix}help | https://discordemoji.com"), UserStatus.DoNotDisturb);
            };
            await Task.Delay(-1);
        }
    }
}
