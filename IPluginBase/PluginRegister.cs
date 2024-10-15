using ConsoleTables;
using FluentScheduler;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TBC.CommonLib;
using UnifyBot;
using UnifyBot.Receiver.EventReceiver;
using UnifyBot.Receiver.MessageReceiver;

namespace IPluginBase
{

    /// <summary>
    /// 插件注册
    /// </summary>
    /// <typeparam name="T">插件表</typeparam>
    public class PluginRegister
    {
        private PluginRegister() { }
        private static PluginRegister? _instance;
        private static readonly object _lock = new object();
        public static PluginRegister Instance
        {
            get
            {
                if (_instance == null)
                    lock (_lock)
                        _instance ??= new PluginRegister();
                return _instance;
            }
        }

        private ConnectionConfig? _connConfig = null;
        private SqlSugarClient Db
        {
            get
            {
                CheckInit();
                return new SqlSugarClient(_connConfig);
            }
        }
        private readonly Dictionary<PluginBT, PluginBase> LoadedPlugins = new Dictionary<PluginBT, PluginBase>();
        private Bot? _bot;

        public List<PluginBT> Plugins
        {
            get
            {
                CheckInit();
                return Db.Queryable<PluginBT>().ToList();
            }
        }

        private void InitTable()
        {
            Db.CodeFirst.InitTables<ConfigBT>();
            Db.CodeFirst.InitTables<PluginBT>();
        }

        private void CheckInit()
        {
            if (_connConfig == null) throw new Exception("请先调用InitPlugin方法");
        }

        public void InitPlugin(SqlSugarClient db, bool rewriteTable = false)
        {
            _connConfig = db.CurrentConnectionConfig.DeepClone();
            AutoLoadPlugin();
            LoadPlugins();
            if (!rewriteTable) InitTable();
        }
        public void InitPlugin(SqlSugarClient db, Bot bot, bool rewriteTable = false)
        {
            _bot = bot;
            _connConfig = db.CurrentConnectionConfig.DeepClone();
            AutoLoadPlugin();
            ExcutePlugin(bot);
            LoadPlugins();
            if (!rewriteTable) InitTable();
        }

        public void InitPlugin(ConnectionConfig conn, bool rewriteTable = false)
        {
            _connConfig = conn.DeepClone();
            AutoLoadPlugin();
            LoadPlugins();
            if (!rewriteTable) InitTable();
        }
        public void InitPlugin(ConnectionConfig conn, Bot bot, bool rewriteTable = false)
        {
            _bot = bot;
            _connConfig = conn.DeepClone();
            AutoLoadPlugin();
            ExcutePlugin(bot);
            LoadPlugins();
            if (!rewriteTable) InitTable();
        }

        /// <summary>
        /// 加载插件
        /// </summary>
        private void LoadPlugins()
        {
            if (!Directory.Exists("plugins")) Directory.CreateDirectory("plugins");
            var files = new DirectoryInfo("plugins").GetFiles();
            foreach (var item in files)
            {
                if (item.Extension != ".dll") continue;
                byte[] buffurs = File.ReadAllBytes(item.FullName);
                Assembly assembly = Assembly.Load(buffurs);
                // 获取 DLL 中的类型
                Type[] types = assembly.GetTypes();
                if (types == null) continue;
                var type = types.FirstOrDefault(x => !x.Name.Contains("<"));
                if (type == null) continue;
                var instanceObj = Activator.CreateInstance(type);
                // 获取私有属性的 PropertyInfo
                var pFields = typeof(PluginBase).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
                // 检查属性信息是否为 null
                if (pFields != null)
                {
                    if (_bot != null)
                    {
                        var pBot = pFields.FirstOrDefault(x => x.Name.Contains("_bot"));
                        pBot?.SetValue(instanceObj, _bot);
                    }
                    var pConn = pFields.FirstOrDefault(x => x.Name.Contains("_config"));
                    pConn?.SetValue(instanceObj, _connConfig);
                }
                using PluginBase? instance = instanceObj as PluginBase;
                if (instance == null) continue;
                PluginBT temp;
                if (!Plugins.Exists(t => t.Name == instance.Name && t.Version == instance.Version))
                {
                    temp = new PluginBT()
                    {
                        Name = instance.Name,
                        Version = instance.Version,
                        Enable = false,
                        Usage = instance.Useage,
                        Desc = instance.Desc
                    };
                    var id = Db.Insertable(temp).ExecuteReturnIdentity();
                    temp.Id = id;
                    instance.PluginId = id;
                }
                else
                {
                    temp = Plugins.FirstOrDefault(x => x.Name == instance.Name && x.Version == instance.Version)!;
                    instance.PluginId = temp.Id;
                }
                if (!temp.Enable)
                {
                    JobManager.GetSchedule(instance.JobName)?.Disable();
                }
                if (!LoadedPlugins.Any(x => x.Key.Name == instance.Name && x.Key.Version == instance.Version))
                    LoadedPlugins.Add(temp, instance);
            }
        }

