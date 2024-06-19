// See https://aka.ms/new-console-template for more information

using System;
using System.IO;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MyBot
{
    public class Program
    {
        private DateTime CurrentTime() { return DateTime.Now; }
        
        private int _Delay; //This is in minutes
        private DateTime _LastExecution;

        private List<string> _lore;

        private Random _Random = new Random();

        private DiscordSocketClient _client;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            _lore = File.ReadAllLines("Lore.txt").ToList();
            _Delay = int.Parse(File.ReadAllText("DelayMinutes.txt"));
            _LastExecution = CurrentTime().AddMinutes(-_Delay);

            var config = new DiscordSocketConfig
            {
                //LogLevel = LogSeverity.Debug,
                AlwaysDownloadUsers = true,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages
            };
            _client = new DiscordSocketClient(config);
            _client.Log += Log;
            _client.UserJoined += AnnounceUserJoined;
            _client.MessageReceived += HandleMessageReceived;

            var token = "MTAwOTA1ODE0OTU2MDQ5MjAzMw.G_PzH8.k3cfHe2_XxY7yHc_Y1c3dYc0NhmD2ydbN2MK24";
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            //Console.WriteLine($"Logged in as {_client.CurrentUser?.Username}");

            //await Task.Delay(5000);

            //Console.WriteLine($"Current user after 5 seconds: {_client.CurrentUser?.Username}");

            await Task.Delay(-1);
        }

        private async Task AnnounceUserJoined(SocketGuildUser user)
        {
            var guild = user.Guild;
            var channel = guild.GetTextChannel(1251900740075655269);
            if (channel != null)
            {
                await channel.SendMessageAsync($"Welcome {user.Mention} to {guild.Name}! Head over to <#1012490936347009135> and <#1012380840380088371> to learn about the rules and setting of the world.\n" +
                    $"\n" +
                    $"If you wanna take part in current ongoing RP, head over to <#1046585931584507924> to learn about it, and go to <#1048025572539912272> to submit a character!");
            }
        }

        private async Task HandleMessageReceived(SocketMessage message)
        {
            if (message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id))
            {
                if (CooldownMethod())
                {
                    int randomNumber = _Random.Next(0, _lore.Count);
                    await message.Channel.SendMessageAsync(_lore[randomNumber]);
                }
                else
                {
                    await message.Channel.SendMessageAsync("Please be patient, my lore punchcard is reformatting right now");
                }
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private bool CooldownMethod()
        {
            Console.WriteLine(_LastExecution);
            Console.WriteLine($"{_LastExecution.AddMinutes(_Delay)}< {CurrentTime()}");

            if (_LastExecution.AddMinutes(_Delay) <= CurrentTime())
            {
                _LastExecution = CurrentTime();
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}