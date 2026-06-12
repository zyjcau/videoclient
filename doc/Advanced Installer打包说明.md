# Advanced Installer 打包说明

本文档用于把当前项目编译输出打包成 Windows 安装包。


## 1. 前置准备

先确认项目已经能正常编译，并且能直接正常启动运行VideoClient.exe：

```text
../videoclient/bin/x64/Debug/VideoClient.exe
```

打包前建议关闭正在运行的客户端：

```text
VideoClient.exe
```

不要把运行时临时文件打进安装包：
```text
DawnCache
GPUCache
debug.log
```

## 2. 新建 Advanced Installer 工程

打开 Advanced Installer，选择：

```text
New Project -> Installer -> Enterprise / Professional / Simple
```

建议选择支持 `EXE setup with prerequisites` 的项目类型。

产品基础信息填写：

```text
Product Name: 远程会诊
Company Name: 远程会诊
Product Version: 1.04.14
```

安装包平台选择：

```text
64-bit package
```

## 3. 设置安装目录

进入：

```text
Install Parameters
```

设置 Application Folder：

```text
[ProgramFiles64Folder]远程会诊
```

安装类型选择：

```text
Per-machine only
```

这个设置会安装到所有用户，并安装到 `C:\Program Files`。安装时需要管理员权限，普通用户会看到 UAC 提权提示。

关键效果：

```text
ALLUSERS=1
安装目录为 Program Files
安装时需要管理员权限
```

## 4. 添加应用文件

进入：

```text
Files and Folders
```

选择 Application Folder，然后添加整个目录内容：

```text
../videoclient/bin/x64/Debug/
```

添加后检查并删除下面这些运行时文件或目录：

```text
DawnCache
GPUCache
debug.log
```

确认主程序在 Application Folder 根目录：

```text
VideoClient.exe
VideoClient.exe.config
wwwroot\
locales\
CefSharp.BrowserSubprocess.exe
*.dll
```
`

正确路径应该是：

```text
C:\Program Files\远程会诊\VideoClient.exe
```

## 5. 设置应用图标

进入：

```text
Product Details
```

设置 Add/Remove Programs 图标为：

```text
../videoclient/wwwroot/res/lss.ico
```


## 6. 添加桌面快捷方式

进入：

```text
Files and Folders
```

在 Application Folder 里选中：

```text
VideoClient.exe
```

右键选择：

```text
New Shortcut To -> Installed File
```

快捷方式名称：

```text
远程会诊
```

把快捷方式拖到：

```text
Desktop
```

快捷方式属性建议：

```text
Name: 远程会诊
Target: [#VideoClient.exe]
Working Directory: APPDIR
Icon: lss.ico 或 VideoClient.exe
```

## 7. 添加运行环境依赖

进入：

```text
Prerequisites
```

添加以下依赖：

```text
Microsoft .NET Framework 4.7.1
Microsoft Visual C++ 2015-2022 Redistributable (x64)
```

如果需要离线安装，把依赖安装包放到本地目录，例如：

```text
../NDP471-KB4033342-x86-x64-AllOS-ENU.exe
../vc_redist.x64.exe
```

在 Advanced Installer 里选择：

```text
Use files from disk
Include install files in EXE
```

这样用户电脑没有依赖时，安装包可以自动安装依赖。



## 8. 设置安装包输出

进入：

```text
Builds
```

设置输出类型：

```text
Single EXE setup
```

设置输出目录：

```text
../installer-output
```

设置安装包名称：

```text
VideoClientWindows_1.04.14_26.6.6.exe
```

确认：

```text
Files inside EXE: true
Package type: x64
Language: Chinese Simplified
```

## 9. 构建安装包

点击：

```text
Build
```

生成成功后应得到：

```text
../installer-output/VideoClientWindows_1.04.14_26.6.6.exe
```