        /// <summary>
        /// 启用通过命令操作插件（也可以自己实现）
        /// 默认全部好友，如果传参qqs，则是特定人能使用
        /// </summary>
        /// <param name="qqs"></param>
        public void FriendControlPlugin(List<long>? qqs = null)
        {
            if (_bot == null) throw new MissingFieldException("如果使用了qq机器人，请在加载插件的方法中传入机器人对象");
            _bot.MessageReceived.OfType<PrivateReceiver>().Subscribe(async x =>
            {
                var text = x.Message.GetPlainText();
                if (text.IsNullOrWhiteSpace()) return;
                if (qqs != null && !qqs.Contains(x.SenderQQ)) return;
                var msg = Handle(text);
                if (msg.IsNullOrWhiteSpace()) return;
                await x.SendMessage(msg);
            });
        }

        /// <summary>
        /// 启用通过命令操作插件（也可以自己实现）
        /// 默认全部群和全部群员，如果传参groups，和qqs，则是特定群的特定人能使用
        /// </summary>
        /// <param name="groups"></param>
        public void GroupControlPlugin(List<long>? groups = null, List<long>? qqs = null)
        {
            if (_bot == null) throw new MissingFieldException("如果使用了qq机器人，请在加载插件的方法中传入机器人对象");
            _bot.MessageReceived.OfType<GroupReceiver>().Subscribe(async x =>
            {
                var text = x.Message.GetPlainText();
                if (text.IsNullOrWhiteSpace()) return;
                if (groups != null && !groups.Contains(x.GroupQQ)) return;
                if (qqs != null && !qqs.Contains(x.SenderQQ)) return;
                var msg = Handle(text);
                if (msg.IsNullOrWhiteSpace()) return;
                await x.SendMessage(msg);
            });
        }

        /// <summary>
        /// 重载插件
        /// </summary>
        public void ReLoadPlugins()
        {
            CheckInit();
            Plugins.Clear();
            LoadedPlugins.Clear();
            LoadPlugins();
        }

        /// <summary>
        /// 禁用插件
        /// </summary>
        public void StopPlugin(long id)
        {
            CheckInit();
            var plugin = Plugins.FirstOrDefault(t => t.Id == id) ?? throw new Exception("插件不存在");
            plugin.Enable = false;
            var b = Db.Updateable(plugin).ExecuteCommand() > 0;
            if (b)
            {
                var load = LoadedPlugins.FirstOrDefault(x => x.Key.Id == id);
                load.Key.Enable = false;
                if (!load.Value.JobName.IsNullOrWhiteSpace())
                    JobManager.GetSchedule(load.Value.JobName).Disable();
            }
        }

        /// <summary>
        /// 启用插件
        /// </summary>
        public void StartPlugin(long id)
        {
            CheckInit();
            var plugin = Plugins.FirstOrDefault(t => t.Id == id) ?? throw new Exception("插件不存在");
            var list = Plugins.Where(x => x.Name == plugin.Name).ToList();
            list.ForEach(x =>
            {
                if (x.Id == id) x.Enable = true;
                else x.Enable = false;
            });

            var b = Db.Updateable(list).ExecuteCommand() > 0;
            if (b)
            {
                var load = LoadedPlugins.FirstOrDefault(x => x.Key.Id == id);
                load.Key.Enable = true;
                if (!load.Value.JobName.IsNullOrWhiteSpace())
                    JobManager.GetSchedule(load.Value.JobName).Enable();
            }
        }

        /// <summary>
        /// 删除插件
        /// </summary>
        public void DelPlugin(long id)
        {
            CheckInit();
            var b = Db.Deleteable<PluginBT>().In(id).ExecuteCommand() > 0;
            if (b)
            {
                Db.Deleteable<ConfigBT>(x => x.PluginId == id).ExecuteCommand();
                var item = LoadedPlugins.FirstOrDefault(x => x.Key.Id == id);
                if (!item.Value.JobName.IsNullOrWhiteSpace())
                    JobManager.RemoveJob(item.Value.JobName);
                LoadedPlugins.Remove(item.Key);
                var file = new DirectoryInfo("plugins").GetFiles().FirstOrDefault(x => x.Name == item.Key.Name + x.Extension);
                file?.Delete();
                DirectoryInfo dir = new DirectoryInfo("plugins/conf/" + item.Key.Name);
                if (dir.Exists)
                    dir.Delete(true);
                //删除配置数据
                Db.Deleteable<ConfigBT>().Where(x => x.PluginId == item.Value.PluginId).ExecuteCommand();
            }
        }

