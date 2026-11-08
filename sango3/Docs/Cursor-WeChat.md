# 公司电脑上的微信小游戏转换任务

基线为 Sango3 0.28.0，Windows / Unity 2022.3.62f3c1。交付目标是在三星微信内跑通白马关，并准备可上传的开发版本。下文可整段交给 Cursor。

## 给 Cursor 的任务

请在公司电脑上完成 Unity 到微信小游戏的转换。直接检查本机、导入 SDK、导出、修复兼容问题并验证结果。把完整工程作为工作目录，把实际使用的版本、导出路径、操作命令及测试结果写入本地交接记录。保持模型和生成的游戏包私有。用户操作微信扫码登录、提供 AppID 或配置公司 CDN 时，说明当前所需的具体输入。

1. 从公司认可的私有渠道取得 `sango3-company-PC-0.28.0.tar.gz`。用 `Get-FileHash -Algorithm SHA256` 核对哈希 `30AD2710704D74479027A98EB8D4D50B10FBA9B3F886B5D688BB03725B3538EF`。在新的空目录运行 `tar.exe -xzf <工程包的实际路径>`（用带引号的实际路径替换占位符）。在 Cursor 和 Unity Hub 打开解压后包含 `Assets`、`Packages`、`ProjectSettings` 的目录。公开 GitHub 分支只用于读取说明和源码。
2. 确认 Unity Hub 中该编辑器已安装 WebGL Build Support，准备微信开发者工具稳定版。打开 `Assets/Sango3/Generated/Yingchuan.unity`，等待导入完成，在 Play 模式验证基线能进入部署界面。确保该场景已加入构建场景。
3. 按[微信 Unity 小游戏适配文档](https://wechat-miniprogram.github.io/minigame-unity-webgl-transform/)选择明确支持当前 Unity 编辑器的适配插件并记录版本。参考所选版本的安装和导出说明，配置目标小游戏 AppID、导出目录和资源加载地址。切换 WebGL 并通过插件导出小游戏。现有 `AndroidBuilder.Build` 是 Android 构建入口。若遇到平台编译或资源加载错误，保留第一条根因日志，在本地工作副本修复后重新导出。
4. 检查导出物，找到包含 `game.json` 的小游戏目录及对应 `project.config.json`。如果选用了远程资源，部署配套数据到公司授权的 HTTPS 资源地址，并在微信后台配置对应下载域名。记录输出目录和所需域名，不写入登录凭据。
5. 在微信开发者工具选择小游戏，导入导出的 `minigame` 工程，使用对应 AppID 和有开发权限的账号。检查控制台及网络加载，生成预览码。在三星微信中验证冷启动、部署、开始战斗、普攻、绝技、退后台再进入及存档恢复。白马关使用独立测试进度。记录手机型号、微信版本、首屏时间和实际通过项。[导入与体验版流程](https://docs.unity.cn/cn/tuanjiemanual/Manual/UploadWeixinMiniGame.html)
6. 将可导入目录、远程资源清单和真机结果交给用户，由用户从公司电脑上传开发版本，在后台版本管理中设为体验版。提交审核和公开发布属于后续步骤。
