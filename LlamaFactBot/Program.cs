using System;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Net.Http; //Works, just need to manage HTML code which is dumb and stupid
using GoogleCSE;

namespace ConsoleApp1
{
    // Struct to pass any information
    //  down to commands from any context.
    public struct FactSettings
    {
        public bool llamaWhy;
    }

    class Program
    {
        private FactSettings fSettings = new FactSettings();

        private int numFacts = 0;
        private const string filepathFacts = @"biggae.txt";
        private List<string> llamaFacts;
        private int lineCountFacts = 0;

        private const string filepathResponses = @"llamaResponses.txt";
        private List<string> llamaKeywords;
        private List<string> llamaResponses;
        private int lineCountResponses = 0;

        private DiscordSocketClient _client;
        private CommandService _commandService;
        private CommandHandler _commandHandler;

        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            ////// LLAMA FACT ARRAY SETUP /////
            SetupLlamaFacts();

            //Basic setup
            _client = new DiscordSocketClient();
            _commandService = new CommandService();
            _commandHandler = new CommandHandler(_client, _commandService, ref fSettings, ref llamaFacts, ref numFacts, 
                ref llamaKeywords, ref llamaResponses);

            await _client.SetGameAsync("mock llama all day lmao");

            await _commandHandler.InstallCommandsAsync();

            //Setup logging tools
            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, "MzI2NTU1NzI0MjUzNzU3NDQx.XOilUA.lmZcmutI2ynsvTZXjzYWUSDSJgE");
            await _client.StartAsync();

            _client.Ready += LlamaReady;
            _client.MessageReceived += MsgLlama;
            
