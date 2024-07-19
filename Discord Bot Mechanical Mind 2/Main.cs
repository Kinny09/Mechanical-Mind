using System;
using System.IO;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using System.Net.Http.Headers;
using System.ComponentModel;

namespace DiscordBot
{
    public class ProfessorMakidia
    {
        //Setting up calls the bot needs
        private DateTime CurrentTime() { return DateTime.Now; }
        private Random random = new Random();

        //Collecting all the information the bot needs and creating the variables
        private string token = File.ReadAllText("Token.txt").Trim();

        private List<string> listOfLoreStrings = File.ReadAllLines("Lore.txt").ToList();
        private int loreTimeDelayMinutes = int.Parse(File.ReadAllText("LoreDelayMinutes.txt"));
        private DateTime lastDateLoreExecuted;

        private int currentDate;
        private int dateTimeDelayDays;
        private TimeOnly dateTriggerTime;
        private DateOnly lastDateTrigger;

        //The discord socket client
        private DiscordSocketClient _client;

        private static void Main(string[] args)
            => new ProfessorMakidia().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        { 
            //Bot configering
            var config = new DiscordSocketConfig { AlwaysDownloadUsers = true, GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages };
            _client = new DiscordSocketClient(config);

            //Getting the date information
            List<string> dateInformation = GetDateInformation();
            currentDate = int.Parse(dateInformation[0]);
            dateTimeDelayDays = int.Parse(dateInformation[1]);
            dateTriggerTime = TimeOnly.Parse(dateInformation[2]);
            lastDateTrigger = DateOnly.Parse(dateInformation[3]);

            //The commands
            _client.Log += Log;
            _client.UserJoined += AnnounceUserJoined;
            _client.MessageReceived += PostALoreSnippet;
            _client.UserLeft += AnnounceUserLeft;
            _client.Ready += UpdateDateLoop;

            //Starting the bot
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        //----------Commands----------//
        private async Task AnnounceUserJoined(SocketGuildUser user)
        {
            var guild = user.Guild;
            var channel = guild.GetTextChannel(1251900740075655269);
            await Task.Delay(100);
            if (channel != null)
            {
                await channel.SendMessageAsync($"Welcome {user.Mention} to {guild.Name}! Head over to <#1251899086723092510> to learn about the rules and <#1251899167446401055> to learn about the setting of the world!\n" +
                    $"\n" +
                    $"Once you've done that you can head to <#1251914497082396743> to create or nation, or...if you want to continue reading about the mechanics and lore of the world, you can go to <#1259886504214397040> or <#1251910530520846478>!");
            }

            await user.AddRoleAsync(1263630944422727794);
        }

        private static async Task AnnounceUserLeft(SocketGuild guild, SocketUser user)
        {
            var channel = guild.GetTextChannel(1253001214807904357);
            if (channel != null)
            {
                await channel.SendMessageAsync($"Goodbye {user.Mention} ({user.Username}) it was nice knowing you!");
            }
        }

        private async Task PostALoreSnippet(SocketMessage message)
        {
            if (message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id))
            {
                if (CooldownMethod())
                {
                    int randomNumber = random.Next(0, listOfLoreStrings.Count);
                    await message.Channel.SendMessageAsync(listOfLoreStrings[randomNumber]);
                }
                else
                {
                    await message.Channel.SendMessageAsync("Have some patience, I'm shuffling through my *countless* notes");
                }
            }
        }

        private async Task UpdateDateLoop()
        {
            while (true)
            {
                int millisecondDelay = (60 - CurrentTime().Minute) * 60000; //Calculating how long the delay should be, the bot checks the time and date every hour
                await Task.Delay(millisecondDelay); //The delay

                //Updating all the date triggers and information
                List<string> dateInformation = GetDateInformation();
                currentDate = int.Parse(dateInformation[0]);
                dateTimeDelayDays = int.Parse(dateInformation[1]);
                dateTriggerTime = TimeOnly.Parse(dateInformation[2]);
                lastDateTrigger = DateOnly.Parse(dateInformation[3]);

                Console.WriteLine($"{DateOnly.FromDateTime(CurrentTime())} >= {lastDateTrigger.AddDays(dateTimeDelayDays)}");
                Console.WriteLine(DateOnly.FromDateTime(CurrentTime()) >= lastDateTrigger.AddDays(dateTimeDelayDays));

                TimeOnly timeNow = TimeOnly.FromDateTime(CurrentTime());

                Console.WriteLine($"{timeNow} >= {dateTriggerTime.AddMinutes(-1)} && {timeNow} <= {dateTriggerTime.AddMinutes(1)}");
                Console.WriteLine(timeNow >= dateTriggerTime.AddMinutes(-1) && timeNow <= dateTriggerTime.AddMinutes(1));

                if (DateOnly.FromDateTime(CurrentTime()) >= lastDateTrigger.AddDays(dateTimeDelayDays) && (timeNow >= dateTriggerTime.AddMinutes(-1) && timeNow <= dateTriggerTime.AddMinutes(1)))
                {
                    var channel = _client.GetChannel(1253022447880372254) as IMessageChannel;

                    int newDate = currentDate + 1;

                    using (StreamWriter writer = new StreamWriter("CurrentDate.txt"))
                    {
                        writer.WriteLine(newDate);
                        writer.WriteLine(dateInformation[1]);
                        writer.WriteLine(dateInformation[2]);
                        writer.WriteLine(DateOnly.FromDateTime(CurrentTime()));
                    }

                    if (channel != null)
                    {
                        await channel.SendMessageAsync($"# The year is now {newDate} \n Happy new year! \n <@&1251897850229690458>");
                    }

                    dateInformation = GetDateInformation();
                    currentDate = int.Parse(dateInformation[0]);
                    lastDateTrigger = DateOnly.Parse(dateInformation[3]);
                }
            }
        }

        //Non-Command methods nececcary for bot functions
        private bool CooldownMethod()
        {
            Console.WriteLine(lastDateLoreExecuted);
            Console.WriteLine($"{lastDateLoreExecuted.AddMinutes(loreTimeDelayMinutes)}< {CurrentTime()}");

            if (lastDateLoreExecuted.AddMinutes(loreTimeDelayMinutes) <= CurrentTime())
            {
                lastDateLoreExecuted = CurrentTime();
                return true;
            }
            else
            {
                return false;
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private List<String> GetDateInformation()
        {
            List<string> dateInformation = new List<string>();

            using (StreamReader reader = new StreamReader("CurrentDate.txt"))
            {
                while (!reader.EndOfStream)
                {
                    dateInformation.Add(reader.ReadLine());
                }
            }

            return dateInformation;
        }
    }
}
