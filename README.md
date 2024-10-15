## 机器人插件功能
onebot11标准，使用简单，快捷

### 使用方法

安装：自行打包

PluginRegister.Instance.InitPlugin(db,bot) //db是sqlsugar的数据库上下文，bot是unifybot的对象；存在重载，自行查看；必须首先调用

PluginRegister.Instance.FriendControlPlugin() //好友通过命令控制插件，可以传入好友qq数组，传入后，只能这些人操作

PluginRegister.Instance.GroupControlPlugin() //好友通过命令控制插件，可以传入群qq数组和群友qq数组，传入后，只能这些群操作，这些人操作

//当然，你也可以自己实现机器人操作命令

//命令有：加载插件，插件列表，搜索插件{关键字}，启用插件{插件id}，禁用插件{插件id}，删除插件{插件id}，插件使用{插件id}，定时任务{任务名}，禁用任务，启用任务{任务名}

### 插件注册类属性和方法说明
- Instance-插件注册器为单例模式，调用此属性自动实例化类
- Plugins-插件表的数据
- InitPlugin-初始化插件，db参数为sqlsuger对象；Bot为UnifyBot对象；conn为Sqlsugar配置对象；rewriteTable-表是否需要重写，如果默认false，会在此类中自动创建和更新必须的表Entity.ConfigBT和Entity.PluginBT，传入true，则不会自动创建和更新，需要自己实现，如果需要加字段需要自己新加实体类并继承自Entity.ConfigBT和Entity.PluginBT(已经在基类中定义了表名没有BT，新建的类请不要重复定义表名)
- FriendControlPlugin-好友通过命令控制插件，可以传入好友qq数组，传入后，只能这些人操作
- GroupControlPlugin-群通过命令控制插件，可以传入群qq数组和群友qq数组，传入后，只能这些群操作，这些人操作


### 插件基类属性方法说明
- Name-插件名
- Version-版本
- Desc-介绍
- Useage-使用方法
- GetConfig-获取插件所有配置，存在重载
- HasKey-检查插件中键是否存在（同一个插件键不能重复）
- SaveConfig-更新配置，传入配置id和值是更新；传入键和值，键存在则更新，否则新增
- RemoveConfig-移除插件所有配置
- ConfPath-配置文件路径
- LogPath-日志文件路径
- SetTimer-定时任务，示例：SetTimer(() => Method(), x => x.ToRunNow().AndEvery(1).Minutes());
- Excute-执行插件
- GroupMessage-接收群消息
- FriendMessage-接收好友消息
- EventMessage-接收事件消息
- UnKnowMessage-接收未知消息
- SendPrivateMsg-发送好友消息，存在重载
- SendGroupMsg-发送群消息，存在重载

### 写自己的插件
- 插件需要继承BasePlugin抽象类
- 必须重写属性Name-插件名，Version-版本，Desc-介绍，Useage-使用方法
- 如果需要接收消息，请重写对应的消息接收的方法，无特殊需求Excute可不用重写
- 添加定时任务，调用SetTimer，使用的是FluentScheduler库，具体使用方法自行查询

### 发布插件
写好后，直接生成，找到对应的dll，放到插件目录（程序根目录/plugins）文件夹中，
然后使用命令加载插件重新加载，或者重写你的程序，或者等待（插件自动注册了一个重新加载插件的定时任务，每10分钟执行一次）