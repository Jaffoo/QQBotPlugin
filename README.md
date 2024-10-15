## 机器人插件功能
onebot11标准，使用简单，快捷

### 使用方法
安装：自行打包

PluginRegister.Instance.InitPlugin(db,bot) //db是sqlsugar的数据库上下文，bot是unifybot的对象；存在重载，自行查看；必须首先调用

PluginRegister.Instance.FriendControlPlugin() //好友通过命令控制插件，可以传入好友qq数组，传入后，只能这些人操作

PluginRegister.Instance.GroupControlPlugin() //好友通过命令控制插件，可以传入群qq数组和群友qq数组，传入后，只能这些群操作，这些人操作

//当然，你也可以自己实现机器人操作命令

//命令有：加载插件，插件列表，搜索插件{关键字}，启用插件{插件id}，禁用插件{插件id}，删除插件{插件id}，插件使用{插件id}，定时任务{任务名}，禁用任务，启用任务{任务名}
