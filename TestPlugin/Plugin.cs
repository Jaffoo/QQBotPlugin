using PluginBase;
using UnifyBot.Receiver.EventReceiver;
using UnifyBot.Receiver.MessageReceiver;

namespace TestPlugin;

public class Plugin : IPluginBase
{
    public override string Name { get; set; } = "TestPlugin";
    public override string Desc { get; set; } = "测试用";
    public override string Version { get; set; } = "0.0.1";
    public override string Useage { get; set; } = "";

    public override Task Excute(MessageReceiver? mr = null, EventReceiver? eb = null, string unKnow = "")
    {
        Console.WriteLine("插件执行成功");
        return Task.CompletedTask;
    }
}
