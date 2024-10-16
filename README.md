## 机器人插件功能
基于C#打造的机器人插件注册器，基于onebot11的所有机器人均可以使用，使用简单，快捷

### 使用方法

安装：[nuget包](https://github.com/Jaffoo/QQBotPlugin/tree/master/IPluginBase/bin/Debug)(本地打包好的，未上传到微软)

PluginRegister.Instance.InitPlugin(db,bot) //db是sqlsugar的数据库上下文，bot是unifybot的对象；存在重载，自行查看；必须首先调用

PluginRegister.Instance.FriendControlPlugin() //好友通过命令控制插件，可以传入好友qq数组，传入后，只能这些人操作

PluginRegister.Instance.GroupControlPlugin() //好友通过命令控制插件，可以传入群qq数组和群友qq数组，传入后，只能这些群操作，这些人操作

//当然，你也可以自己实现机器人操作命令

//命令有：加载插件，插件列表，搜索插件{关键字}，启用插件{插件id}，禁用插件{插件id}，删除插件{插件id}，插件使用{插件id}，定时任务{任务名}，禁用任务，启用任务{任务名}

### 插件注册类属性和方法说明
- Instance-插件注册器为单例模式，调用此属性自动实例化类
- Plugins-插件表的数据，如需使用，必须，必须在调用InitPlugin的时候传入db或者conn调用InitPlugin的时候传入db或者conn
- InitPlugin-初始化插件，db参数为sqlsuger对象；Bot为UnifyBot对象；conn为Sqlsugar配置对象；rewriteTable-表是否需要重写，如果默认false，会在此类中自动创建和更新必须的表Entity.ConfigBT和Entity.PluginBT，传入true，则不会自动创建和更新，需要自己实现，如果需要加字段需要自己新加实体类并继承自Entity.ConfigBT和Entity.PluginBT(已经在基类中定义了表名没有BT，新建的类请不要重复定义表名)
- FriendControlPlugin-好友通过命令控制插件，可以传入好友qq数组，传入后，只能这些人操作(如需使用，必须在调用InitPlugin的时候传入bot)
- GroupControlPlugin-群通过命令控制插件，可以传入群qq数组和群友qq数组，传入后，只能这些群操作，这些人操作(如需使用，必须在调用InitPlugin的时候传入bot)


### 插件基类属性方法说明
- Name-插件名
- Version-版本
- Desc-介绍
- Useage-使用方法
- GetConfig-获取插件所有配置，存在重载(如需使用，必须在调用InitPlugin的时候传入db或者conn)
- HasKey-检查插件中键是否存在（同一个插件键不能重复）(如需使用，必须在调用InitPlugin的时候传入db或者conn)
- SaveConfig-更新配置，传入配置id和值是更新；传入键和值，键存在则更新，否则新增(如需使用，必须在调用InitPlugin的时候传入db或者conn)
- RemoveConfig-移除插件所有配置(如需使用，必须在调用InitPlugin的时候传入db或者conn)
- ConfPath-配置文件路径，在不用数据库又需要配置的时候就用这个
- LogPath-日志文件路径
- SetTimer-定时任务，示例：SetTimer(() => Method(), x => x.ToRunNow().AndEvery(1).Minutes());
- Excute-执行插件(如需使用，必须在调用InitPlugin的时候传入bot)
- GroupMessage-接收群消息(如需使用，必须在调用InitPlugin的时候传入bot)
- FriendMessage-接收好友消息(如需使用，必须在调用InitPlugin的时候传入bot)
- EventMessage-接收事件消息(如需使用，必须在调用InitPlugin的时候传入bot)
- UnKnowMessage-接收未知消息(如需使用，必须在调用InitPlugin的时候传入bot)
- SendPrivateMsg-发送好友消息，存在重载(如需使用，必须在调用InitPlugin的时候传入bot)
- SendGroupMsg-发送群消息，存在重载(如需使用，必须在调用InitPlugin的时候传入bot)

### 写自己的插件
- 插件需要继承BasePlugin抽象类
- 必须重写属性Name-插件名，Version-版本，Desc-介绍，Useage-使用方法
- 如果需要接收消息，请重写对应的消息接收的方法，无特殊需求Excute可不用重写
- 添加定时任务，调用SetTimer，使用的是FluentScheduler库，具体使用方法自行查询

### 发布插件
写好后，直接生成，找到对应的dll，放到插件目录（程序根目录/plugins）文件夹中，
然后使用命令【加载插件】重新加载，或者重启你的程序，或者等待（插件自动注册了一个重新加载插件的定时任务，每10分钟执行一次）
