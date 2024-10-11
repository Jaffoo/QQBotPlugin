using SqlSugar;

namespace PluginBase;

/// <summary>
/// 插件配置类
/// </summary>
public class Config
{
    /// <summary>
    /// id
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 插件id
    /// </summary>
    public long PluginId { get; set; }

    /// <summary>
    /// 键
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// 值
    /// </summary>
    public string Value { get; set; } = "";
}


/// <summary>
/// 插件表
/// </summary>
public class Plugin
{
    /// <summary>
    /// id
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 插件名
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 描述
    /// </summary>
    public string? Desc { get; set; }

    /// <summary>
    /// 使用方法
    /// </summary>
    public string? Usage { get; set; }

    /// <summary>
    /// 版本
    /// </summary>
    public string Version { get; set; } = "0.0.1";

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable { get; set; }
}
