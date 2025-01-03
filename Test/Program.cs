using IPluginBase;
using SqlSugar;
using System.Reflection;
using UnifyBot.Model;

namespace Test;

internal class Program
{
    async static Task Main(string[] args)
    {
        var conn = new Connect("127.0.0.1", 3001, 3000,"523366");
        UnifyBot.Bot bot = new(conn);
        await bot.StartAsync();
        Console.WriteLine("QQ机器人服务连接成功");
        PluginRegister.Instance.InitPlugin(InitDb(), bot);
        PluginRegister.Instance.FriendControlPlugin();
        while (true)
        {
            Thread.Sleep(1);
        }
    }

    static SqlSugarClient InitDb()
    {
        var master = "Data Source=data/main.db";
        ConnectionConfig config = new()
        {
            ConnectionString = master,
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true,
            ConfigureExternalServices = new()
            {
                //注意:  这儿AOP设置不能少
                EntityService = (c, p) =>
                {
                    if (p.IsPrimarykey == false && new NullabilityInfoContext()
                     .Create(c).WriteState is NullabilityState.Nullable)
                    {
                        p.IsNullable = true;
                    }
                }
            }
        };
        SqlSugarClient sqlSugarClient = new(config);
        if (!Directory.Exists("data")) Directory.CreateDirectory("data");
        sqlSugarClient.DbMaintenance.CreateDatabase();
        sqlSugarClient.CodeFirst.InitTables<ConfigBT>();
        sqlSugarClient.CodeFirst.InitTables<PluginBT>();
        return sqlSugarClient;
    }
}
