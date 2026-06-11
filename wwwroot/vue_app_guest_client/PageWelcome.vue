<template>
  <el-container>
    <div id="login_form_container">
      <div>
        <div id="login_form">
          <p>
            <img class="img_logo" :src="res.logo" alt=""/>
            <span class="p_system_name">{{ res.system_name }}</span>
          </p>
          <el-alert
              v-if="alert.content"
              :type="alert.type"
              :description="alert.content"
              show-icon
          ></el-alert>
          <el-button v-if="!isServerVisible" @click="onEditServerClick" style="width: 100%">
            <&nbsp;&nbsp;&nbsp;&nbsp;{{ server }}
          </el-button>
          <el-input style="margin-top: 8px" v-if="isServerVisible" placeholder="服务器" type="url"
                    v-model="server"></el-input>
          <el-input style="margin-top: 8px" v-if="isRoomKeyVisible" placeholder="房间号" type="text"
                    v-model="roomKey"></el-input>
          <el-input style="margin-top: 8px" v-if="isRoomPINVisible" placeholder="房间密码（如无则无需填写）"
                    type="password" v-model="roomPin"
                    @keyup.enter="onJoinClick"></el-input>
          <el-input style="margin-top: 8px" placeholder="您的姓名" type="text" v-model="displayName"
                    @keyup.enter="onJoinClick"></el-input>
          <div style="display: inline">
            <el-button @click="onAVCheckClick" style="margin-top: 24px;width: 35%">检测设备</el-button>
            <el-button type="primary" @click="onJoinClick" style="margin-top: 24px;width: 60%">加入</el-button>

          </div>

        </div>
        <div style="display: flex;justify-content:center;width: 100%;margin-top: 56px">
          <el-tooltip :content="isMuteMic?'打开麦克风':'禁用麦克风'" placement="top" effect="light">
            <el-button size="large" @click="onMuteMicClick" :type="isMuteMic?'danger':'success'" circle>
              <i :class="isMuteMic?'el-icon-tangomicmute':'el-icon-tangomicopen'"></i></el-button>
          </el-tooltip>
          <el-tooltip :content="isMuteSpeaker?'打开扬声器':'禁用扬声器'" placement="top" effect="light">
            <el-button size="large" @click="onMuteSpeakerClick" :type="isMuteSpeaker?'danger':'success'" circle>
              <i :class="isMuteSpeaker?'el-icon-tangospeakerclose':'el-icon-tangospeakeropen'"></i></el-button>
          </el-tooltip>
          <el-tooltip :content="isMuteCamera?'打开摄像头':'禁用摄像头'" placement="top" effect="light">
            <el-button size="large" @click="onMuteCameraClick" :type="isMuteCamera?'danger':'success'" circle>
              <i :class="isMuteCamera?'el-icon-tangocameramute':'el-icon-tangocameraopen'"></i></el-button>
          </el-tooltip>
        </div>
      </div>
      <div style="position: absolute;top: 0;right: 0;padding: 16px">
        <el-tooltip content="设置" placement="bottom" effect="light">
          <el-button size="large" @click="onSettingClick" circle>
            <i :class="'el-icon-tangoshezhi'"></i></el-button>
        </el-tooltip>
      </div>
      <div style="position: absolute;left: 0;top: 0;padding: 16px" v-if="isBackToLoginVisible">
        <el-button size="large" @click="onBackToLoginClick">返回到登录</el-button>
      </div>
    </div>
    <span style="position: absolute;right: 16px;bottom: 16px;font-size: 12px;color: #1b1b1b">
      {{ version }}
    </span>
  </el-container>
