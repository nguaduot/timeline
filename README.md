![icon](./sample/icon.png)

# 拾光 for Windows 11

![developing](https://img.shields.io/badge/developing-v7.5-brightgreen)
[![release](https://img.shields.io/badge/release-v7.4.221004-blue)](https://gitee.com/nguaduot/timeline/releases)
![platform](https://img.shields.io/badge/platform-windows%2011%20%26%2010-lightgrey)

> 时光如歌，岁月如诗。拾光，每日一景

`拾光` 是一款壁纸应用，干净、舒适、流畅。集成多个高质量图源，支持每日推送桌面/锁屏。使用 UWP 框架开发，遵循 Fluent Design，是原生的 Windows 应用，于 Windows 11 体验最佳，向下兼容 Windows 10。

## 开始

提供以下两种安装方式：

+ 从 Microsoft Store 安装
  
  在 Microsoft Store 搜索 `拾光壁纸` 进行安装。直达链接：[拾光壁纸 - Microsoft Store](https://www.microsoft.com/store/apps/9N7VHQ989BB7)

+ 下载安装包手动安装
  
  在右侧的 [发行版](https://gitee.com/nguaduot/timeline/releases) 页面，找到最新版本，下载压缩包，然后解压，找到 `install.ps1` 脚本，右键 **使用 PowerShell 运行**，根据提示即可顺利安装。

  如无法顺利安装，请参考：[Issues #I5AQTO](https://gitee.com/nguaduot/timeline/issues/I5AQTO)

## 图源

自建图源：
+ [拾光](https://api.nguaduot.cn/timeline/doc) - 时光如歌，岁月如诗
  API 官网：[api.nguaduot.cn/timeline](https://api.nguaduot.cn/timeline/doc)
+ 贪食鬼 - 饕餮盛宴
+ 本地图源 - 我的精选

三方图源：
+ [Microsoft Bing](https://cn.bing.com) - 每天发现一个新地方
+ [NASA](https://apod.nasa.gov/apod) - 每日天文一图
+ [OnePlus](https://photos.oneplus.com) - Shot on OnePlus
+ [ONE · 一个](http://m.wufazhuce.com/one) - 复杂世界里，一个就够了
+ [向日葵8号](https://himawari.asia/) - 实时地球
+ [一梦幽黎](https://www.ymyouli.com) - 8K优质壁纸资源
+ [轻壁纸](https://bz.qinggongju.com) - 壁纸分享站
+ [wallhaven](https://wallhaven.cc/) - The best wallpapers on the Net
+ [WallHere](https://wallhere.com) - One of the best hd wallpapers site
+ [WallpaperUP](https://www.wallpaperup.com) - Your best source of wallpapers
+ [壁纸社](https://www.toopic.cn/dnbz) - 桌面高清壁纸
+ [Infinity](http://cn.infinitynewtab.com) - 精选高清壁纸
+ LSP - 不可描述

本地图源：
+ 本地图库 - 我的精选

*特别注明：三方图源均为来自网络，本应用无权且不提供商用授权，所以请勿用于商业用途，仅供学习交流。欢迎分享图源*

## 推送

以下为各图源推送服务对设置项的支持情况：

+ `一日一图` 类
  + Microsoft Bing
    + `地区`：忽略，默认当前地区
  + NASA
    + `排序`：忽略，默认 `收录`
    + `使用北京天文馆镜像`：忽略，默认 `关`
  + 拾光
    + `类别`：忽略，默认 `全部`
    + `排序`：忽略，默认 `收录`
    + `unauthorized`：忽略，默认 `0`
  + ONE · 一个
    + `排序`：忽略，默认 `收录`
  + OnePlus
    + `排序`：忽略，默认 `收录`
+ `实时一图` 类
  + 向日葵8号
    + `地球位置`：支持
    + `地球大小`：支持
+ `一次一图` 类
  + 本地图库
    + `图库路径`：支持
    + `图库深度`：支持
    + `排序`：忽略，默认 `随缘`
  + 一梦幽黎
    + `类别`：支持
    + `排序`：取当前排序结果第一页随机
  + 轻壁纸
    + `类别`：支持
    + `排序`：取当前排序结果第一页随机
  + wallhaven
    + `类别`：支持
    + `排序`：取当前排序结果第一页随机
  + WallHere
    + `类别`：支持
    + `排序`：取当前排序结果第一页随机
  + WallpaperUP
    + `类别`：支持
    + `排序`：取当前排序结果第一页随机
  + 壁纸社
    + `类别`：支持
    + `排序`：取当前排序结果第一页随机
  + Infinity
    + `排序`：取当前排序结果第一页随机
  + 贪食鬼
    + `集`：支持
    + `排序`：取当前排序结果第一页随机
  + LSP
    + `类别`：支持
    + `排序`：取当前排序结果第一页随机
    + `贤者模式`：忽略，默认 `0`
  + 乌云壁纸
    + `类别`：支持
    + `排序`：取当前排序结果第一页随机

## 快捷键

打开 `拾光`，快捷键 `F1` 可查看。
或前往浏览：[wiki / 快捷键](https://gitee.com/nguaduot/timeline/wikis/%E5%BF%AB%E6%8D%B7%E9%94%AE)

## 进阶

+ 如何调高桌面壁纸推送频率？
  + `Ctrl` + `I` 打开配置文件（或右键菜单点击“**设置**”图标，导航至“**常规**”组，展开“**配置文件**”，点击“**打开**”）
  + 找到目标图源的块 `[xxx]`（如 `一梦幽黎` 块为 `[ymyouli]`），找到参数 `desktopperiod`（推送间隔小时数），调为 `1` - `24` 之间的值，保存即可
  + 右键菜单开启桌面/锁屏推送即可

+ 如何解锁 `LSP` 图源？
  + `Ctrl` + `I` 打开配置文件（或右键菜单点击“**设置**”图标，导航至“**常规**”组，展开“**配置文件**”，点击“**打开**”）
  + 找到 `[app]` 块，将 `r18` 参数值修改为 `1`，保存，重新打开应用，图源列表中 `LSP` 将变为可用

## 更新日志

[CHANGELOG.md](./CHANGELOG.md)

## 反馈

相关问题或合作意向，可在以下渠道联系我：
+ 邮件 [nguaduot@163.com](mailto:nguaduot@163.com)
+ 酷安 [@南瓜多糖](http://www.coolapk.com/u/474144)
+ Telegram [@nguaduot](https://t.me/nguaduot)

## 截图

![Microsoft Store](./sample/store.png)

[![截图1](./sample/screenshot02.png)](https://gitee.com/nguaduot/timeline/raw/master/sample/%E6%8B%BE%E5%85%89_%E4%B8%80%E6%A2%A6%E5%B9%BD%E9%BB%8E_ABUIABACGAAgi8DPjwYoiKbruQYwgDw4-Bw.jpg)

![截图2](./sample/向日葵8号.gif)

*Copyright © 2021-2022 nguaduot. All rights reserved.*