            await Task.Delay(-1);
        }


        // Logging tools
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        // Notify everyone that I'm ready to roll
        private async Task LlamaReady()
        {
            SocketTextChannel initChannel = (SocketTextChannel)_client.GetChannel(500863591977713674);
            await initChannel.SendMessageAsync("Let's start shamin llama");
        }

        private async Task MsgLlama(SocketMessage messageParam)
        {
            await Task.Delay(TimeSpan.FromHours(2));

            Random _rand = new Random();

            int randMin = 0;
            int randMax = numFacts - 1;
            int randNum = _rand.Next(randMin, randMax);
            
            string msg = llamaFacts.ElementAt(randNum);

            await _client.GetUser(151055150054768641).SendMessageAsync(msg);

            //llama gae
        }

        private void SetupLlamaFacts()
        {
            ////// LLAMA FACT ARRAY SETUP /////

            fSettings.llamaWhy = false;

            // Count the lines
            using (StreamReader sr = File.OpenText(filepathFacts))
            {
                string str;
                while ((str = sr.ReadLine()) != null)
                {
                    numFacts++;
                }
            }

            llamaFacts = new List<string>(numFacts);

            // Populate the array
            using (StreamReader sr = File.OpenText(filepathFacts))
            {
                string str;
                while ((str = sr.ReadLine()) != null)
                {
                    llamaFacts.Insert(lineCountFacts, str);
                    lineCountFacts++;
                }
            }

            ////// LLAMA KEYWORDS AND RESPONSES ARRAYS SETUP /////
            /* Expected file format has the keywords on every even line
             *  and the responses on every odd line (in paired order)
             */
            using (StreamReader sr = File.OpenText(filepathResponses))
            {
                llamaKeywords = new List<string>();
                llamaResponses = new List<string>();

                string str;
                bool even;                
                while ((str = sr.ReadLine()) != null)
                {
                    even = lineCountResponses % 2 == 0;
                    if (even)
                        llamaKeywords.Add(str);
                    else
                        llamaResponses.Add(str);
                    lineCountResponses++;
                }
            }
        }
    }

    // Manages the Command Tasks and setup
    class CommandHandler
    {
        //Context data for the commands
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private FactSettings _fSettings;
        private List<string> _llamaFacts;
        private int _numFacts;
        private List<string> _llamaResponses;
        private List<string> _llamaKeywords;
        
        // CommandHandler constructor
        public CommandHandler(DiscordSocketClient client, CommandService commands, ref FactSettings fSettings,
            ref List<string> llamaFacts, ref int numFacts, ref List<string> llamaKeywords, ref List<string>  llamaResponses)
        {
            _client = client;
            _commands = commands;
            _fSettings = fSettings;
            _llamaFacts = llamaFacts;
            _numFacts = numFacts;
            _llamaKeywords = llamaKeywords;
            _llamaResponses = llamaResponses;
        }

        public async Task InstallCommandsAsync()
        {
            // Handle commands
            _client.MessageReceived += HandleCommandAsync;

            // Handle other keyword inputs from UserLlama
            _client.MessageReceived += CheckForLlamaKeyword;

            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);
        }

        // Handle a command (when one is found)
        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;
            string _prefix = "llama ";
            // validate command request
            if (!(message.HasStringPrefix(_prefix, ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;
            
            // generate context for the command
            var context = new LlamaSocketCommandContext(_client, message, ref _fSettings, ref _llamaFacts, ref _numFacts);

            // call the command with the given context
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);
        }

        private async Task CheckForLlamaKeyword(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            if (message.Author.Id != 151055150054768641 || message.Author.IsBot)
                return;

            string[] keywords = _llamaKeywords.ToArray();
            string[] responses = _llamaResponses.ToArray();
            
            for (int i = 0; i < keywords.Length; i++)
            {
                if (message.Content.ToLower().Contains(keywords[i].ToLower()))
                {
                    await message.Channel.SendMessageAsync($"{message.Author.Mention} { responses[i]}");
                }
            }

        }
    }

    // Class inheritance to send the extra info through
    public class LlamaSocketCommandContext : SocketCommandContext
    {
        public FactSettings _fSettings;
        public List<string> _llamaFacts;
        public int _numFacts;

        public LlamaSocketCommandContext(DiscordSocketClient client, SocketUserMessage msg, ref FactSettings fSettings,
            ref List<string> llamaFacts, ref int numFacts) : base(client, msg)
        {
            _fSettings = fSettings;
            _llamaFacts = llamaFacts;
            _numFacts = numFacts;
        }
    }

    //Command Modules
    [Group("fact")]
    public class LlamaFactModule : ModuleBase<LlamaSocketCommandContext>
    {
        private List<string> llamaFacts;

        // ~say hello world -> hello world
        [Command("")]
        [Summary("Echoes a message.")]
        public async Task FactAsync()
        {
            string msg = "";
            SocketGuildUser userLlama = Context.Guild.CurrentUser;
            
            BasicFactGetter(ref msg, ref userLlama);

            if (userLlama != null)
                await userLlama.SendMessageAsync(msg);
            await Context.Channel.SendMessageAsync($"{Context.User.Mention} {msg}");
        }

        [Command("gae")]
        [Summary("Echoes a message.")]
        public async Task FactGaeAsync()
        {
            string msg = "";
            SocketGuildUser userLlama = Context.Guild.CurrentUser;

            BasicFactGetter(ref msg, ref userLlama);

            char[] dumbMsg = new char[msg.Length];
            string msgUpper = msg.ToUpper();
            string msgLower = msg.ToLower();
            for (int i = 0; i < msg.Length; i++)
            {
                bool isEven = i % 2 != 0;

                if (isEven)
                {
                    char _temp = msgUpper.ElementAt(i);
                    dumbMsg[i] = _temp;
                }
                else
                {
                    char _temp = msgLower.ElementAt(i);
                    dumbMsg[i] = _temp;
                }
                //Console.WriteLine(_temp);
            }

            msg = new string(dumbMsg);

            if (userLlama != null)
                await userLlama.SendMessageAsync(msg);

            await Context.Channel.SendMessageAsync($"{Context.User.Mention} {msg}");

            return;
        }
        
        private const string filepath = @"biggae.txt";
        [Command("add")]
        [Summary("adds a llama fact to the current llama facts instance")]
        public async Task AddLlamaFact(string msg)
        {
            Context._llamaFacts.Insert(Context._numFacts-1, msg);

            await Context.Channel.SendMessageAsync($"Added {msg} succesfully");
        }

        private void BasicFactGetter(ref string msg, ref SocketGuildUser userLlama)
        {
            llamaFacts = Context._llamaFacts;

            Random _rand = new Random();
            int randMin = 0;
            int randMax = Context._numFacts - 1;
            int randNum = _rand.Next(randMin, randMax);

            SocketGuild _guild = Context.Guild;

            IReadOnlyCollection<SocketGuildUser> _users = _guild.Users;

            ulong usrId = 151055150054768641;
            ///////////// UserIDGrabber Usage Eg /////////////
            // string usrTag = "llama#0145";
            // UserIdGetter(ref usrId, usrTag, ref _users);

            msg = llamaFacts.ElementAt(randNum);

            userLlama = Context.Guild.GetUser(usrId);
        }

        /*User ID grabber function:
         llama's ID: 151055150054768641 
        */
        private void UserIdGetter(ref ulong usrId, string usrTag, ref IReadOnlyCollection<SocketGuildUser> _users)
        {
            for (int i = 0; i < _users.Count; i++)
            {
                if (_users.ElementAt(i).ToString() == usrTag)
                    usrId = _users.ElementAt(i).Id;
            }

            if (usrId == 0)
                Console.WriteLine("ERROR: No ID found");

            Console.WriteLine($"{usrId}");
        }
    }


    public class SpamModule : ModuleBase<LlamaSocketCommandContext>
    {
        private List<string> llamaFacts;
        private int numFacts;

        [Command("fact spam")]
        [Summary("Spams fact at your dms, winky face.")]
        public async Task SpamAsync()
        {
            llamaFacts = Context._llamaFacts;
            numFacts = Context._numFacts;

            for (int i = 0; i < numFacts; i++)
            {
                await Context.User.SendMessageAsync(llamaFacts.ElementAt(i));
            }
        }

    }

    [Group("why")]
    public class LlamaWhyModule : ModuleBase<LlamaSocketCommandContext>
    {
        [Command("set")]
        [Summary("Why would you ever want this?")]
        public async Task LlamaSetWhyAsync(bool _set)
        {
            Context._fSettings.llamaWhy = _set;
            await Context.Channel.SendMessageAsync($"Set 'LLAMA WHY' to {_set}");
        }
    }

    [Group("make me a")]
    public class SandwichModule : ModuleBase<LlamaSocketCommandContext>
    {
        private const string goodSammyText = "tasty, fresh sandwich!";
        private const string badSammyText = "shitty, sloppy sandwich..";

        private const string goodSammyFile = @"good sammy.jpg";
        private const string badSammyFile = @"bad sammy.jpg";

        [Command(" sandwich")]
        [Summary("Makes a sandwich")]
        public async Task MakeSandwichAsync()
        {
            if (Context.User.Id == 151055150054768641)
            {
                await Context.Channel.SendMessageAsync("Llama you hekin dum-dum, get back in the kitchen and make someone else a sandwich.");
                await Context.Channel.SendFileAsync("dummo.png");
                return;
            }

            Random _rand = new Random();
            int randBool = _rand.Next(0, 2);

            string sammyText = "";
            string sammyFile = "";
            switch(randBool)
            {
                case 0 :
                    sammyText = goodSammyText;
                    sammyFile = goodSammyFile;
                    break;
                case 1 :
                    sammyText = badSammyText;
                    sammyFile = badSammyFile;
                    break;
            }

            await Context.User.SendFileAsync(sammyFile);

            await Context.Channel.SendMessageAsync( $"You got a {sammyText}");
            await Context.Channel.SendFileAsync(sammyFile);
        }

        private static string uri = "http://www.google.com/";
        [Command("")]
        [Summary("Makes whatever the user wants")]
        public async Task MakeItemAsync(string item)
        {
            string response = "";
            GoogleSearch gSearch = new GoogleSearch("007509064276126836146:56wy_xocjvk", "AIzaSyApvwKmpMqbImEEkQdLE7uPe5LdAxOrV9Q");
            List<GoogleSearchResult> results = gSearch.Search(item);
            response = results.Last().ToString();
            await Context.User.SendMessageAsync(response);
        }
    }

    // TEMPLATE MODULE cuz im too lazy to write it out each time
    [Group("join")]
    public class SampleModule : ModuleBase<SocketCommandContext>
    {
        [Command("")]
        [Summary("Joins the user's vc")]
        public async Task JoinMyVCAsync([Summary("A variable if you need one (optional)")] int num)
        {
        }
    }
}