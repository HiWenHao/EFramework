![image](https://github.com/HiWenHao/EFramework/blob/master/View.png)

# EasyFramework
***EasyFramework*** 是一套用于Unity3D的项目控制系统，用于帮助研发团队快速部署和交付游戏。    
目前一切都在完善中，有什么问题可以加群讨论，或者帮小黑来一起完善。技术不强，还在进步中。

## 版本   
Unity 2020.3.33f1   
Microsoft Visual Studio Professional 2022    
目前在高版本中有应用到，没大问题，旧版本还没测试或者应用过。   

## 框架使用集合地

- [点击链接加入群聊【EF】](https://jq.qq.com/?_wv=1027&k=4GvMJd6w)

## Xmind文件
[EasyFramework.xmind](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Other/EF.xmind)
统计了项目整体框架内容，随着框架再慢慢完善的同时，该文件也会被同时更新。

## 系统特点
- ***让一切变得简单***  尽可能的满足一切大众需求，让功能模块的开启变得更简单。    
- ***[GameEditorConfig](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Editor/GameEditorConfig.cs)***   
  项目编辑器配置，项目创建后先设置这里，接着在版本管理工具中设置提交忽略。    
  作用：在创建脚本时，附带着作者信息 + 脚本版号,其中***FrameworkPath***一定要配置正确，以确保框架的正常使用。     
  
- ***[AppConst](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Scripts/AppConst.cs)***    
  项目配置文件，包括了项目名称、版本号、前缀；    
  所有加载的资源路径，也都在这里配置。   
  
- ***[GameManagerController](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Scripts/GameManagerController.cs)***，这是项目的开始    

## 各个管理器
### Mono
- **[UIManager](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Scripts/Managers/Mono/UIManager.cs)**    
  提供Push进入、Pop离开等接口。   
  创建新UI，相关脚本须继承**UIPageBase**，然后在**Resources**下保存与新UI类名一样名字的Prefab，路径需要与***AppConst***下配置的一样.    
  
- **[TimeManager](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Scripts/Managers/Mono/TimeManager.cs)**    
  提供Add和Remove接口，和SetTimeScale设置时间速率的接口。
  
- **[LoadManager](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Scripts/Managers/Mono/LoadManager.cs)**   
  目前只是简单的对于Resources.Load进行了套壳应用，后面会逐渐完善。      
  
- **[ScenesManager](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Scripts/Managers/Mono/ScenesManager.cs)**   
  提供***LoadSceneWithName***与***LoadSceneWithNameNow***接口，暂时只支持输入***string***类型的场景名。   
  
- **[SourceManager](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Scripts/Managers/Mono/SourceManager.cs)**   
  提供***Play、Mute、Pause***等等一系列相关接口
  
#### 和网络相关的    
- **[HttpsManager](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Scripts/Managers/Mono/HttpsManager.cs)**   
  基于***BestHttp***库的***Get***和***Post***相关函数   
  
- **[SocketManager](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Scripts/Managers/Mono/SocketManager.cs)**   
  基于***BestHttp***库的***OnOpen、OnMessageReceived、OnOnBinaryReceived、OnClosed、OnError***函数   
  
   
 ### Other   
- **[ToolManager](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Scripts/Managers/Other/ToolManager.cs)**    
  尽可能的提供各类工具、例如：递归查找、字节转音频、控制手机屏幕旋转等。   
  
- **[GameObjectPoolManager](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Scripts/Managers/Other/GameObjectPoolManager.cs)**   
  提供***CreateTPool、Get、Recycle、ReleasePool***等等相关接口   
  
- **[FolderManager](https://github.com/HiWenHao/EFramework/blob/master/Assets/EasyFramework/Scripts/Managers/Other/FolderManager.cs)**   
  提供***Creat、Delete、ComparisonMD5***和其余接口   
   
   
---   
## 优秀的第三方插件
  ### YooAsset
  - **异步加载** 支持协程，Task，委托等多种异步加载方式。
  - **同步加载** 支持同步加载和异步加载混合使用。
  - **边玩边下载** 在加载资源对象的时候，如果资源对象依赖的资源包在本地不存在，会自动从服务器下载到本地，然后再加载资源对象。
  - **多线程下载** 支持断点续传，自动验证下载文件，自动修复损坏文件。
  - **多功能下载器** 可以按照资源分类标签创建下载器，也可以按照资源对象创建下载器。可以设置同时下载文件数的限制，设置下载失败重试次数，设置下载超时判定时间。多个下载器同时下载不用担心文件重复下载问题，下载器还提供了下载进度以及下载失败等常用接口。


  ### PlayerPrefsEditor
  - **更快的搜索** 搜索不区分大小写
  - **优秀的增删** 更优秀的增删设计。
  - **加密和解密** 增加加密设，更可以获取到加密数据并自动解密。
  - **有好的导入** 可以根据公司名加项目名、快速的导入另一个项目的持久化数据。
   
   
   ### Best HTTP (Pro)
   - Best HTTP是基于***RFC 2616***的***HTTP/1.1***实现，它支持几乎所有的***Unity***所支持的平台   
   - 其目标是创建一个易于使用，但仍然强大的Unity插件。  【我很喜欢这点，这和EF的初衷是一样的】
   
   
## 推荐仓库

**[YooAsset](https://github.com/tuyoogame/YooAsset)**   
  YooAsset是一套用于Unity3D的资源管理系统，用于帮助研发团队快速部署和交付游戏。   
  它可以满足商业化游戏的各类需求，并且经历多款百万DAU游戏产品的验证   
   
**[HybridCLR](https://github.com/focus-creative-games/hybridclr)**   
    一个特性完整、零成本、高性能、低内存的近乎完美的Unity全平台原生c#热更方案   
   
**[Deer_GameFramework_Wolong](https://github.com/It-Life/Deer_GameFramework_Wolong)**   
    基于GameFramework框架衍生的一个wolong热更框架（HybridCLR(wolong)），实现除GameFramework库底层代码以及更新流程逻辑层代码，其余流程及义务层代码全部热更。
