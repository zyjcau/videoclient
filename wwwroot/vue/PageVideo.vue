<template>
  <el-container id="container_in_video">
    <el-aside id="video_aside" v-if="isParticipantsVisible">
      <el-header id="video_header" :class="participant_list_header_class">
        <div ref="carousel" :class="startAnimation" style="display: inline-block">
          <span>{{ title }}</span>
          <i class="el-icon-tangorecordingstart" v-if="isRecording"></i>
        </div>
      </el-header>
      <!--参会人列表-->
      <el-row class="list_item"
              align="middle"
              v-for="participant in participants"
              v-bind:key="participant.id">
        <el-col :span="4" style="font-size: 0" align="middle">
          <div class="list_item_icon">
            <el-avatar
                fit="contain">
              {{ participant.name ? participant.name.substring(0, 1) : 'g' }}
            </el-avatar>
          </div>
        </el-col>
        <el-col :span="16">
          <div style="height: 100%;padding-left: 16px">
            <p style="color: white">{{ participant.name }}</p>
          </div>
        </el-col>
        <el-col :span="4">
          <div>
            <span>
              
            </span>
          </div>
          <!--          <i class="el-icon-tangofangjian"></i>-->
        </el-col>
      </el-row>
    </el-aside>
    <el-container>
      <el-main id="main_video">

        <el-row style="height: 100%">
          <el-col :span="isChatVisible?19:24">
            <div id="div_video" ref="divVideo">
              <div v-for="(d,index) in enableSetting" :key="index" style="width: 100%;height: 100%">
                <settings-layout></settings-layout>
              </div>
            </div>
          </el-col>
          <el-col :span="5" v-if="isChatVisible">
            <text-chat></text-chat>
          </el-col>
        </el-row>

        <el-dialog
            v-model="dialogTranscribe"
            title="录制填写信息"
            :close-on-click-modal="false"
            :close-on-press-escape="false"
            :show-close="false"
        >
          <el-form>
            <el-form-item label="输入录制名称信息:">
              <el-input autocomplete="off" v-model="recordingVideoMeg.title"></el-input>
            </el-form-item>
            <el-form-item label="患者名称:">
              <el-input autocomplete="off" v-model="recordingVideoMeg.patientName"></el-input>
            </el-form-item>
            <el-form-item label="主治医生名称:">
              <el-input autocomplete="off" v-model="recordingVideoMeg.doctorName"></el-input>
            </el-form-item>
          </el-form>
          <div style="text-align: right">
            <el-button style="display: inline-block" @click="onDialogDismiss">取消</el-button>
            <el-button style="display: inline-block" @click="onStartRecord">开始录制</el-button>
          </div>
        </el-dialog>

      </el-main>
      <el-footer id="video_footer">
        <el-row>
          <el-col :span="4">
            <el-button size="small" v-if="openByClientNative"
                       @click="onParticipantsVisible"
                       :class="isParticipantsVisible?'icon_menu_close':'icon_menu_open'">
            </el-button>
            <el-tooltip v-if="isTangoEnable" content="会议控制" placement="right" effect="light"
                        style="margin-left: 16px">
              <el-button size="small" @click="onMeetingControlClick">
                <i class="el-icon-tangorepair" style="size:24px"></i>
              </el-button>
            </el-tooltip>
          </el-col>
          <el-col :span="18">
            <div style="text-align: center">
              <el-tooltip v-if="isLayoutBtnVisible" :content="isLayoutModeLecture?'网格布局':'画廊布局'"
                          placement="right"
                          effect="light">
                <el-button size="small"
                           @click="onLayoutModeClick"
                           circle
                           style="margin-right: 32px">
                  <i :class="isLayoutModeLecture?'el-icon-tangolayoutgrid':'el-icon-tangolayoutlecture'"
                     style="size: 24px">
                  </i>
                </el-button>
              </el-tooltip>
              <el-tooltip content="查看我的录像" placement="right" effect="light" v-if="isRecordingVisible">
                <el-button size="small" @click="onBrowseRecordClick" style="margin-left: 32px" round>
                  查看录像
                </el-button>
              </el-tooltip>
              <el-tooltip :content="isRecording?'停止录制':'开始录制'" placement="right" effect="light"
                          v-if="isRecordingVisible">
                <el-button size="small" @click="onRecordingClick" :loading="inHandleRecording" round
                           style="margin-right: 32px">
                  <template #loading>
                    处理中...
                    <div class="custom-loading" v-if="false">
                      <svg class="circular" viewBox="-10, -10, 50, 50">
                        <path
                            class="path"
                            d="
            M 30 15
            L 28 17
            M 25.61 25.61
            A 15 15, 0, 0, 1, 15 30
            A 15 15, 0, 1, 1, 27.99 7.5
            L 15 15
          "
                            style="stroke-width: 4px; fill: rgba(0, 0, 0, 0)"
                        />
                      </svg>
                    </div>
                  </template>
                  <i :class="isRecording?'el-icon-tangorecordingstop':'el-icon-tangorecordingstart'"
                     style="size: 24px"></i>
                </el-button>
              </el-tooltip>
              <el-tooltip content="电子白板" placement="left" effect="light" v-if="isWhiteBoardAvailable">
                <el-button size="small" @click="onWhiteboardClick" circle>
                  <i :class="'el-icon-tangowhiteboard'"></i></el-button>
              </el-tooltip>
              <el-tooltip v-if="isStartShareBtnVisible" :content="isSharing?'停止分享':'分享屏幕'" placement="left"
                          effect="light">
                <el-button size="small" @click="onStartShareClick" circle>
                  <i :class="isSharing?'el-icon-tangorecordingstop':'el-icon-tangoscreenshare'"></i></el-button>
              </el-tooltip>
              <el-tooltip :content="isLockMuteMic?'麦克风已被管理员禁用':'禁用麦克风'" placement="left" effect="light">
                <el-button size="small" @click="onMuteMicClick"
                           :type="isLockMuteMic?'info':(isMuteMic?'danger':'success')" circle>
                  <i :class="isMuteMic?'el-icon-tangomicmute':'el-icon-tangomicopen'"></i></el-button>
              </el-tooltip>
              <el-tooltip content="禁用扬声器" placement="left" effect="light">
                <el-button size="small" @click="onMuteSpeakerClick" :type="isMuteSpeaker?'danger':'success'" circle>
                  <i :class="isMuteSpeaker?'el-icon-tangospeakerclose':'el-icon-tangospeakeropen'"></i>
                </el-button>
              </el-tooltip>
              <el-tooltip :content="isLockMuteCamera?'摄像头已被管理员禁用':'禁用摄像头'" placement="right"
                          effect="light">
                <el-button size="small" @click="onMuteCameraClick"
                           :type="isLockMuteCamera?'info':(isMuteCamera?'danger':'success')" circle>
                  <i :class="isMuteCamera?'el-icon-tangocameramute':'el-icon-tangocameraopen'"></i></el-button>
              </el-tooltip>
              <el-tooltip content="布局设置" placement="right" effect="light"
                          v-if="isLayoutSettingVisible">
                <el-button size="small" @click="onLayoutSettingClick" circle style="margin-left: 32px">
                  <i class="el-icon-tangolayout"></i></el-button>
              </el-tooltip>
              <el-tooltip content="设置" placement="right" effect="light">
                <el-button size="small" @click="onSettingClick" circle style="margin-left: 32px">
                  <i class="el-icon-tangoshezhi"></i></el-button>
              </el-tooltip>
              <el-tooltip content="聊天消息" placement="right" effect="light">
                <el-button :type="isHasNewMessage?'success':''" size="small" @click="onChatClick"
                           style="margin-left: 32px" round>{{ isHasNewMessage ? "文字沟通（新消息）" : "文字沟通" }}
                </el-button>
              </el-tooltip>
            </div>
          </el-col>
          <el-col :span="2">
            <div style="text-align: right">
              <el-tooltip content="挂断" placement="left" effect="light">
                <el-button size="small" type="danger" @click="onLeaveClick" circle><i
                    class="el-icon-tangohangup"></i></el-button>
              </el-tooltip>
            </div>
            <!--            <el-tooltip content="共享" placement="left" effect="light">-->
            <!--              <el-button size="small" @click="onScreenShareClick" plain><i class="el-icon-tangoscreenshare"></i>-->
            <!--              </el-button>-->
            <!--            </el-tooltip>-->
            <!--            <el-tooltip content="文字讨论" placement="left" effect="light">-->
            <!--              <el-button size="small" @click="onChatClick" plain><i class="el-icon-tangochat"></i></el-button>-->
            <!--            </el-tooltip>-->
          </el-col>
        </el-row>
      </el-footer>
    </el-container>
  </el-container>
