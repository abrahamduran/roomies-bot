using Flypack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RoomiesBot.Services;
using FlypackSettings = RoomiesBot.Settings.Flypack;
using TelegramSettings = RoomiesBot.Settings.Telegram;

namespace RoomiesBot
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((ctx, services) =>
                {
                    services.Configure<TelegramSettings>(ctx.Configuration.GetSection("Telegram"));
                    services.Configure<FlypackSettings>(ctx.Configuration.GetSection("Flypack"));
                    services.AddScoped<FlypackScrapper>();
                    services.AddScoped<FlypackService>();
                    services.AddHostedService<RoomiesBotWorker>();
                });
    }
}
