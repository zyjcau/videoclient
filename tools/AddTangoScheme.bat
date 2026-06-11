@echo off

REM 设置协议名称，这里以 tango:// 为例，可以根据实际需求修改
set "PROTOCOL_NAME=tango"

REM 设置要关联启动的应用程序的完整路径，根据实际情况替换路径
set "APP_PATH=D:\Develop\Win\TangoWin\bin\x64\Debug\VideoClient.exe"

REM 以下是往注册表中写入相关键值的命令
reg add "HKCR\%PROTOCOL_NAME%" /v "URL Protocol" /t REG_SZ /d "" /f

REM 创建协议的注册表项（主项）
reg add "HKCR\%PROTOCOL_NAME%\shell\open\command" /ve /d "\"%APP_PATH%\" \"%%1\"" /f

REM 设置协议的默认值（友好名称等相关显示设置，这里简单设置协议名称）
reg add "HKCR\%PROTOCOL_NAME%" /ve /d "%PROTOCOL_NAME% Protocol" /f

REM 设置 URL 协议的默认图标（可选，如果需要指定应用程序相关图标，修改路径即可，此处示例为应用程序自身图标）
reg add "HKCR\%PROTOCOL_NAME%\DefaultIcon" /ve /d "\"%APP_PATH%\",0" /f

echo 注册表写入完成，已配置私有协议 %PROTOCOL_NAME%:// 关联到应用程序 %APP_PATH%
pause