</template>
<script>
module.exports = {
  mounted() {

    console.log('---> VideoPage mounted')

    this.isTangoEnable = this.isTangoAvailable()

    this.isGuestMode = this.$route.query.isGuestMode
    if (this.isGuestMode) {
      console.log('is GuestMode')

      this.isRecordingVisible = tangoWin.enableRecording
    } else {
      console.log('is not GuestMode')

      //接收档案模块参数
      this.recordingVideoMeg.title = `${this.$route.query.fileId}_${this.$route.query.doctorName}_${this.$route.query.patientName}_${Date.now()}`
      this.recordingVideoMeg.doctorName = this.$route.query.doctorName
      this.recordingVideoMeg.patientName = this.$route.query.patientName

      //设置录制功能可见性
      this.isRecordingVisible = tangoWin.enableRecording ||
          (this.isTangoEnable && tangoWin.curRoomKey === tango.getMyVideoRoomKey()) ||
          tangoWin.curRoomOwnerIsMine
    }


    let vm = this
    this.title = tangoWin.curRoomName

    this.openByClientNative = tangoWin.openByClient
    if (!this.openByClientNative) {
      setTimeout(() => {
        this.showLayoutSetting()
      }, 1500)
    }

    this.isLayoutSettingVisible = !tangoWin.useDefaultLayout && tangoWin.openByClient && !tangoWin.isAutoAssignRenderer

    let erd = elementResizeDetectorMaker({
      strategy: "scroll", //<- For ultra performance.
      callOnAdd: true,
      debug: false
    })
    erd.listenTo(document.getElementById("div_video"), function (element) {
      // console.log(`On div_video size changed : ${element.offsetWidth}x${element.offsetHeight}`)
      vm.syncVideoRect()
    })

    tangoWin.setVideoVisible(true, {})
    //监听视频div尺寸
    this.syncVideoRect()
    window.onresize = this.syncVideoRect


    this.clearAndPushAll(tangoWin.getParticipantsList())

    tangoWin.listeners.onParticipantAdded = function (participant) {
      vm.addParticipant(participant)
    }
    tangoWin.listeners.onParticipantRemoved = function (participant) {
      vm.removeParticipant(participant)
    }

    //初始化设备禁用状态
    tangoWin.getIODeviceState({
      success: resp => {
        this.isMuteMic = resp.data.isMuteMic
        this.isLockMuteMic = resp.data.isLockMuteMic

        this.isMuteSpeaker = resp.data.isMuteSpeaker
        this.isLockMuteSpeaker = resp.data.isLockMuteSpeaker

        this.isMuteCamera = resp.data.isMuteCamera
        this.isLockMuteCamera = resp.data.isLockMuteCamera
      },
      error: err => {
      }
    })

    if (this.isTangoEnable && tango.serverConfig.subsystemTangoDrawUrl) {
      this.isWhiteBoardAvailable = true
    } else if (this.isGuestMode && tangoWin.guest.subsystemTangoDrawUrl) {
      this.isWhiteBoardAvailable = true
    }

    // this.isParticipantsVisible = tangoWin.web_ui_in_video_show_participants

    if (this.isTangoEnable) tango.IM.socket.on('message', this.onRemoteOrderReceived)

    tangoWin.on('vidyo_connection_status_updated', this.onVideoConnectionStatusChanged)
    tangoWin.on('layout_status_updated', this.onLayoutStatusChanged)
    tangoWin.on('function_available_updated', this.onFunctionAvailableUpdated)
    tangoWin.on('function_state_updated', this.onFunctionStateUpdated)
    tangoWin.on('system_status_updated', this.onSystemStatusUpdated)
    tangoWin.on('moderation_command_received', this.onModerationCommandReceived)
    tangoWin.on('chat_message_received', this.onMessageReceived)

    //通过vidyo api获取录制状态
    wsapi.getEntityByRoomKey(
        tangoWin.curRoomKey,
        resp => {
          this.conferenceID = wsapi.find(resp, 'ns1\\:entityID')
          tangoWin.curRoomId = this.conferenceID //全局缓存roomId，用于会中呼叫323的参数
          console.log('query conference ID success:', this.conferenceID)

          this.requestRecordingState(false, state => {
            console.log(`-----> Recorder state:${state}`)
            //开始自动录制，如果设置了的话
            if (state === 0 && tangoWin.launchParams.autoRecord) {
              console.log('-----> start auto record...')
              this.onRecordingClick()
              tangoWin.launchParams.autoRecord = false;//消费掉，防止下次入会也开启
            }
          })
        },
        resp => {
          console.log('query conference ID failed', resp)
        }
    )

    this.whetherFiringAnimation()
  },
  unmounted() {
    console.log('---> VideoPage unmounted')

    // this.divObserver.disconnect()

    window.onresize = () => {
    }

    tangoWin.listeners.onParticipantAdded = function (p) {
    }
    tangoWin.listeners.onParticipantRemoved = function (p) {
    }
    this.participants.splice(0)

    tangoWin.setVideoVisible(false, {})

    if (this.isTangoEnable) tango.IM.socket.off('message', this.onRemoteOrderReceived)

    tangoWin.off('vidyo_connection_status_updated', this.onVideoConnectionStatusChanged)
    tangoWin.off('layout_status_updated', this.onLayoutStatusChanged)
    tangoWin.off('function_available_updated', this.onFunctionAvailableUpdated)
    tangoWin.off('function_state_updated', this.onFunctionStateUpdated)
    tangoWin.off('system_status_updated', this.onSystemStatusUpdated)
    tangoWin.off('moderation_command_received', this.onModerationCommandReceived)
    tangoWin.off('chat_message_received', this.onMessageReceived)
  },
  data() {
    return {
      isTangoEnable: false,
      isGuestMode: false,
      divObserver: undefined,
      useDefaultLayout: tangoWin.useDefaultLayout,
      openByClientNative: false,
      enableSetting: [],
      title: '',
      participant_list_header_class: '',
      isLayoutBtnVisible: !tangoWin.useDefaultLayout && (tangoWin.windowNumber === 1 && tangoWin.layoutInfo.curLayoutMode !== 1),
      isStartShareBtnVisible: !tangoWin.enableEndpointMode,
      isParticipantsVisible: tangoWin.web_ui_in_video_show_participants,
      isRecordingVisible: true,
      isLayoutSettingVisible: false,
      conferenceID: 0,
      participants: [],
      isLayoutModeLecture: true,
      isRecording: false,
      isSharing: tangoWin.isSharing(),
      isMuteMic: false,
      isLockMuteMic: false,
      isMuteSpeaker: false,
      isLockMuteSpeaker: false,
      isMuteCamera: false,
      isLockMuteCamera: false,
      isWhiteBoardAvailable: false,
      isActiveExit: false,
      inHandleRecording: true,
      recorderId: 0,
      isChatVisible: false,
      isHasNewMessage: false,
      dialogTranscribe: false,
      recordingVideoMeg: {
        title: "",
        patientName: "",
        doctorName: "",

      },
      isDisabledInput: true,
      startAnimation: ''
    }
  },
  methods: {
    isTangoAvailable() {
      return (typeof (tango) != 'undefined' && tango != null)
    },
    //------------------------------参会人--------------------------------------
    clearAndPushAll(participantsList) {
      this.participants.splice(0)
      participantsList.forEach(participant => {
        this.participants.push(JSON.parse(JSON.stringify(participant)))
      })
    },
    addParticipant(participant) {
      this.participants.push(JSON.parse(JSON.stringify(participant)))
    },
    removeParticipant(participant) {
      for (let i = 0; i < this.participants.length; i++) {
        if (this.participants[i].id === participant.id) {
          this.participants.splice(i, 1)
        }
      }
    },
    onRemoteOrderReceived(message) {
      if (message.msg.op === 'leave_video') {
        this.toast('您被强制请出当前房间...')
        this.onLeaveClick()
      } else if (message.msg.op === 'close_mic') {
        this.isMuteMic = true
        this.toast('您的麦克风已被管理员禁用')
      } else if (message.msg.op === 'open_mic') {
        this.isMuteMic = false
        this.toast('您的麦克风已被管理员开启')
      } else if (message.msg.op === 'close_camera') {
        this.isMuteCamera = true
        this.toast('您的摄像头已被管理员禁用')
      } else if (message.msg.op === 'open_camera') {
        this.isMuteCamera = false
        this.toast('您的摄像头已被管理员开启')
      }
    },
    onVideoConnectionStatusChanged(statusCode) {//-2 重连失败、-1 连接失败 、0 连接断开 、1 连接成功、2 重连中、3 重连成功
      if (statusCode === 0) {
        if (this.isActiveExit) {
          if (tangoWin.launchParams.leaveAndMinimizeApp) {
            this.pageBack()
            tangoWin.appMinimize({})
            return
          } else if (tangoWin.launchParams.leaveAndExitApp) {
            tangoWin.setVideoVisible(false, {})
            showLoading('正在退出程序...')
            tangoWin.appExit({})
            return
          }
        }

        this.pageBack()

      } else if (statusCode === 2) {
        this.title = '网络异常，正在重连...'
        this.participant_list_header_class = 'participant_list_header'

      } else if (statusCode === 3) {
        this.title = tangoWin.curRoomName
        this.participant_list_header_class = ''

      } else if (statusCode === -2) {
        this.title = '重连失败，正在继续尝试...'
        this.participant_list_header_class = 'participant_list_header'
      }
    },
    pageBack(){
      closeLoading()
      tangoWin.participants = {}
      this.$router.go(-1)
    },
    onLayoutStatusChanged(json) {
      console.log('onLayoutStatusChanged->', json)
      this.isLayoutBtnVisible = (tangoWin.windowNumber === 1 && tangoWin.layoutInfo.curLayoutMode !== 1)
    },
    onFunctionAvailableUpdated(jsonStr) {
      if (!tangoWin.enableEndpointMode) {
        this.isStartShareBtnVisible = tangoWin.functionAvailable.sendMinorStream
      }
    },
    onFunctionStateUpdated(jsonStr) {
      let jsonObj = JSON.parse(jsonStr)
      let recordingStateUpdated = jsonObj.data.recordingStateUpdated
      if (recordingStateUpdated) {
        this.requestRecordingState()
      }
    },
    onSystemStatusUpdated(jsonStr) {
      tangoWin.getSystemStatusJson({
        error: function () {
        },
        success: resp => {
          this.isSharing = tangoWin.isSharing()
        }
      })
    },
    onModerationCommandReceived(jsonStr) {
      console.log('onModerationCommandReceived -> ', jsonStr)
      console.log('onModerationCommandReceived -> ', JSON.parse(jsonStr))

      let command = JSON.parse(jsonStr).data
      let deviceType = command.deviceType;
      let roomModerationType = command.moderationType;
      let state = command.state;

      if (deviceType === 'DevicetypeLocalCamera') {
        if (roomModerationType === 'RoommoderationtypeHardMute') {
          if (state) {
            this.lockMuteCameraByAdmin();
          } else {
            this.unlockMuteCameraByAdmin();
          }
        } else if (roomModerationType === 'RoommoderationtypeSoftMute') {
          if (state) {
            this.muteCameraByAdmin();
          } else {
            this.unmuteCameraByAdmin();
          }
        }
      } else if (deviceType === 'DevicetypeLocalMicrophone') {
        if (roomModerationType === 'RoommoderationtypeHardMute') {
          if (state) {
            this.lockMuteMicByAdmin();
          } else {
            this.unlockMuteMicByAdmin();
          }
        } else if (roomModerationType === 'RoommoderationtypeSoftMute') {
          if (state) {
            this.muteMicByAdmin();
          } else {
            this.unmuteMicByAdmin();
          }
        }
      }
    },
    lockMuteCameraByAdmin() {
      this.isLockMuteCamera = true;
      this.isMuteCamera = true;
      this.notify("您的摄像头已被管理员禁用并锁定");
    },
    unlockMuteCameraByAdmin() {
      this.isLockMuteCamera = false;
      this.notify("您的摄像头已被管理员解除锁定");
    },
    muteCameraByAdmin() {
      this.isMuteCamera = true;
      this.notify("您的摄像头已被管理员关闭");
    },
    unmuteCameraByAdmin() {
      this.isMuteCamera = false;
      this.notify("您的摄像头已被管理员打开");
    },
    lockMuteMicByAdmin() {
      this.isLockMuteMic = true;
      this.isMuteMic = true;
      this.notify("您的麦克风已被管理员禁用并锁定");
    },
    unlockMuteMicByAdmin() {
      this.isLockMuteMic = false;
      this.notify("您的麦克风已被管理员解除锁定");
    },
    muteMicByAdmin() {
      this.isMuteMic = true;
      this.notify("您的麦克风已被管理员禁用");
    },
    unmuteMicByAdmin() {
      this.isMuteMic = false;
      this.notify("您的麦克风已被管理员打开");
    },
    notify(message) {
      ElementPlus.ElNotification({
        title: '提示',
        // dangerouslyUseHTMLString: true,
        type: 'warning',
        showClose: false,
        duration: 3000,
        position: 'bottom-left',
        message: message,
      })
    },
    toast(message) {
      ElementPlus.ElMessage({
        message: message,
        type: 'warning',
        offset: 150,
        duration: 1000
      })
    },
    //------------------------------控制视频渲染--------------------------------------
    syncVideoRect() {
      if (this.$refs.divVideo) {
        let rect = this.$refs.divVideo.getBoundingClientRect()
        // console.log(`left:${rect.left},top:${rect.top},width:${rect.width},height:${rect.height}`)
        this.refreshVideoRect(rect.left, rect.top, rect.width, rect.height)
      }
    },
    refreshVideoRect:
        debounce(
            function (left, top, width, height) {
              // console.log(`innerHeight:${window.innerHeight},clientHeight:${document.documentElement.clientHeight},offsetHeight:${document.documentElement.offsetHeight}`)
              // console.log(`refreshVideoRect(left:${left},top:${top},width:${width},height:${height})`)
              tangoWin.setVideoLocationSize(left, top, width, height, {
                success: resp => {
                }, error: resp => {
                }
              })
            },
            20,
            false),
    //------------------------------按钮点击--------------------------------------
    onLayoutModeClick() {
      this.isLayoutModeLecture = !this.isLayoutModeLecture
      tangoWin.setCustomLayoutMode(this.isLayoutModeLecture ? 2 : 0, 0, {
        isByUser: true,//用户操作会被记忆，以便变换后恢复到用户操作状态
        success: resp => {

        },
        error: resp => {

        }
      })
    },
    onParticipantsVisible() {
      this.isParticipantsVisible = !this.isParticipantsVisible
      tangoWin.web_ui_in_video_show_participants = this.isParticipantsVisible
      // this.syncVideoRect()
    },
    onMeetingControlClick() {
      if (this.isTangoEnable) {
        wsapi.getModeratorURLWithToken(
            window.btoa(tango.getMyVideoAccountName() + ':' + tango.getMyVideoAccountPassword()),
            this.conferenceID,
            resp => {
              console.log('> getModeratorURLWithToken result', resp)
              let moderatorURL = wsapi.find(resp, 'ns1\\:moderatorURL')
              console.log('> getModeratorURLWithToken moderatorURL', moderatorURL)
              if (moderatorURL) tangoWin.startUrl({
                url: moderatorURL,
              })
            },
            err => {
            })
      }
    },
    onBrowseRecordClick() {
      wsapi.getVidyoReplayLibrary(
          resp => {
            let authToken = wsapi.find(resp, 'ns1\\:authToken')
            let videoUrl = wsapi.find(resp, 'ns1\\:vidyoReplayLibraryUrl')
            tangoWin.startUrl({
              url: `${videoUrl}?token=${authToken}`,
            })
          },
          resp => {
          }
      )
    },
    onRecordingClick() {
      if (this.inHandleRecording) {
        return
      }
      this.inHandleRecording = true

      //判断是否是通过档案进会
      if (this.isRecording) {
        wsapi.stopRecording(
            this.conferenceID,
            this.recorderId,
            resp => {
              setTimeout(() => {

                this.notifyRecordingStateChanged()
                this.requestRecordingState(true)

                if (this.$route.query.bool && this.isTangoEnable && !this.isGuestMode) {
                  console.log("this.recorderId.toString().slice(4)", this.recorderId.toString().slice(4))
                  console.log(`${this.recordingVideoMeg.title}_${this.recordingVideoMeg.doctorName}_${this.recordingVideoMeg.patientName}_${Date.now()}`)
                  console.log(this.$route.query.fileId)
                  tango.requestUpdateRecordSimple(
                      this.recorderId.toString().slice(4),
                      `${this.recordingVideoMeg.title}_${this.recordingVideoMeg.doctorName}_${this.recordingVideoMeg.patientName}_${Date.now()}`,
                      this.$route.query.fileId,
                      resp => {
                        console.log(resp)
                      },
                      resp => {
                        console.log(resp)
                      }
                  )
                }
              }, 1500)
            },
            resp => {
              this.toast(`停止录制失败，请联系管理员！`)
            }
        )
      } else {
        //判断是不是通过档案中加入会议进入的会议bool进入会议的状态
        console.log("this.$route.query.bool", this.$route.query.bool)
        if (this.$route.query.bool) {
          //打开dialog关闭视频，如不这样操作视频会把dialog遮盖住
          this.dialogTranscribe = true
          // this.isDisabledInput = true
          this.inHandleRecording = false
          tangoWin.setVideoVisible(false, {})
        } else {
          wsapi.startRecording(
              this.conferenceID,
              '01',
              resp => {
                setTimeout(() => {
                  this.notifyRecordingStateChanged()
                  this.requestRecordingState()
                }, 1500)
              },
              resp => {
                let faultStr = wsapi.find(resp.responseText, 'faultstring')
                if (faultStr.startsWith('Recorder already present in the conference')) {
                  tangoWin.showDialog({
                    message: `录制失败，会议录制已经被其他参会者使用中！`,
                  })
                } else {
                  tangoWin.showDialog({
                    message: `录制失败，请联系管理员！\n${faultStr}`,
                  })
                }

                this.inHandleRecording = false
              }
          )
        }
      }
    },
    onStartRecord() {//医疗录制对话框调用
      wsapi.startRecording(
          this.conferenceID,
          '01',
          resp => {
            setTimeout(() => {
              this.notifyRecordingStateChanged()
              this.requestRecordingState()
              this.dialogTranscribe = false
              tangoWin.setVideoVisible(true, {})
            }, 1500)
          },
          resp => {
            this.toast(`开始录制失败，请联系管理员！`)
          }
      )
    },
    notifyRecordingStateChanged() {
      tangoWin.sendChatMessage({
        message: '[action]${\'action\':\'updateRecordingState\'}'
      })
    },
    onDialogDismiss() {
      //打开视频关闭弹窗
      tangoWin.setVideoVisible(true, {})
      this.dialogTranscribe = false
    },
    requestRecordingState(toastUrl, callback) {
      wsapi.getParticipants(
          this.conferenceID,
          resp => {
            console.log('requestRecordingState success', resp)

            this.recorderId = wsapi.find(resp, 'ns1\\:recorderID')
            if (this.recorderId) {

              console.log('录制状态：正在录制，RecordId；', this.recorderId)
              this.isRecording = true
              this.title = `${tangoWin.curRoomName} （录制中）`

              this.inHandleRecording = false

              if (callback) callback(1)
            } else {

              console.log('录制状态：停止')
              this.isRecording = false
              this.title = `${tangoWin.curRoomName}`

              this.inHandleRecording = false

              if (callback) callback(0)

              if (toastUrl) {
                wsapi.getVidyoReplayLibrary(
                    resp => {
                      let videoUrl = wsapi.find(resp, 'ns1\\:vidyoReplayLibraryUrl')
                      ElementPlus.ElNotification({
                        title: '查看我的录像',
                        // dangerouslyUseHTMLString: true,
                        type: 'success',
                        duration: 10000,
                        position: 'bottom-left',
                        message: `点击此处查看我的录像 （也可通过点击工具栏的查看录像）`,
                        onClick: () => {
                          tangoWin.startUrl({
                            url: videoUrl,
                          })
                        }
                      })
                    },
                    resp => {

                    }
                )
              }
            }

          },
          resp => {
            console.log('requestRecordingState failed', resp)
          }
      )
    },
    onWhiteboardClick() {
      if (this.isGuestMode && !tangoWin.guest.subsystemTangoDrawUrl) return;
      tangoWin.setVideoVisible(false, {})
      ElementPlus.ElMessageBox.confirm(
          `是否打开白板的同时，也邀请其他参会者一起参与其中？`,
          '提示',
          {
            'close-on-click-modal': true,
            distinguishCancelAndClose: true,
            cancelButtonText: '与他人一起',
            confirmButtonText: '仅自己查看',
            center: true,
          }
      )
          .then(() => {
            tangoWin.setVideoVisible(true, {})
            this.openWhiteBoardWindow(false)
          })
          .catch(action => {
            console.log('ElMessageBox.confirm click action ->', action)
            tangoWin.setVideoVisible(true, {})
            if (action === 'cancel') {
              this.openWhiteBoardWindow(true)
            }
          })
    },
    openWhiteBoardWindow(isStarter) {
      if (this.isTangoEnable) {
        tangoWin.openTangoDrawWindow({
          url: tango.serverConfig.subsystemTangoDrawUrl,
          username: tango.myAccount.name,
          room: tangoWin.curRoomKey,
          title: tangoWin.curRoomName,
          isStarter: isStarter
        })
      } else if (this.isGuestMode && tangoWin.guest.subsystemTangoDrawUrl) {
        tangoWin.openTangoDrawWindow({
          url: tangoWin.guest.subsystemTangoDrawUrl,
          username: tangoWin.guest.displayName,
          room: tangoWin.curRoomKey,
          title: tangoWin.curRoomName,
          isStarter: isStarter
        })
      }

    },
    onStartShareClick() {
      if (this.isSharing) {
        tangoWin.stopSharing({
          success: () => {
            this.isSharing = false
          },
          error: () => {

          }
        })
      } else {
        tangoWin.openStartShareWindow({
          success: function () {
          }, error: function () {
          }
        })
      }
    },
    onMuteMicClick() {
      if (this.isLockMuteMic) return;
      this.isMuteMic = !this.isMuteMic
      tangoWin.setMicPrivacy(this.isMuteMic, {})
    },
    onMuteSpeakerClick() {
      if (this.isLockMuteSpeaker) return;
      this.isMuteSpeaker = !this.isMuteSpeaker
      tangoWin.setSpeakerPrivacy(this.isMuteSpeaker, {})
    },
    onMuteCameraClick() {
      if (this.isLockMuteCamera) return;
      this.isMuteCamera = !this.isMuteCamera
      tangoWin.setCameraPrivacy(this.isMuteCamera, {})
    },
    onSettingClick() {
      tangoWin.openSettingsWindow({})
    },
    onLayoutSettingClick() {
      if (this.enableSetting.length > 0) {
        this.dismissLayoutSetting()
      } else {
        this.showLayoutSetting()
      }
    },
    showLayoutSetting() {
      tangoWin.setVideoVisible(false, {})
      this.enableSetting.push({})
    },
    dismissLayoutSetting() {
      tangoWin.setVideoVisible(true, {})
      this.enableSetting.splice(0)
    },
    onScreenShareClick() {
      this.toast(`紧张开发中...`)
    },
    onMessageReceived() {
      if (!this.isChatVisible) {
        this.isHasNewMessage = true
      }
    },
    onChatClick() {
      this.isChatVisible = !this.isChatVisible
      this.isHasNewMessage = false
    },
    onLeaveClick() {
      this.isActiveExit = true;
      showLoading()
      this.dismissLayoutSetting()
      tangoWin.leave({
        success: resp => {
          let json = JSON.parse(resp)

          if (json.code === 0) {
          } else {
            closeLoading()
            this.isActiveExit = false;
            this.toast(`退出失败，请重新尝试`)
          }
        },
        error: resp => {
          closeLoading()
          this.isActiveExit = false;
          this.toast(`退出异常，请联系管理员`)
        }
      })
    },

    whetherFiringAnimation() {
      //入会的时候获取header和滚动区域的具体宽度来做判断
      this.$nextTick(() => {
        let carouselWidth = this.$refs.carousel.offsetWidth
        let headerWidth = document.getElementById("video_header").offsetWidth
        if (carouselWidth > headerWidth) {
          //如果滚动区域的宽度大于header的宽度说明文字溢出添加动画类名
          this.startAnimation = 'marquee'
        }
      })
    }
  }
}
</script>
<style scoped>
#video_aside {
  background-color: #1a212b;
  color: var(--el-text-color-primary);
  width: 22%;
  min-width: 270px;
}

#video_header {
  background-color: #01488f;
  height: 56px;
  line-height: 56px;
  color: white;
  font-size: 18px;
  text-align: center;
  /*文本溢出强制不换行只能在一行显示*/
  white-space: nowrap;
  overflow: hidden;
}

/*定义动画*/
#video_header .marquee {
  display: inline-block;
  animation: marquee 10s linear infinite;
}

/*每一帧的动画效果*/
@keyframes marquee {
  0% {
    transform: translateX(0);
  }
  100% {
    transform: translateX(-100%);
  }
}

#video_footer {
  background-color: #1b1e21;
  height: 48px;
  line-height: 48px;
  color: white;
}

/*#container_in_video .el-main{*/
/*  padding: 0;*/
/*}*/

#main_video {
  padding: 0 !important;
  --el-main-padding: 0px;
  overflow: hidden;
}

#div_video {
  height: 100%;
  width: 100%;
}

.participant_list_header {
  background-color: #ffd230;
}

.list_item {
  padding: 8px 16px;
}

.list_item:hover {
  background-color: #6483af;
  color: white;
}
</style>