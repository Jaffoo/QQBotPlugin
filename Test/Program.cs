using PluginBase;
using UnifyBot.Model;

namespace Test;

internal class Program
{
   async static Task Main(string[] args)
    {
        var conn = new Connect("localhost", 3001, 3000);
        UnifyBot.Bot bot = new(conn);
        await bot.StartAsync();
        Console.WriteLine("QQ机器人服务连接成功");
        PluginRegister.LoadPlugins(bot);
        PluginRegister.FriendControlPlugin();
        while (true)
        {
            Thread.Sleep(1);
        }
    }
}