</template>
<script>
module.exports = {
  mounted() {

    if (tangoWin.GetQueryBooleanByDefault('fromLoginPage', false)) {
      this.isBackToLoginVisible = true
    }

    tangoWin.check({
      success: resp => {
        console.log(`Native组件：可用。 版本->${resp.data}`)
        web_version = resp.data
        this.version = web_version
        tangoWin.connect()

        tangoWin.getSystemStatusJson({
          success: resp => {

            this.version = `${this.version}_(${tangoWin.sdkVersion})`
            // this.res.logo = tangoWin.web_ui_logo
            this.res.system_name = tangoWin.appName

            this.isMuteMic = resp.data.isMuteMic
            this.isMuteSpeaker = resp.data.isMuteSpeaker
            this.isMuteCamera = resp.data.isMuteCamera

            this.server = tangoWin.guest.server
            if (!this.server) {
              this.isServerVisible = true
            }
            this.roomKey = tangoWin.launchParams.roomKey
            if (!this.roomKey) {
              this.isRoomKeyVisible = true
              this.isRoomPINVisible = true
              this.roomKey = tangoWin.guest.roomKey
            }
            this.displayName = tangoWin.guest.displayName

            //已经视频连接中时，恢复到视频页面
            if (tangoWin.isVideoConnected) {
              this.restoreVideoState()
              return
            }

            if (this.roomKey) {
              this.getAndShowRoomName()
            }

            if (tangoWin.openByClient && tangoWin.tango && tangoWin.tango.server) {
              tangoWin.checkAppVersion({
                tangoPortal: tangoWin.tango.server,
                success: version => {
                  if (version) {
                  } else {
                    this.tryAutoJoin()
                  }
                },
                failed: err => {
                  this.tryAutoJoin()
                }
              })
            } else {
              this.tryAutoJoin()
            }

          },
          error: err => {
          }
        })
      },
      error: function (resp) {
        console.log('Native组件：不可用')
        console.log(resp)
      }
    })

    tangoWin.listeners.onVideoConnectionStateUpdated = statusCode => {
      if (statusCode === 0 && !tangoWin.isVideoConnected) {
        closeLoading()
        this.$message.error('加入失败，请稍候再次尝试！')
      }
    }
  },
  unmounted() {
    tangoWin.listeners.onVideoConnectionStateUpdated = function () {
    }
  },
  data() {
    return {
      version: '',
      isBackToLoginVisible: false,
      isServerVisible: false,
      isRoomKeyVisible: false,
      isRoomPINVisible: false,
      server: '',
      roomName: '',
      roomKey: '',
      roomPin: '',
      displayName: '',
      isMuteMic: false,
      isMuteSpeaker: false,
      isMuteCamera: false,
      res: {
        system_name: '系统',
        logo: '/res/img/logo_lss.png'
      },
      alert: {
        content: '',
        type: 'warning'
      }
    }
  },
  props: {},
  methods: {
    tryAutoJoin() {
      if (isAutoJoinNecessary && tangoWin.launchAndJoin) {
        isAutoJoinNecessary = false;
        showLoading('正在自动连接中...')
        setTimeout(() => {
          this.onJoinClick(false)
        }, 800)
      }
    },
    onEditServerClick() {
      this.isServerVisible = true
    },
    onJoinClick(loading = true) {
      this.dismissAlert()

      //表单验证
      if (!this.server) {
        this.showAlert('请输入 服务器地址', 'warning')
        return;
      }
      if (!this.roomKey) {
        this.showAlert('请输入 房间号', 'warning')
        return;
      }
      if (!this.displayName) {
        this.showAlert('请输入您的 参会名称', 'warning')
        return;
      }

      showLoading('正在连接中...')

      //直接入会（不查询房间信息）
      if (tangoWin.isDirectlyJoin) {
        console.log('> Join room by directly...')
        this.invokeVideoJoin(this.server, this.displayName, this.roomName, this.roomKey, this.roomPin, () => {
        })
        return
      }

      //初始化 Vidyo api
      wsapi.setHost(this.server)
      wsapi.setAuthDirectly(tangoWin.apiCode)
      wsapi.getRoomInfo(
          this.roomKey,
          (room, resp) => {
            console.log('room', room)

            this.isRoomPINVisible = room.hasPin
            this.roomName = room.roomName
            this.res.system_name = `房间：${this.roomName}`

            this.invokeVideoJoin(this.server, this.displayName, this.roomName, room.roomKey, this.roomPin,
                () => {
                  //guest_server & guest_display_name在入会后自动存储到本地
                  tangoWin.saveConfig({
                    key: 'guest_room_key',
                    value: this.roomKey
                  })
                })
          },
          resp => {
            console.log(`getRoomInfo failed.`, resp)
            closeLoading()
            if (resp.msg) {
              ElementPlus.ElMessage({
                message: `房间号不存在！请检查！`,
                type: 'error',
              })
            } else {
              ElementPlus.ElMessage({
                message: resp.status === 401 ? `请联系管理员开通adm授权！` : `网络异常，请检查！`,
                type: 'error',
              })
            }
          }
      )
    },
    invokeVideoJoin(server, displayName, roomName, roomKey, roomPin, success) {
      let vm = this;
      tangoWin.join({
        portal: server,
        userName: '',
        password: '',
        displayName: displayName,
        roomName: `${roomName}的房间`,
        roomKey: roomKey,
        roomPin: roomPin,
        success: resp => {
          console.log(`launchVideo completed result: `, resp)
          closeLoading()
          if (resp.code === 0) {
            success()
            vm.$router.push({
              name: 'Video', query: {
                isGuestMode: true
              }
            })
          } else {
            ElementPlus.ElMessage({
              message: `加入失败，请稍后重新尝试`,
              type: 'error',
            })
          }
        },
        error: resp => {
          console.log(`launchVideo error result: `, resp)
          closeLoading()
          ElementPlus.ElMessage({
            message: `加入失败，请稍后重新尝试`,
            type: 'warning',
          })
        }
      })
    },
    restoreVideoState() {
      wsapi.setHost(tangoWin.guest.server)
      wsapi.setAuthDirectly(tangoWin.apiCode)
      // wsapi.getRoomInfo(
      //     this.roomKey,
      //     (room, resp) => {
      //       this.isRoomPINVisible = room.hasPin
      //       this.roomName = room.roomName
      //       this.res.system_name = `房间：${this.roomName}`
      //       console.log('> restoreVideoState success,room info -> ', room)
      //     },
      //     resp => {
      //       console.log(`> restoreVideoState failed.`, resp)
      //     }
      // )
      tangoWin.setVideoVisible(false,{})
      this.$router.push({
        name: 'Video', query: {
          isGuestMode: true
        }
      })
    },
    getAndShowRoomName() {
      if (this.server && this.roomKey) {
        wsapi.setHost(this.server)
        wsapi.setAuthDirectly(tangoWin.apiCode)

        wsapi.getRoomInfo(
            this.roomKey,
            (room, resp) => {
              this.isRoomPINVisible = room.hasPin
              this.roomName = room.roomName
              this.res.system_name = `房间：${this.roomName}`
              console.log('room', room)
            },
            resp => {
              console.log(`getRoomInfo failed.`, resp)
              if (resp.msg) {
                this.showAlert('房间号不存在！请检查！', 'error')
              } else {
                this.showAlert(
                    resp.status === 401 ? `请联系管理员开通adm授权！` : '网络异常，查询会议号信息失败，请检查网络！',
                    'error')
              }
            }
        )
      }
    },

    onMuteMicClick() {
      this.isMuteMic = !this.isMuteMic
      tangoWin.setMicPrivacy(this.isMuteMic, {})
    },
    onMuteSpeakerClick() {
      this.isMuteSpeaker = !this.isMuteSpeaker
      tangoWin.setSpeakerPrivacy(this.isMuteSpeaker, {})
    },
    onMuteCameraClick() {
      this.isMuteCamera = !this.isMuteCamera
      tangoWin.setCameraPrivacy(this.isMuteCamera, {})
    },
    showAlert(msg, type) {
      this.alert.content = msg
      if (type) this.alert.type = type
    },
    dismissAlert() {
      this.alert.content = ''
    },
    onSettingClick() {
      try {
        JSProxy.openSettingWindow()
      } catch (e) {
        console.log(e)
      }
    },
    onAVCheckClick() {
      try {
        JSProxy.openAVCheckWindow()
      } catch (e) {
        console.log(e)
      }
      // tangoWin.appExit({})
    },
    onBackToLoginClick() {
      // window.history.go(-1)
      location.href = '../index.html?openByClient=true&isAutoLoginNecessary=false'
    }
  }
}
</script>
<style scoped>
#login_form_container {
  width: 100vw;
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

#login_form {
  width: 300px;
  text-align: center;
}

#login_form input {
  margin-top: 6px;
  margin-bottom: 6px;
}

#login_form button {
  width: 200px;
  margin-top: 8px;
}

.p_system_name {
  margin: 16px;
  font-weight: bold;
}

.img_logo {
  /*display: inline-block;*/
  width: 48px;
  vertical-align: middle;
}

</style>