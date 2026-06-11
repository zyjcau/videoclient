<template>
  <el-container style="overflow: hidden">
    <el-header v-if="!isVideoConnected" id="header_main">
      <img src="../res/img/logo_lss.png" style="height: 28px;vertical-align: middle;margin-right: 8px">
      {{ appName }}
    </el-header>
    <el-container>
      <el-aside class="el-aside_menu">
        <el-menu
            :default-active="activeIndex"
            @select="onMenuSelected"
            class="el-menu-vertical-demo"
            collapse
            router>
          <el-menu-item index="/main/contacts" v-if="contactsVisible">
            <i class="el-icon-tangolianxiren"></i>
            <template #title>联系人</template>
          </el-menu-item>
          <el-menu-item index="/main/rooms" v-if="roomsVisible">
            <i class="el-icon-tangofangjian"></i>
            <template #title>{{ roomLabelName }}</template>
          </el-menu-item>
          <el-menu-item index="/main/direct_call" v-if="callVisible">
            <i class="el-icon-tangocall"></i>
            <template #title>呼叫</template>
          </el-menu-item>
          <el-menu-item index="/main/settings">
            <i class="el-icon-tangoshezhi"></i>
            <template #title>设置</template>
          </el-menu-item>
          <el-menu-item index="/main/files" v-if="false">
            <i class="el-icon-tangoarchives"></i>
            <template #title>档案</template>
          </el-menu-item>
          <el-menu-item index="/main/correct" v-if="archivesVisible">
            <i class="el-icon-tangoarchives"></i>
            <template #title>审批</template>
          </el-menu-item>
          <el-menu-item index="/main/video" :disabled="!isVideoConnected">
            <i class="el-icon-tangovideo_comm"></i>
            <template #title>通话</template>
          </el-menu-item>
        </el-menu>
      </el-aside>
      <el-main>
        <router-view style="height: 100%"></router-view>
      </el-main>
    </el-container>
  </el-container>
</template>
<script>