        /// <summary>
        /// 调用插件
        /// </summary>
        private async Task Excute(MessageReceiver? mrb = null, EventReceiver? eb = null, string unKnow = "")
        {
            foreach (var item in LoadedPlugins)
            {
                if (item.Key == null || item.Value == null) continue;
                if (!item.Key.Enable) continue;
                await item.Value.Excute(mrb, eb, unKnow);
            }
        }
        /// <summary>
        /// 启用自动同步插件定时任务
        /// </summary>
        private void AutoLoadPlugin()
        {
            CheckInit();
            JobManager.Initialize();
            JobManager.RemoveAllJobs();
            JobManager.AddJob(() => LoadPlugins(), x => x.WithName("AutoLoadPlugins").ToRunEvery(10).Minutes());
        }

        /// <summary>
        /// 执行插件
        /// </summary>
        /// <param name="bot"></param>
        private void ExcutePlugin(Bot bot)
        {
            bot.MessageReceived.Subscribe(async x => await Excute(x));

            bot.EventReceived.Subscribe(async x => await Excute(null, x));

            bot.UnknownMessageReceived.Subscribe(async x => await Excute(null, null, x));
        }

        /// <summary>
        /// 处理命令
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string Handle(string text)
        {
            if (text == "加载插件")
            {
                ReLoadPlugins();
                return "插件已更新";
            }

            if (text == "插件列表")
            {
                var plist = Plugins.ToList();
                if (plist.Count == 0)
                    return "无插件";
                else
                    return CreatePluginTable(plist);
            }
            if (text.Length > 4 && text[..4] == "搜索插件")
            {
                var key = text[4..];
                var plist = Plugins.Where(x => x.Name.Contains(key)).ToList();
                if (plist != null && plist.Count > 0)
                    return CreatePluginTable(plist);
                else
                    return "未搜索到插件";
            }
            if (text.Length > 4 && text[..4] == "启用插件")
            {
                var id = text[4..].ToInt(0);
                StartPlugin(id);
                return "插件已启用";
            }
            if (text.Length > 4 && text[..4] == "禁用插件")
            {
                var id = text[4..].ToInt(0);
                StopPlugin(id);
                return "插件已禁用";
            }
            if (text.Length > 4 && text[..4] == "删除插件")
            {
                var id = text[4..].ToInt(0);
                DelPlugin(id);
                return "插件已删除";
            }
            if (text.Length > 4 && text[..4] == "插件使用")
            {
                var id = text[4..].ToInt(0);
                var plugin = Plugins.FirstOrDefault(x => x.Id == id);
                return plugin?.Usage ?? "无使用说明";
            }
            if (text == "定时任务")
            {
                var jobs = JobManager.AllSchedules.ToList();
                return CreateTimerTable(jobs);
            }
            if (text.Length > 4 && text[..4] == "禁用任务")
            {
                var jobName = text[4..];
                var job = JobManager.GetSchedule(jobName);
                if (job == null)
                    return "任务[" + jobName + "]不存在";
                else
                {
                    job.Disable();
                    return "任务[" + jobName + "]已禁用";
                }
            }
            if (text.Length > 4 && text[..4] == "启用任务")
            {
                var jobName = text[4..];
                var job = JobManager.GetSchedule(jobName);
                if (job == null)
                    return "任务[" + jobName + "]不存在";
                else
                {
                    job.Enable();
                    return "任务[" + jobName + "]已启用";
                }
            }
            return "";
        }

        private string CreatePluginTable(List<PluginBT> plugins)
        {
            var table = new ConsoleTable("标识", "插件名", "版本", "状态");
            foreach (var plugin in plugins)
            {
                table.AddRow(plugin.Id, plugin.Name, plugin.Version, plugin.Enable ? "启用" : "停用");
            }
            return table.ToString();
        }
        private string CreateTimerTable(List<Schedule> timers)
        {
            var table = new ConsoleTable("名称", "状态", "下次运行");
            foreach (var timer in timers)
            {
                table.AddRow(timer.Name, timer.Disabled ? "禁用" : "启用", timer.NextRun.ToString("yyyy/MM/dd HH:mm:ss"));
            }
            return table.ToString();
        }
    }
}