using UnifyBot.Model;
using UnifyBot.Receiver.EventReceiver;
using UnifyBot.Receiver.MessageReceiver;
using FluentScheduler;
using SqlSugar;
using UnifyBot.Message.Chain;
using UnifyBot;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace IPluginBase
{

    /// <summary>
    /// 插件父类
    /// </summary>
    public abstract class PluginBase : IDisposable
    {
        private bool disposedValue;

        /// <summary>
        /// 插件id
        /// </summary>
        public int PluginId { get; set; }
        /// <summary>
        /// 插件名称(请使用英文)
        /// </summary>
        public abstract string Name { get; set; }

        /// <summary>
        /// 插件描述
        /// </summary>
        public abstract string Desc { get; set; }

        /// <summary>
        /// 版本
        /// </summary>
        public abstract string Version { get; set; }

        /// <summary>
        /// 使用方法
        /// </summary>
        public abstract string Useage { get; set; }

        private string _jobName = "";
        public string JobName => _jobName;

        private string? _confPath = null;

        private ConnectionConfig? _config { get; set; }
        /// <summary>
        /// 数据库上下文
        /// </summary>
        private SqlSugarClient Db
        {
            get
            {
                if (_config == null) throw new Exception("连接数据库失败");
                return new SqlSugarClient(_config);
            }
        }

        /// <summary>
        /// 获取插件所有配置
        /// </summary>
        /// <returns></returns>
        public async Task<List<ConfigBT>> GetConfig()
        {
            var data = await Db.Queryable<ConfigBT>().Where(x => x.PluginId == PluginId).ToListAsync();
            return data;
        }

        /// <summary>
        /// 根据key获取插件配置
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public async Task<string> GetConfig(string key)
        {
            var data = await Db.Queryable<ConfigBT>().FirstAsync(x => x.PluginId == PluginId && x.Key == key);
            return data?.Value ?? "";
        }

        /// <summary>
        /// 检查插件中键是否存在（同一个插件键不能重复）
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<bool> HasKey(string key)
        {
            return await Db.Queryable<ConfigBT>().AnyAsync(x => x.PluginId == PluginId && x.Key == key);
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public async Task<bool> SaveConfig(int id, string value)
        {
            var res = await Db.Updateable<ConfigBT>().SetColumns(x => x.Value == value).Where(x => x.Id == id).ExecuteCommandAsync();
            return res > 0;
        }

        /// <summary>
        /// 添加或更新配置
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public async Task<bool> SaveConfig(string key, string value)
        {
            if (!await HasKey(key))
            {
                var config = new ConfigBT
                {
                    Key = key,
                    Value = value,
                    PluginId = PluginId
                };
                var res = await Db.Insertable(config).ExecuteCommandAsync();
                return res > 0;
            }
            else
            {
                var res = await Db.Updateable<ConfigBT>().SetColumns(x => x.Value == value).Where(x => x.PluginId == PluginId && x.Key == key).ExecuteCommandAsync();
                return res > 0;
            }
        }

        /// <summary>
        /// 添加或更新配置
        /// </summary>
        /// <param name="config">配置</param>
        /// <returns></returns>
        public async Task<bool> SaveConfig(ConfigBT config)
        {
            if (!await HasKey(config.Key))
            {
                config.PluginId = PluginId;
                var res = await Db.Insertable(config).ExecuteCommandAsync();
                return res > 0;
            }
            else
            {
                var res = await Db.Updateable(config).ExecuteCommandAsync();
                return res > 0;
            }
        }

        /// <summary>
        /// 移除插件所有配置
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RemoveConfig()
        {
            return await Db.Deleteable<ConfigBT>(GetConfig()).ExecuteCommandAsync() > 0;
        }

        /// <summary>
        /// 根据key移除配置
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<bool> RemoveConfig(string key)
        {
            return await Db.Deleteable<ConfigBT>(await GetConfig(key)).ExecuteCommandAsync() > 0;
        }

        /// <summary>
        /// 配置文件路径
        /// </summary>
        public virtual string ConfPath
        {
            get
            {
                if (_confPath != null) return _confPath;
                var dir = Path.Combine(Environment.CurrentDirectory, $"plugins/conf/{Name}/");
                return dir;
            }
            set { _confPath = value; }
        }

        private string? _logPath = null;
        /// <summary>
        /// 日志
        /// </summary>
        public virtual string LogPath
        {
            get
            {
                if (_logPath != null) return _logPath;
                var dir = Path.Combine(Environment.CurrentDirectory, $"plugins/logs/{Name}");
                return dir;
            }
            set { _logPath = value; }
        }

        /// <summary>
        /// 定时任务
        /// 示例：SetTimer(() => Method(), x => x.ToRunNow().AndEvery(1).Minutes());
        /// </summary>
        /// <returns></returns>
        public void SetTimer(string jobName, Action job, Action<Schedule> schedule)
        {
            _jobName = jobName;
            if (JobManager.GetSchedule(JobName) == null)
                JobManager.AddJob(job, schedule);
        }

        /// <summary>
        /// 执行插件
        /// </summary>
        /// <returns></returns>
        public virtual async Task Excute(MessageReceiver? mr = null, EventReceiver? eb = null, string unKnow = "")
        {
            try
            {
                if (mr != null)
                {
                    if (mr.MessageType == MessageType.Private)
                        await FriendMessage((PrivateReceiver)mr!);
                    if (mr.MessageType == MessageType.Group)
                        await GroupMessage((GroupReceiver)mr!);
                }
                if (eb != null)
                {
                    await EventMessage(eb);
                }
                if (!string.IsNullOrWhiteSpace(unKnow))
                {
                    await UnKnowMessage(unKnow);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 群消息
        /// </summary>
        /// <param name="gmr"></param>
        /// <returns></returns>
        public virtual async Task GroupMessage(GroupReceiver gmr)
        {
            await Task.Delay(1);
            return;
        }

        /// <summary>
        /// 好友消息
        /// </summary>
        /// <param name="fmr"></param>
        /// <returns></returns>
        public virtual async Task FriendMessage(PrivateReceiver fmr)
        {
            await Task.Delay(1);
            return;
        }

        /// <summary>
        /// 事件消息
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public virtual async Task EventMessage(EventReceiver e)
        {
            await Task.Delay(1);
            return;
        }

        /// <summary>
        /// 未知消息
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public virtual async Task UnKnowMessage(string unKnow)
        {
            await Task.Delay(1);
            return;
        }

        private Bot? _bot { get; set; }

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        /// <param name="qq"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<long> SendPrivateMsg(long qq, string msg)
        {
            if (_bot == null) throw new MissingFieldException("如果使用了qq机器人，请在加载插件的方法中传入机器人对象");
            return await _bot.SendPrivateMessage(qq, msg);
        }

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        /// <param name="qq"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<long> SendPrivateMsg(long qq, MessageChain msg)
        {
            if (_bot == null) throw new MissingFieldException("如果使用了qq机器人，请在加载插件的方法中传入机器人对象");
            return await _bot!.SendPrivateMessage(qq, msg);
        }

        /// <summary>
        /// 发送群消息
        /// </summary>
        /// <param name="qq"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<long> SendGroupMsg(long qq, string msg)
        {
            if (_bot == null) throw new MissingFieldException("如果使用了qq机器人，请在加载插件的方法中传入机器人对象");
            return await _bot!.SendGroupMessage(qq, msg);
        }

        /// <summary>
        /// 发送群消息
        /// </summary>
        /// <param name="qq"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<long> SendGroupMsg(long qq, MessageChain msg)
        {
            if (_bot == null) throw new MissingFieldException("如果使用了qq机器人，请在加载插件的方法中传入机器人对象");
            return await _bot!.SendGroupMessage(qq, msg);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~BasePlugin()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}