﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MarkovNextGen;
using Newtonsoft.Json;
using BestGirl.Responses;
using Monika.Emotions;
using Monika.AdminController;

namespace Monika
{
    class Program
    {
        static void Main(string[] args)
        {
            var mkbot = new MonikaBot();
            mkbot.MainAsync().GetAwaiter().GetResult(); // Just let it run in background

            while(!mkbot.IsReady)
            {
                // Wait for bot to be ready
            }

            AdminConsole admin = new AdminConsole();
            admin.Client = mkbot.Client;
            admin.Generator = mkbot.Generator;
            admin.Manager = mkbot.Manager;

            Console.Write("> ");
            var cmd = Console.ReadLine();
            while (cmd != "exit")
            {
                admin.ParseCommand(cmd);
                Console.Write("> ");
                cmd = Console.ReadLine();
            }
        }
    }

    public class MonikaBot
    {
        const string TOKFILE = "tokens.pdo";
        Random randy = new Random();

        public Markov Generator { get; private set; } = new Markov();
        public DiscordSocketClient Client { get; private set; } = new DiscordSocketClient();
        public EmotionManager Manager { get; private set; }

        public Boolean IsReady { get; private set; } = false;

        TokenSet Tokens
        {
            get
            {
                if (!File.Exists(TOKFILE))
                {
                    const string DEVELOPMENT = "Dev Token Here";
                    const string RELEASE = "Release Token Here";
                    return new TokenSet(DEVELOPMENT, RELEASE);
                }
                else
                {
                    var json = File.ReadAllText(TOKFILE);
                    return JsonConvert.DeserializeObject<TokenSet>(json);
                }
            }
        }
        public static Boolean TaggedIn(SocketMessage msg, String username)
        {
            var _tagged = false;
            foreach (var usr in msg.MentionedUsers)
            {
                if (usr.Username == username)
                {
                    _tagged = true;
                }
            }
            return _tagged;
        }

        public static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
        //public async Task SendMessage(String message)
        //{
        // TODO add asynchronous message sending
        //}
        public async Task MessageReceived(SocketMessage msg)
        {
            var author = msg.Author.Username;
            var text = msg.Content;

            if (author != Client.CurrentUser.Username) // Don't process our own messages
            {
                if (TaggedIn(msg, Client.CurrentUser.Username))
                {
                    if (text.Contains(" say "))
                    {
                        var response = text.Substring(text.IndexOf("say") + 4);
                        await msg.Channel.SendMessageAsync(response);
                    }
                    else if (text.Contains(" markov "))
                    {
                        var ss = text.Substring(text.IndexOf("markov") + 7);
                        if (Int32.TryParse(ss, out int i))
                        {
                            var response = Generator.Generate(i);
                            await msg.Channel.SendMessageAsync(response);
                        }
                    }
                    else if (text.Contains(" nick "))
                    {
                        var ss = text.Substring(text.IndexOf("nick") + 5);
                        // We'll need a check to make sure no one else was tagged
                        foreach (var usr in msg.MentionedUsers)
                        {
                            await (usr as IGuildUser).ModifyAsync(x => x.Nickname = ss);
                        }
                    }
                    else
                    {
                        var line = Lines.VoiceLines.RandomElement<string>();
                        var response = line.Replace("[player]", msg.Author.Mention);
                        await msg.Channel.SendMessageAsync(response);
                    }
                }
                else if (text.StartsWith("delete"))
                {
                    var character = text.Substring(7);
                    var response = character + ".chr deleted";
                    await msg.Channel.SendMessageAsync(response);
                }
                else if (msg.Channel.Name.StartsWith("markov-")) 
                {
                    Generator.AddToChain(text); // Record the message
                }
            }
        }

        // Deprecated?
        public async Task ChangeAvatar(string filename)
        {
            var me = Client.CurrentUser;
            using (FileStream fs = File.OpenRead(filename))
            {
                var avatar = new Image(fs);
                await me.ModifyAsync(x =>
                {
                    x.Avatar = avatar;
                });
            }
        }

        public async Task Ready()
        {
            Manager = new EmotionManager(Client);
            IsReady = true;
            // await ChangeAvatar("someavatar.jpg");
        }

        public async Task MainAsync()
        {

            Client.Log += Log;
            Client.MessageReceived += MessageReceived;
            Client.Ready += Ready;

            var TOKEN = Tokens.Development;

            await Client.LoginAsync(TokenType.Bot, TOKEN);
            await Client.StartAsync();

            // Removed delay
        }
    }

    public class TokenSet
    {
        public String Development { get; set; }
        public String Release { get; set; }

        public TokenSet(String dev, String release)
        {
            Development = dev;
            Release = release;
        }

        public TokenSet()
        {
            Development = String.Empty;
            Release = String.Empty;
        }
    }

    // Don't call any of these rapidly
    public static class Extensions
    {
        public static T RandomElement<T>(this List<T> lst)
        {
            Random randy = new Random();
            return lst[randy.Next(0, lst.Count)];
        }
        public static T RandomElement<T>(this T[] lst)
        {
            Random randy = new Random();
            return lst[randy.Next(0, lst.Length)];
        }

        public static Boolean OneIn(int i)
        {
            Random randy = new Random();
            return (randy.Next(0, i) == 1);
        }
    }
}
