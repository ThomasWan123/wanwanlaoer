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

## 商业化交接（2026-11-08）

**11 月 9 日的交付目标是免费体验版。** 先完成转换、微信内真机预览和开发版本上传。下述收费设计是后续开发方案，真实收费另行开通、测试并发布。

### 玩家体验与首测

- 前三关免费。颍川烽火让玩家部署武将并亲手释放关羽绝技；长社破围通过中央弯道火阵展示布阵带来的收益；汝南清剿通过双路斥候检验分兵能力。第三关获胜先展示战果和奖励，再引导进入宛城夜袭。
- 第四关免费试玩第一波夜战护送，在自然停顿处展示「解锁完整战役 · 第 4–30 关 · 一次购买」，提供「返回免费关卡」。体验版标明后续战役的开放状态。首轮只验证一个永久商品。人民币 6 元是首测价格假设，实际价格需符合 AppID 后台可配置的档位。
- 记录新玩家、第三关通关、第四关试玩开始、解锁页展示、购买点击、服务端确认付款、权益到账。分别计算第三关通关率、通关后试玩率、解锁页到付款的转化率。免费体验阶段只报告已经开放的环节。

### 后续支付接入与验收

1. 先由账号管理员在微信公众平台核实该小游戏 AppID 的虚拟支付资格、协议、直购商品价格档位、结算主体和可用平台。使用[小游戏道具直购](https://developers.weixin.qq.com/minigame/dev/api/midas-payment/wx.requestMidasPaymentGameItem.html)，按[支付能力检测](https://developers.weixin.qq.com/minigame/dev/api/midas-payment/wx.checkIsSupportMidasPayment.html)决定是否展示购买入口，分别验证安卓与 iOS。
2. 服务端绑定微信玩家身份，校验商品和金额，创建唯一订单并签名。Unity 客户端调起支付。服务端核验平台通知或主动查单，以订单号幂等写入永久战役权益，重试后仍只发放一次。登录时从服务端恢复权益。支付密钥仅存服务端。参考[腾讯 CloudBase 接入说明](https://github.com/TencentCloudBase/CloudBase-AI-Toolkit/blob/main/config/source/skills/cloudbase-wechat-integration/references/virtual-payment.md)。
3. 沙箱验证购买成功、用户取消、重复通知、扣款后断网、重新登录及换设备恢复。确认未购买玩家可重玩前三关和试玩第四关第一波，购买者可继续第 4–30 关。记录实际 AppID 的设备支持情况、订单核验与到账结果后，再开启真实收费。
