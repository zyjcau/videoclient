function TangoWin(config) {
    this.version = 'V1.01.10'
    this.sdkVersion = ''
    this._init_(config)
}

TangoWin.prototype = {
    constructor: TangoWin,
    _init_: function (config) {
        this.DEBUG = (!config || config.DEBUG === undefined ? false : config.DEBUG)
        this.openByClient = this.getQueryParam('openByClient')

        this.host = this.parseHost(config)

        this.isNativeOK = false;//Native是否通讯正常

        this.appVersionCode = ''
        this.appVersionNumber = 0

        this.appName = ''
        this.clientName = 'VideoClientWindows'
        this.localIp = ''
        this.winScaling = '1' //适配不同的缩放比例，类型为string
        this.windowNumber = 1 //窗口数量
        this.enableDebug = false
        this.snapshotFolder = ''

        this.apiCode = ''

        this.enableRecording = true
        this.enableEndpointMode = false
        this.useDefaultLayout = false
        this.isAutoAssignRenderer = true
        this.displaySelfViewInMeeting = false

        this.web_ui_logo = ''
        this.web_ui_title = ''//需要在language.js中检索具体值
        this.web_ui_module_contacts_visible = true
        this.web_ui_module_contacts_favorite_btn_visible = !(this.getQueryParam('contactFavoriteHide') == 'true')
        this.web_ui_module_rooms_visible = true
        this.web_ui_module_callsip323_visible = true
        this.web_ui_module_profile_visible = true
        this.web_ui_contact_detail_join_btn_visible = true
        this.web_ui_create_room_btn_visible = true
        this.web_ui_room_label_display_name = ''
        this.web_ui_in_video_show_participants = true

        this.web_webrtc_site = ''//web参会地址

        //会议中状态
        this.isVideoConnected = false
        this.connectionStatusCode = 0
        this.curRoomId = undefined
        this.curRoomKey = undefined
        this.curRoomName = undefined
        this.curRoomOwnerIsMine = false
        this.participants = {}
        this.messages = []

        // this.isMuteMic = false
        // this.isLockMuteMic = false
        // this.isMuteSpeaker = false
        // this.isLockMuteSpeaker = false
        // this.isMuteCamera = false
        // this.isLockMuteCamera = false

        this.layoutInfo = {}
        this.functionAvailable = {sendMinorStream: true}

        //设备
        this.cameras = []
        this.camera_resolutions = [
            {label: '480p', value: '480p'},
            {label: '540p', value: '540p'},
            {label: '720p', value: '720p'},
            {label: '1080p', value: '1080p'},
            {label: '2K', value: '2K'},
            {label: '4K', value: '4K'},
        ]
        this.camera_framerate = [
            {label: '5帧', value: '5'},
            {label: '10帧', value: '10'},
            {label: '15帧', value: '15'},
            {label: '25帧', value: '25'},
            {label: '30帧', value: '30'},
            // {label: '40帧', value: '40'},
            {label: '50帧', value: '50'},
            {label: '60帧', value: '60'},
        ]
        this.microphone = {}
        this.speaker = {}
        this.displays = []

        this.isDirectlyJoin = false
        this.isAutoLoginNecessary = this.GetQueryBooleanByDefault('isAutoLoginNecessary', true);

        this.launchAndLogin = false;
        this.launchAndJoin = false;
        // this.leaveAndExitApp = this.getQueryParam('leaveAndExitApp') === 'true';
        this.tango = {};
        this.guest = {};
        this.launchParams = {
            leaveAndMinimizeApp: this.getQueryParam('leaveAndMinimizeApp') === 'true',
            leaveAndExitApp: this.getQueryParam('leaveAndExitApp') === 'true',
            server: this.getQueryParam('portal'),
            username: this.UrlDecode(this.getQueryParam('username')),
            password: this.GetQueryStringByDefault('password', ''),
            myContacts: this.getQueryParam('myContacts'),
            roomName: this.UrlDecode(this.getQueryParam('roomName')),
            roomKey: this.GetQueryStringByDefault('roomKey', ''),
            extension: this.getQueryParam('extension'),
            enableGuestMode: this.getQueryParam('enableGuestMode') === 'true',
            welcomePage: this.GetQueryBooleanByDefault('welcomePage', true),
            displayName: this.UrlDecode(this.getQueryParam('displayName')),
            autoRecord: this.GetQueryBooleanByDefault('autoRecord', false),
        };

        //传参以账号密码方式登录
        if (this.launchParams.server &&
            this.launchParams.username &&
            this.launchParams.password) {
            this.launchAndLogin = true;//如果账号密码都不为空，则以账号密码方式登录

            // this.launchParams.myContacts = this.getQueryParam('myContacts');

            // this.launchParams.roomName = this.UrlDecode(this.getQueryParam('roomName'));
            // this.launchParams.roomKey = this.getQueryParam('roomKey');
            // this.launchParams.extension = this.getQueryParam('extension');
            if (this.launchParams.roomKey) {
                this.launchAndJoin = true;//如果房间名和房间号都不为空，则登录后自动加入
            }
        }

        //传参以来宾方式加入
        if (this.launchParams.enableGuestMode && this.launchParams.server) {
            this.guest.server = this.launchParams.server;
            this.guest.displayName = this.launchParams.displayName;
            if (
                this.guest.displayName &&
                this.launchParams.roomKey &&
                !this.launchParams.welcomePage
            ) {
                this.launchAndJoin = true;
            }
        }

        this.myAudio = undefined;

        this.listeners = {
            onConnected: function () {
            },
            onDisconnected: function () {
            },
            onReconnect: function () {
            },
            onError: function () {
            },
            onVideoConnectionStateUpdated: function (statusCode) {
            },
            onParticipantAdded: function (participant) {
            },
            onParticipantRemoved: function (participant) {
            },
            onVideoSourcesUpdated: function (videoSources) {
            },
            onLayoutStatusChanged: function (json) {
            },
            onFunctionAvailableUpdated: function (json) {
            },
        }

        this.IM = (!config || !config.IM ? {
            isConnected: false,
            host: '',
            port: -1,
            enableSSL: true,
            scheme: 'http'
        } : config.IM);
        if (window.location.host.indexOf(':') === -1) {
            this.IM.host = window.location.host;
        } else {
            this.IM.host = window.location.host.split(':')[0];
        }
        this.IM.port = (parseInt(window.location.port) + 1);//端口对规则

        //tango
        this.isIgnoreFirstConstraint = true;//因为入会前已经同步过房间模式，不需要入会后通过消息再次响应，这可能会导致与vidyo的消息同时触发布局变化导致不可预料的bug
    },
    parseHost: function (config) {
        console.log(`> TangoWin parseHost(${config})`);
        let host;
        if (!config || !config.host) {
            host = `${window.location.protocol}//${window.location.host}`;
        } else {
            host = config.host;
        }
        console.log('parseHost result -> ' + host);
        return host;
    },
    //---------------------Native IM---------------------
    connect: function () {
        if (this.IM.socket) {
            console.log('-----> Native Connection already connected,do not need connect again.');
            return;
        }

        let addr = `${this.IM.scheme}://${this.IM.host}:${this.IM.port}`;

        console.log(`> Native connect(${addr})`);

        this.IM.socket = io.connect(
            addr,
            {
                transports: ['websocket'],
                rejectUnauthorized: false
            }
        );

        this.IM.socket.on('connect', (res) => {
            console.log('-----> Native on connected to im server. ');
            this.listeners.onConnected(res);
        });
        this.IM.socket.on('error', (res) => {
            console.log('-----> Native on error to im server. ' + res);
            this.listeners.onError(res);
        });
        this.IM.socket.on('reconnect', (res) => {
            console.log('-----> Native on reconnect to im server. ' + res);
            this.listeners.onReconnect(res);
        });
        this.IM.socket.on('disconnect', (res) => {
            console.log('-----> Native on disconnect from im server. ' + res);
            this.listeners.onDisconnected(res);
        });
        this.IM.socket.on('heartbeat', (data) => {
            this.IM.socket.emit('heartbeat', 'ack');
        });

        this.IM.socket.on('vidyo_connection_status_updated', (statusCode) => {// 1连接成功、-1连接失败、0断开连接、2正在重连、3重连成功、-2连接丢失
            this.connectionStatusCode = statusCode;
            this.isVideoConnected = !(statusCode === -1 || statusCode === 0);//如果不是失败或断开，则判定为已连接
            console.log(`-----> Native on vidyo_connection_status_updated(statusCode:${statusCode}),isVideoConnected:${this.isVideoConnected}`);

            this.listeners.onVideoConnectionStateUpdated(statusCode);

            //当退出会议后，初始化变量
            if (!this.isVideoConnected) {
                this.curRoomId = undefined;
                this.curRoomKey = undefined;
                this.curRoomName = undefined;
                this.messages.splice(0)
            }
        });

        this.IM.socket.on('system_status_updated', (jsonStr) => {

            let resp = JSON.parse(jsonStr);
            console.log(`-----> Native on system_status_updated`, resp);
            let data = resp.data;

            this.parseSystemStatusData(data);
        })

        this.IM.socket.on('layout_status_updated', (jsonStr) => {
            console.log(`-----> Native on layout_status_updated`, JSON.parse(jsonStr));

            let resp = JSON.parse(jsonStr);
            let data = resp.data;

            this.layoutInfo.curLayoutMode = data.curLayoutMode;
            this.layoutInfo.remoteWindowShareCount = data.remoteWindowShareCount;

            this.listeners.onLayoutStatusChanged(jsonStr);
        })

        this.IM.socket.on('function_available_updated', (jsonStr) => {
            console.log(`-----> Native on function_available_updated`, JSON.parse(jsonStr));

            let resp = JSON.parse(jsonStr);
            let data = resp.data;

            this.functionAvailable.sendMinorStream = data.sendMinorStream;

            this.listeners.onFunctionAvailableUpdated(jsonStr);
        })

        this.IM.socket.on('function_state_updated', (jsonStr) => {
            console.log(`-----> Native on function_state_updated`, JSON.parse(jsonStr));

            this.listeners.onFunctionAvailableUpdated(jsonStr);
        })

        this.IM.socket.on('video_sources_updated', (jsonStr) => {
            console.log(`-----> Native on video_sources_updated`, JSON.parse(jsonStr));

            let resp = JSON.parse(jsonStr);
            let videoSources = resp.data;
            this.listeners.onVideoSourcesUpdated(videoSources);
        })

        this.IM.socket.on('participant_updated', (jsonStr) => {
            console.log(`-----> Native on participant_updated`, JSON.parse(jsonStr));

            let resp = JSON.parse(jsonStr);
            let participantsMap = resp.data;
            if (participantsMap) {
                this.participants = participantsMap;
            }
        })
        this.IM.socket.on('participant_onadded', (participant) => {
            console.log(`-----> Native on participant_onadded`, participant);

            this.participants[participant.id] = participant;
            this.listeners.onParticipantAdded(participant);
        })
        this.IM.socket.on('participant_onremoved', (participant) => {
            console.log(`-----> Native on participant_onremoved`, participant);

            delete this.participants[participant.id];
            this.listeners.onParticipantRemoved(participant);
        })
        this.on('chat_message_received', (message) => {
            this.messages.push(JSON.parse(message))
        })

        return this.IM.socket;
    },
    disconnect: function () {
        console.log(`> disconnect()`);
        if (this.IM.socket) {
            this.IM.socket.removeAllListeners();
            this.IM.socket.disconnect();
            console.log("-----> IM Connection disconnect success.");
        } else {
            console.log("-----> IM Connection disconnect failed,because has not connect.");
        }
    },
    on: function (event, callback) {
        if (this.IM.socket) {
            this.IM.socket.on(event, callback)
        }
    },
    off: function (event, callback) {
        if (this.IM.socket) {
            this.IM.socket.off(event, callback)
        }
    },
    //---------------------API---------------------
    check: function ({error, success}) {
        this.getNativeVersion({
            success: this.createDelegate(function (resp) {
                this.isNativeOK = true;
                success(resp)
            }, this),
            error: this.createDelegate(function (resp) {
                this.isNativeOK = false;
                error(resp)
            }, this),
        })
    },
    getNativeVersion: function ({success, error}) {
        this.requestByPost(
            'getVersion',
            {},
            {dataType: "json", error: error, success: success});
    },
    saveConfig: function ({key, value, error, success}) {
        console.log(`saveConfig(${key},${value})`);

        this.requestByPost(
            'saveConfig',
            {
                key: key,
                value: value
            },
            {
                dataType: 'json',
                error: error,
                success: success
            });
    },
    getIODeviceState: function ({error, success}) {
        this.requestByPost(
            'getIODeviceState',
            {},
            {
                dataType: "json",
                error: error,
                success: success
            });
    },
    getSystemStatusJson: function ({error, success}) {
        this.requestByPost(
            'getSystemStatus',
            {},
            {
                dataType: "json",
                error: error,
                success: this.createDelegate(function (resp) {
                    // console.log(resp)
                    this.parseSystemStatusData(resp.data);
                    success(resp)
                }, this)
            });
    },
    parseSystemStatusData: function (data) {
        this.sdkVersion = data.sdkVersion;
        this.appVersionCode = data.appVersionCode
        this.appVersionNumber = data.appVersionNumber
        this.appName = data.appName;
        this.clientName = data.clientName;
        this.localIp = data.localIp;
        this.winScaling = data.winScaling;
        this.windowNumber = data.windowNumber;
        this.enableDebug = data.enableDebug;
        this.isVideoConnected = data.isConnected;
        this.connectionStatusCode = data.connectionStatusCode;
        this.snapshotFolder = this.UrlDecode(data.snapshotFolder);

        this.apiCode = data.apiCode;
        this.curRoomKey = data.curRoomKey;
        this.curRoomName = data.curRoomName;

        this.web_ui_logo = data.web_ui_logo;
        this.web_ui_title = data.web_ui_title;
        this.web_ui_room_label_display_name = data.web_ui_room_label_display_name;
        this.web_ui_module_contacts_visible = data.web_ui_module_contacts_visible;
        this.web_ui_module_rooms_visible = data.web_ui_module_rooms_visible;
        this.web_ui_module_callsip323_visible = data.web_ui_module_callsip323_visible;
        this.web_ui_module_profile_visible = data.web_ui_module_profile_visible;
        this.web_ui_contact_detail_join_btn_visible = data.web_ui_contact_detail_join_btn_visible;
        this.web_ui_create_room_btn_visible = data.web_ui_create_room_btn_visible;
        this.web_webrtc_site = data.web_webrtc_site;

        this.enableRecording = data.enableRecording;
        this.enableEndpointMode = data.enableEndpointMode;
        this.useDefaultLayout = data.useDefaultLayout;
        this.isAutoAssignRenderer = data.isAutoAssignRenderer;
        let displayGuestVideo = data.displayGuestVideo;
        let displayGatewayVideo = data.displayGatewayVideo;
        this.displaySelfViewInMeeting = data.displaySelfViewInMeeting;

        let gatewayPrefixH323 = data.gatewayPrefixH323;
        let gatewayPrefixSIP = data.gatewayPrefixSIP;

        //缓存摄像头配置信息
        this.cameras.splice(0)
        data.camera1.index = 1;
        this.cameras.push(data.camera1);
        data.camera2.index = 2;
        this.cameras.push(data.camera2);
        data.camera3.index = 3;
        this.cameras.push(data.camera3);
        data.camera4.index = 4;
        this.cameras.push(data.camera4);
        data.camera5.index = 5;
        this.cameras.push(data.camera5);
        //缓存音频设备配置信息
        this.microphone = data.microphones;
        this.speaker = data.speakers;
        //display
        this.displays = data.displays;
        //tango
        this.tango = data.tango;
        //guest 当未通过传参启动时，读取本地缓存数据
        if (!this.launchParams.enableGuestMode) this.guest = data.guest;
    },
    join: function (
        {
            portal: portal,
            userName: userName = '',
            password: password = '',
            roomName: roomName,
            roomKey: roomKey,
            roomPin: roomPin,
            displayName: displayName,
            success: success,
            error: error
        }) {

        console.log('----Join----');
        console.log('portal:' + portal);
        console.log('userName:' + userName);
        console.log('password:' + password);
        console.log('roomName:' + roomName);
        console.log('roomKey:' + roomKey);
        console.log('roomPin:' + roomPin);
        console.log('displayName:' + displayName);

        if (!this.isNativeOK) {
            console.log('Native组件不可用，无法调用join')
            error('Native组件不可用，无法调用join')
            return;
        }

        this.curRoomKey = roomKey;
        this.curRoomName = roomName;

        this.requestByPost(
            'join',
            {
                "portal": portal,
                "userName": userName,
                "password": password,
                "roomKey": roomKey,
                "roomName": roomName,
                "roomPin": roomPin,
                "displayName": displayName
            },
            {
                dataType: "json",
                error: this.createDelegate(function (resp) {
                    this.curRoomId = undefined;
                    this.curRoomKey = undefined;
                    this.curRoomName = undefined;
                    error(resp)
                }, this),
                success: this.createDelegate(function (resp) {
                    success(resp)
                }, this)
            });
    },
    leave: function ({success, error}) {
        if (!this.isNativeOK) {
            console.log('Native组件不可用，无法调用leave')
            return;
        }
        console.log('----Leave----');
        this.requestByPost(
            'leave',
            {},
            {error: error, success: success});
    },
    sendChatMessage: function ({message, success, error}) {
        if (!this.isNativeOK) {
            console.log('Native组件不可用，无法调用sendChatMessage')
            return;
        }
        console.log('----sendChatMessage----');
        this.messages.push({
            from: '我',
            content: message,
            fromMe: true
        })
        this.requestByPost(
            'sendChatMessage',
            {message: message},
            {error: error, success: success});
    },
    openCefWindow: function ({url, title, success, error}) {
        if (!this.isNativeOK) {
            console.log('Native组件不可用，无法调用leave')
            return;
        }
        console.log('----openCefWindow----');
        this.requestByPost(
            'openCefWindow',
            {
                "url": url,
                "title": title
            },
            {error: error, success: success});
    },
    openTangoDrawWindow: function ({url, username, room, title, isStarter = false, success, error}) {
        if (!this.isNativeOK) {
            console.log('Native组件不可用，无法调用leave')
            return;
        }
        console.log('----openTangoDrawWindow----');
        this.requestByPost(
            'openTangoDrawWindow',
            {
                "url": url,
                "username": username,
                "room": room,
                "title": title,
                "isStarter": isStarter
            },
            {error: error, success: success});
    },
    openStartShareWindow: function ({success, error}) {
        if (!this.isNativeOK) {
            console.log('Native组件不可用，无法调用leave')
            return;
        }
        console.log('----openStartShareWindow----');
        this.requestByPost(
            'openStartShareWindow',
            {},
            {error: error, success: success});
    },
    openSettingsWindow: function ({success, error}) {
        if (!this.isNativeOK) {
            console.log('Native组件不可用，无法调用leave')
            return;
        }
        console.log('----openSettingsWindow----');
        this.requestByPost(
            'openSettingsWindow',
            {},
            {error: error, success: success});
    },
    openAVCheckWindow: function ({success, error}) {
        if (!this.isNativeOK) {
            console.log('Native组件不可用，无法调用leave')
            return;
        }
        console.log('----openAVCheckWindow----');
        this.requestByPost(
            'openAVCheckWindow',
            {},
            {error: error, success: success});
    },
    getParticipantsList: function () {
        let list = [];
        for (let pid in this.participants) {
            list.push(this.participants[pid])
        }
        return list;
    },
    setVideoLocationSize: function (x, y, width, height, {success, error}) {

        if (this.enableEndpointMode) {
            console.log(`当前为终端模式，无法调用setVideoLocationSize(${x},${y},${width},${height})`)
            return;
        }
        if (!this.isNativeOK) {
            console.log(`Native组件不可用，无法调用setVideoLocationSize(${x},${y},${width},${height})`)
            return;
        }

        let actuallyWinScaling = parseFloat(this.winScaling);
        x = x * actuallyWinScaling;
        y = y * actuallyWinScaling;
        width *= actuallyWinScaling;
        height *= actuallyWinScaling;

        console.log(`----SetVideoLocationSize(${x},${y},${width},${height})----`);
        this.requestByPost(
            'setVideoLocationSize',
            {
                "x": x,
                "y": y,
                "width": width,
                "height": height
            },
            {error: error, success: success});
    },
    setVideoVisible: function (visible, {success, error}) {
        if (this.enableEndpointMode) {
            console.log(`当前为终端模式，无法调用setVideoVisible(${visible})`)
            return;
        }
        if (!this.isNativeOK) {
            console.log(`Native组件不可用，无法调用setVideoVisible(${visible})`)
            return;
        }
        console.log(`----setVideoVisible(${visible})----`);
        this.requestByPost(
            'setVideoVisible',
            {
                "visible": visible
            },
            {error: error, success: success});
    },
    setMicPrivacy: function (isPrivacy, {success, error}) {
        if (!this.isNativeOK) {
            console.log(`Native组件不可用，setMicPrivacy(${isPrivacy})`)
            return;
        }
        console.log(`----setMicPrivacy(${isPrivacy})----`);
        this.requestByPost(
            'setMicPrivacy',
            {
                "isPrivacy": isPrivacy
            },
            {error: error, success: success});
    },
    setSpeakerPrivacy: function (isPrivacy, {success, error}) {
        if (!this.isNativeOK) {
            console.log(`Native组件不可用，setSpeakerPrivacy(${isPrivacy})`)
            return;
        }
        console.log(`----setSpeakerPrivacy(${isPrivacy})----`);
        this.requestByPost(
            'setSpeakerPrivacy',
            {
                "isPrivacy": isPrivacy
            },
            {error: error, success: success});
    },
    setCameraPrivacy: function (isPrivacy, {success, error}) {
        if (!this.isNativeOK) {
            console.log(`Native组件不可用，setCameraPrivacy(${isPrivacy})`)
            return;
        }
        console.log(`----setCameraPrivacy(${isPrivacy})----`);
        this.requestByPost(
            'setCameraPrivacy',
            {
                "isPrivacy": isPrivacy
            },
            {error: error, success: success});
    },
    assignCamera: function (cameraIndex, cameraName, {error, success}) {
        console.log(`----assignCamera(${cameraIndex},${cameraName})----`);
        this.requestByPost(
            'assignCamera',
            {
                "cameraIndex": cameraIndex,
                "cameraName": cameraName
            },
            {error: error, success: success});
    },
    assignMicrophone: function (micName, {error, success}) {
        console.log(`----assignMicrophone(${micName})----`);
        this.requestByPost(
            'assignMicrophone',
            {
                "micName": micName
            },
            {error: error, success: success});
    },
    assignSpeaker: function (speakerName, {error, success}) {
        console.log(`----assignSpeaker(${speakerName})----`);
        this.requestByPost(
            'assignSpeaker',
            {
                "speakerName": speakerName
            },
            {error: error, success: success});
    },
    getCameraList: function (cameraIndex, {success, error}) {
        console.log(`----getCameraList(${cameraIndex})----`);
        this.requestByPost(
            'listCamera',
            {
                "cameraIndex": cameraIndex
            },
            {dataType: "json", error: error, success: success});
    },
    setCameraConfig: function (cameraIndex, resolution, framerate, resolutionProfile, framerateProfile, {
        error,
        success
    }) {
        console.log(`----setCameraConfig(${cameraIndex},${resolution},${framerate},${resolutionProfile},${framerateProfile})----`);
        this.requestByPost(
            'configCamera',
            {
                "cameraIndex": cameraIndex,
                "resolution": resolution,
                "framerate": framerate,
                "resolutionProfile": resolutionProfile,
                "framerateProfile": framerateProfile
            },
            {error: error, success: success});
    },
    setDebugEnable: function (enable, {error, success}) {
        console.log(`----setDebugEnable(${enable})----`);
        // this.enableDebug = enable;
        this.requestByPost(
            'setDebugEnable',
            {
                "enable": enable
            },
            {error: error, success: success});
    },
    setScreenIndexDisplay: function (display, {error, success}) {//显示屏幕编号
        console.log(`----setScreenIndexDisplay(${display})----`);
        this.requestByPost(
            'setScreenIndexDisplay',
            {
                "visible": display
            },
            {error: error, success: success});
    },
    setLayoutMode: function (mode, {error, success}) {//是否使用默认布局，默认：default
        console.log(`----setLayoutMode(${mode})----`);
        this.requestByPost(
            'setLayoutMode',
            {
                "mode": mode
            },
            {error: error, success: success});
    },
    setCustomLayoutMode: function (layoutMode, lecturerId, {error, success, isByUser = false}) {//是否使用默认布局，默认：default
        console.log(`----setCustomLayoutMode(${layoutMode},${lecturerId})----`);
        this.requestByPost(
            'setCustomLayoutMode',
            {
                "layoutMode": layoutMode,
                "lecturerId": lecturerId,
                "isByUser": isByUser
            },
            {error: error, success: success});
    },
    getVideoSources: function ({error, success}) {
        console.log(`----getVideoSources----`);
        this.requestByPost(
            'getVideoSources',
            {},
            {dataType: 'json', error: error, success: success});
    },
    /**
     *
     * @param sourceKey
     * @param screenIndex
     * @param rendererIndex
     * @param error
     * @param success
     */
    startRendering: function (sourceKey, screenIndex = -1, rendererIndex = -1, {error, success}) {
        console.log(`----startRendering(${sourceKey},${screenIndex},${rendererIndex})----`);
        this.requestByPost(
            'startRendering',
            {
                "sourceKey": sourceKey,
                "screenIndex": screenIndex,
                "rendererIndex": rendererIndex
            },
            {error: error, success: success});
    },
    stopRendering: function (sourceKey, {error, success}) {
        console.log(`----stopRendering(${sourceKey})----`);
        this.requestByPost(
            'stopRendering',
            {
                "sourceKey": sourceKey
            },
            {error: error, success: success});
    },
    setAutoAssignRenderer: function (auto, {error, success}) {
        console.log(`----setAutoAssignRenderer(auto:${auto})----`);
        this.requestByPost(
            'setAutoAssignRenderer',
            {
                "auto": auto
            },
            {error: error, success: success});
    },
    setRendererContainerForceLayoutNum: function (containerIndex, num, {error, success}) {
        console.log(`----setForceRendererDisplayNum(${containerIndex},${num})----`);
        this.requestByPost(
            'setRendererContainerForceLayoutNum',
            {
                "containerIndex": containerIndex,
                'num': num
            },
            {error: error, success: success});
    },
    setAutoAnswer: function (auto, {error, success}) {
        console.log(`----setAutoAnswer(auto:${auto})----`);
        this.requestByPost(
            'setAutoAnswer',
            {
                "auto": auto
            },
            {error: error, success: success});
    },
    saveTangoLoginInfo: function (server, username, password, isAutoLogin, subsystemTangoDrawUrl, {error, success}) {
        console.log(`----saveTangoLoginInfo(${server},${username},${password},${isAutoLogin})----`);
        this.requestByPost(
            'saveTangoLoginInfo',
            {
                'server': server,
                'username': username,
                'password': password,
                'isAutoLogin': isAutoLogin,
                'subsystemTangoDrawUrl': subsystemTangoDrawUrl
            },
            {error: error, success: success});
    },
    addUserHardDeviceInfo: function (name, callNumber, {error, success}) {
        console.log(`----addUserHardDeviceInfo(${name},${callNumber})----`);
        this.requestByPost(
            'addUserHardDeviceInfo',
            {
                'name': name,
                'callNumber': callNumber
            },
            {error: error, success: success});
    },
    deleteUserHardDeviceInfo(name, callNumber, {error, success}) {
        console.log(`----deleteUserHardDeviceInfo(${name},${callNumber})----`);
        this.requestByPost(
            'deleteUserHardDeviceInfo',
            {
                'name': name,
                'callNumber': callNumber
            },
            {error: error, success: success});
    },
    copyToClipboard({content, error, success}) {
        console.log(`----copyToClipboard()----`);
        this.requestByPost(
            'copyToClipboard',
            {
                content: content
            },
            {error: error, success: success});
    },
    startUrl({url, error, success}) {
        console.log(`----startUrl(${url})----`);
        this.requestByPost(
            'startUrl',
            {
                url: url
            },
            {error: error, success: success});
    },
    showDialog({message, error, success}) {
        console.log(`----showDialog(${message})----`);
        this.requestByPost(
            'showDialog',
            {
                message: message
            },
            {error: error, success: success});
    },
    appActivate({error, success}) {
        console.log(`----appActivate()----`);
        this.requestByPost(
            'appActivate',
            {},
            {error: error, success: success});
    },
    appMinimize({error, success}) {
        console.log(`----appMinimize()----`);
        this.requestByPost(
            'appMinimize',
            {},
            {error: error, success: success});
    },
    appExit({error, success}) {
        console.log(`----appExit()----`);
        this.requestByPost(
            'appExit',
            {},
            {error: error, success: success});
    },
    openFolder({path, success, error}) {
        console.log(`----openFolder(${path})----`);
        this.requestByPost(
            'openFolder',
            {path: path},
            {error: error, success: success});
    },
    startProcess({target, success, error}) {
        console.log(`----startProcess(${target})----`);
        this.requestByPost(
            'startProcess',
            {target: target},
            {error: error, success: success});
    },
    checkAppVersion({tangoPortal, success, failed}) {
        $.ajax({
            url: `${tangoPortal}/client/getNewVersion`,
            data: {
                platform: 'windows',
                clientCurrentVersionCode: this.appVersionNumber,
                clientName: this.clientName
            },
            type: 'get',
            dataType: 'json',
            success: resp => {
                console.log(`-----> checkAppVersion success -> `, resp)
                if (resp.data && resp.data.install_file_name) {
                    console.log('----->  need update to newest version...')
                    if (success) success(resp.data)
                    ElementPlus.ElMessageBox.confirm(
                        `新版本内容：${resp.data.version_desc}`,
                        `新版本：${resp.data.version_name}`,
                        {
                            'close-on-click-modal': false,
                            cancelButtonText: '取消',
                            confirmButtonText: '下载更新',
                            center: true
                        }
                    ).then(() => {
                        this.startProcess({target: `${tangoPortal}/client/get?filename=${resp.data.install_file_name}`})
                    }).catch(() => {

                    })
                } else {
                    if (success) success(undefined)
                }
            },
            error: err => {
                console.log(`----->  checkAppVersion failed. -> `, err)
                if (failed) failed(err)
            }
        })
    },
    requestByPost: function (method, data, {dataType = 'text', error, success}) {
        $.ajax({
            url: this.host + '/' + method,
            type: 'post',
            headers: {
                "Content-Type": "application/x-www-form-urlencoded"
            },
            dataType: dataType,
            contentType: 'text/xml;charset="UTF-8"',
            data: data,
            error: error,
            success: success
        });
    },
    //播放响铃
    playAudio: function () {
        if (!this.myAudio) {
            this.myAudio = new Audio('../res/audio/ling.mp3')
        }
        this.myAudio.play();
        console.log('audio.play')
    },
    stopAudio: function () {
        if (this.myAudio) {
            this.myAudio.pause();
        }
    },
    //判断是否正在发送辅流（包括窗口共享、显示器共享）
    isSharing: function () {
        return this.cameras[3].inUse !== '关闭' || this.cameras[4].inUse !== '关闭';
    },
    stopSharing: function ({success, error}) {//4 monitor 5 window
        if (this.cameras[3].inUse !== '关闭') {
            this.assignCamera(4, '关闭', {success: success, error: error})
            console.log(`stopSharing monitor success.`)
        } else if (this.cameras[4].inUse !== '关闭') {
            this.assignCamera(5, '关闭', {success: success, error: error})
            console.log(`stopSharing window success.`)
        } else {
            console.log(`stopSharing failed,has no 1 sharing.`)
            error()
        }
    },
    isNumber: function (val) {
        let regPos = /^\d+(\.\d+)?$/;
        let regNeg = /^(-(([0-9]+\.[0-9]*[1-9][0-9]*)|([0-9]*[1-9][0-9]*\.[0-9]+)|([0-9]*[1-9][0-9]*)))$/;
        return regPos.test(val) || regNeg.test(val);
    },
    debounce: function (fn, delay, isImmediate) {
        var timer = null;  //初始化timer，作为计时清除依据
        return function () {
            let context = this;  //获取函数所在作用域this
            let args = arguments;  //取得传入参数
            clearTimeout(timer);
            if (isImmediate && timer === null) {
                //时间间隔外立即执行
                fn.apply(context, args);
                timer = 0;
                return;
            }
            timer = setTimeout(function () {
                fn.apply(context, args);
                timer = null;
            }, delay);
        }
    },
    // function createDelegate
    createDelegate: function (handler, obj) {
        obj = obj || this;
        return function () {
            handler.apply(obj, arguments);
        }
    },
    //function parse soap
    findNode: function (xml, nodeName) {
        return $(xml).find(nodeName).first().text();
    },
    getQueryParam: function (variable) {
        var query = window.location.search.substring(1);
        var vars = query.split("&");
        for (var i = 0; i < vars.length; i++) {
            var pair = vars[i].split("=");
            if (pair[0] == variable) {
                return pair[1];
            }
        }
        return (false);
    },
    GetQueryStringByDefault: function (name, defaultValue) {
        var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)");
        var r = window.location.search.substr(1).match(reg);
        if (r != null) return decodeURI(r[2]);
        return defaultValue;
    },
    GetQueryBooleanByDefault: function (name, defaultValue) {
        var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)");
        var r = window.location.search.substr(1).match(reg);
        if (r != null) return decodeURI(r[2]) == 'true' ? true : false;
        return defaultValue ? true : false;
    },
    UrlDecode: function (zipStr) {
        var uzipStr = "";
        for (var i = 0; i < zipStr.length; i++) {
            var chr = zipStr.charAt(i);
            if (chr == "+") {
                uzipStr += " ";
            } else if (chr == "%") {
                var asc = zipStr.substring(i + 1, i + 3);
                if (parseInt("0x" + asc) > 0x7f) {
                    uzipStr += decodeURI("%" + asc.toString() + zipStr.substring(i + 3, i + 9).toString());
                    i += 8;
                } else {
                    uzipStr += this.AsciiToString(parseInt("0x" + asc));
                    i += 2;
                }
            } else {
                uzipStr += chr;
            }
        }
        return uzipStr;
    },
    StringToAscii: function (str) {
        return str.charCodeAt(0).toString(16);
    },
    AsciiToString: function (asccode) {
        return String.fromCharCode(asccode);
    },
    setCookie: function (cname, cvalue, exdays) {
        var d = new Date();
        d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
        var expires = "expires=" + d.toGMTString();
        document.cookie = cname + "=" + cvalue + "; " + expires;
    },
    getCookie: function (cname) {
        var name = cname + "=";
        var ca = document.cookie.split(';');
        for (var i = 0; i < ca.length; i++) {
            var c = ca[i].trim();
            if (c.indexOf(name) == 0) return c.substring(name.length, c.length);
        }
        return "";
    },
//log
    log: function (content) {
        if (this.DEBUG) console.log(content);
    }
}