module.exports = {
  mounted() {
    console.log('main mounted')

    this.appName = tangoWin.appName

    //-----根据app模块可见配置显示不同菜单
    this.contactsVisible = tangoWin.web_ui_module_contacts_visible
    this.roomsVisible = tangoWin.web_ui_module_rooms_visible
    this.callVisible = tangoWin.web_ui_module_callsip323_visible
    this.archivesVisible = tangoWin.web_ui_module_profile_visible
    if (this.contactsVisible) {
      this.activeIndex = '/main/contacts'
    } else if (this.roomsVisible) {
      this.activeIndex = '/main/rooms'
    } else if (this.callVisible) {
      this.activeIndex = '/main/direct_call'
    } else if (this.archivesVisible) {
      this.activeIndex = '/main/files'
    } else {
      this.activeIndex = '/main/settings'
    }
    console.log(`default -> ${this.activeIndex}`)
    this.roomLabelName = tangoWin.web_ui_room_label_display_name
    // this.activeIndex = this.$route.matched[0].path || '/main/contacts'

    //-----同步视频状态，视频中时点亮通话按钮
    this.isVideoConnected = tangoWin.isVideoConnected
    tangoWin.listeners.onVideoConnectionStateUpdated = statusCode => {
      this.isVideoConnected = tangoWin.isVideoConnected
      tango.IM.socket.emit('sync', {//同步加入退出tango房间
        op: tangoWin.isVideoConnected ? 'act_join' : 'act_leave',
        param: {
          roomKey: tangoWin.curRoomKey,
          videoUserId: tango.getMyVideoId(),
          isIgnoreConstraintOnJoined: true
        }
      })
      ////实现未连接视频时，禁用所有设备
      // tangoWin.setCameraPrivacy(
      //     this.isVideoConnected && !tangoWin.isMuteCamera ? 'false' : 'true',
      //     {})
      // tangoWin.setMicPrivacy(
      //     this.isVideoConnected && !tangoWin.isMuteMic ? 'false' : 'true',
      //     {})
      // tangoWin.setSpeakerPrivacy(
      //     this.isVideoConnected && !tangoWin.isMuteSpeaker ? 'false' : 'true',
      //     {})
    }

    //-----监听Tango系统消息，实现响铃接听、受控等
    tango.IM.socket.on('message', message => {
      console.log(message)
      if (message.msg.op === 'join_video') {
        this.answerMessage = message
        if (tangoWin.isVideoConnected) {
          ElementPlus.ElNotification({
            title: '提示',
            message: `${message.senderUserName} 正在尝试呼叫你`,
            type: 'warning',
          })
        } else {
          if (tangoWin.tango.isAutoAnswer) {
            launchVideo(this, message.msg.roomKey, `${message.senderUserName} 的房间`)
          } else {
            //show the dialog
            ElementPlus.ElMessageBox.confirm(
                `收到 ${message.senderUserName} 的呼叫`,
                '通知',
                {
                  'close-on-click-modal': false,
                  cancelButtonText: '拒绝',
                  confirmButtonText: '接听',
                  center: true
                }
            )
                .then(() => {
                  tangoWin.stopAudio()
                  launchVideo(this, message.msg.roomKey, `${message.senderUserName} 的房间`)
                })
                .catch(() => {
                  tangoWin.stopAudio()
                })
            //make app foreground
            tangoWin.appActivate({})
            //play ringtone
            tangoWin.playAudio()
          }
        }
      } else if (message.msg.op === 'leave_video') {
        tangoWin.leave({
          success: resp => {
          }, error: resp => {
          }
        })
      } else if (message.msg.op === 'close_mic') {
        tangoWin.setMicPrivacy(true, {})
      } else if (message.msg.op === 'open_mic') {
        tangoWin.setMicPrivacy(false, {})
      } else if (message.msg.op === 'close_camera') {
        tangoWin.setCameraPrivacy(true, {})
      } else if (message.msg.op === 'open_camera') {
        tangoWin.setCameraPrivacy(false, {})
      }
    })
    tango.listeners.onVideoConstraintReceived = constraint => {
      console.log('onVideoConstraintReceived', constraint)
      // if (tangoWin.isIgnoreFirstConstraint) {
      //   tangoWin.isIgnoreFirstConstraint = false;
      //   return;
      // }
      syncLayoutMode(constraint)
    }
    tango.listeners.onOnlineRoomStateReceived = room => {
      console.log('onOnlineRoomStateReceived', room)
    }

    //-----实现启动软件时直接入会
    if (tangoWin.launchAndJoin) {
      showLoading()
      setTimeout(() => {
        this.invokeAutoJoin(tangoWin.launchParams.roomKey)
      }, 1000)
    }

    //-----实现检测软件版本提示更新
    tangoWin.checkAppVersion({
      tangoPortal: tango.host,
    })
  },
  unmounted() {
    tangoWin.listeners.onVideoConnectionStateUpdated = function () {
    }
    // tango.IM.socket.off('message')
  },
  data() {
    return {
      contactsVisible: true,
      roomsVisible: true,
      callVisible: true,
      settingVisible: true,
      archivesVisible: true,
      appName: '',
      roomLabelName: '',
      activeIndex: '/main/contacts',
      isVideoConnected: false,
      isAnswerDialogVisible: false,
      answerMessage: {}
    }
  },
  watch: {
    $route(to, from) {
      this.onMenuSelected(to.path)
    }
  },
  methods: {
    invokeAutoJoin(roomKey) {
      wsapi.getRoomInfo(
          roomKey,
          (room, resp) => {
            launchVideo(this, room.roomKey, room.roomName)
          },
          resp => {
            closeLoading()
            ElementPlus.ElMessage({
              message: `房间号不存在！自动加入会议失败，请联系管理员！`,
              type: 'error',
            })
          }
      )
    },
    onMenuSelected(path) {
      // console.log(`onMenuSelected(${path})`)
      this.activeIndex = path;
    }
  }
}
</script>
<style scoped>
#header_main {
  background-color: #1386ee;
  height: 52px;
  text-align: center;
  line-height: 52px;
  color: white;
}

.el-aside_menu {
  /*background-color: #d3dce7;*/
  color: var(--el-text-color-primary);
  text-align: center;
  line-height: 200px;
  width: auto;
  height: 100%;
}

.el-main {
  background-color: #f7fafc;
  color: var(--el-text-color-primary);
  padding: 0;
  /*text-align: center;*/
  /*line-height: 160px;*/
}
</style>