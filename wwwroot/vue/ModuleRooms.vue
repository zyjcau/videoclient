<template>
  <el-container>
    <el-aside class="el-aside_list_rooms">
      <div ref="input_search" style="padding: 8px">
        <el-row justify="center">
          <el-col :span="isCreateRoomBtnVisible?18:24">
            <el-input
                v-model="searchKeyWord"
                placeholder="搜索"
                class="input-with-select"
                @input="onSearchChanged"
                clearable
            ></el-input>
          </el-col>
          <el-col :span="6" style="text-align: center" v-if="isCreateRoomBtnVisible">
            <el-button @click="onCreateDialogShowClick">
              创建
            </el-button>
          </el-col>
        </el-row>
      </div>
      <div>
        <el-scrollbar :height="roomListHeight">
          <el-row class="list_item" align="middle" v-for="room in listForDisplay" v-bind:key="room.id"
                  @click="onItemClick(room)">
            <el-col :span="4" style="font-size: 0">
              <div class="list_item_icon">
                <el-avatar fit="contain"
                           style="border: 2px solid dodgerblue;">
                  {{ room.roomName.substring(0, 1) }}
                </el-avatar>
              </div>
            </el-col>
            <el-col :span="16">
              <div style="height: 100%;padding-left: 16px">
                <p>{{ room.roomName }}</p>
              </div>
            </el-col>
            <el-col :span="4">
              <div style="text-align: center">
                <i v-if="room.isMine||room.isFavorite"
                   :class="room.isMine?'el-icon-tangofangjian':'el-icon-tangofavorite'"></i>
                <!--            <el-tag class="mx-1" effect="dark" size="small">-->
                <!--              {{ room.roomKey }}-->
                <!--            </el-tag>-->
              </div>
            </el-col>
          </el-row>
        </el-scrollbar>
      </div>
    </el-aside>
    <el-main id="main_detail" style="padding: 20px;position: relative">
      <div id="div_detail" v-if="detail.isSelected">
        <div style="position:absolute;width:100%;height:100%;display: flex;justify-content: center;align-items: center">
          <el-card style="width: 45%">
            <el-row>
              <el-col>{{ detail.data.roomName }}</el-col>
            </el-row>
            <el-row class="my_row">
              <el-col>分机号：{{ detail.data.extension }}</el-col>
            </el-row>
            <div v-if="isJoinUrlVisible">
              浏览器加入链接：<span class="enable_select">{{ detail.webWebrtcSite }}</span>
            </div>
            <el-row justify="center" class="my_row_button">
              <el-col>
                <el-button type="primary"
                           @click="onJoinRoomClick(detail.data)">加入房间
                </el-button>
              </el-col>
            </el-row>
            <el-row justify="center" class="my_row_button">
              <el-col>
                <el-button @click="copyMeetingShareInfo(detail.data)">发起邀请
                </el-button>
              </el-col>
            </el-row>
            <el-row justify="center" class="my_row_button" v-if="isWhiteBoardAvailable">
              <el-col>
                <el-button @click="openWhiteBoard(detail.data)">查看白板
                </el-button>
              </el-col>
            </el-row>
            <el-row justify="center" class="my_row_button" v-if="detail.isMine">
              <el-col>
                <el-button @click="onMeetingControlClick">房间控制
                </el-button>
              </el-col>
            </el-row>
          </el-card>
        </div>
      </div>
      <div v-if="!detail.isSelected">选择房间查看详情</div>
      <div style="display:flex;position: absolute;top:0;right: 0;margin:36px">
        <el-checkbox
            v-model="detail.isFavorite"
            label="收藏"
            size="large"
            @change="onFavoriteChanged"
            border
            :class="detail.isDisplayFavorite?'visible':'invisible'"
        ></el-checkbox>
        <el-button type="danger" v-if="detail.isDisplayDelete" @click="onDeleteClick">
          删除
        </el-button>
      </div>
      <div style="display:flex;position:absolute;right:0;bottom:0;margin:48px">
        <el-tooltip content="禁用麦克风" placement="left" effect="light">
          <el-button size="large" @click="onMuteMicClick" :type="isMuteMic?'danger':'success'" circle>
            <i :class="isMuteMic?'el-icon-tangomicmute':'el-icon-tangomicopen'"></i></el-button>
        </el-tooltip>
        <el-tooltip content="禁用摄像头" placement="right" effect="light">
          <el-button size="large" @click="onMuteCameraClick" :type="isMuteCamera?'danger':'success'" circle>
            <i :class="isMuteCamera?'el-icon-tangocameramute':'el-icon-tangocameraopen'"></i></el-button>
        </el-tooltip>
      </div>
    </el-main>
    <el-dialog v-model="dialogFormVisible" title="创建">
      <el-form :model="form">
        <el-form-item label="名称" label-width="140px">
          <el-input v-model="form.name" autocomplete="off"></el-input>
        </el-form-item>
        <el-form-item label="模式" label-width="140px" v-if="false">
          <el-select v-model="form.modeName" placeholder="Select">
            <el-option
                v-for="item in form.modeOptions"
                :key="item.value"
                :label="item.label"
                :value="item.value"
                :disabled="item.disabled"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="主讲人" label-width="140px" v-if="form.modeName === 'lecture'">
          <el-select v-model="form.lecturerVideoId" class="m-2" placeholder="Select" filterable>
            <el-option
                v-for="item in form.lecturerCandidateList"
                :key="item.videoId"
                :label="item.name"
                :value="item.videoId"
            />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
      <span class="dialog-footer">
        <el-button @click="dialogFormVisible = false">取消</el-button>
        <el-button type="primary" @click="onCreateClick"
        >创建</el-button>
      </span>
      </template>
    </el-dialog>
    <dialog-copymeeting ref="dialogCopyMeeting" :room_key="detail.data.extension"></dialog-copymeeting>
  </el-container>
</template>
<script>

module.exports = {
  mounted() {
    // console.log('contacts mounted.')

    this.updateListHeight()
    window.onresize = this.updateListHeight

    this.isCreateRoomBtnVisible = tangoWin.web_ui_create_room_btn_visible

    //初始化设备禁用状态
    tangoWin.getIODeviceState({
      success: resp => {
        this.isMuteMic = resp.data.isMuteMic
        this.isMuteSpeaker = resp.data.isMuteSpeaker
        this.isMuteCamera = resp.data.isMuteCamera
      },
      error: err => {
      }
    })

    if (tango.serverConfig.subsystemTangoDrawUrl) {
      this.isWhiteBoardAvailable = true
    }

    // let vm = this;
    // tango.listeners.onAllOnlineRoomStateReceived = function (all) {
    //   for (let userKey in all) {
    //     // vm.updateUserState(all[userKey])
    //   }
    // }
    // tango.listeners.onOnlineRoomStateReceived = function (imUser) {
    //   // vm.updateUserState(imUser)
    // }
    // console.log('set onUserStateReceived listener.')

    this.switchToMyRoomsList()
  },
  unmounted() {
    tango.listeners.onAllUserStateReceived = function () {
    }
    tango.listeners.onUserStateReceived = function (imUser) {
    }
  },
  data() {
    return {
      roomListHeight: '80%',
      searchKeyWord: '',
      list: [],
      listForDisplay: [],
      isJoinUrlVisible: false,
      isCreateRoomBtnVisible: true,
      detail: {
        isSelected: false,
        isDisplayFavorite: false,
        isDisplayDelete: false,
        isFavorite: false,
        data: {
          extension: ''
        },
        webWebrtcSite: ''
      },
      isMuteMic: false,
      isMuteSpeaker: false,
      isMuteCamera: false,
      isWhiteBoardAvailable: false,
      dialogFormVisible: false,
      dialogCopyMeeting: false,
      form: {
        name: '',
        modeName: 'normal',
        modeOptions: [
          {
            value: 'normal',
            label: '普通模式',
          },
          {
            value: 'lecture',
            label: '主讲人模式',
          }
        ],
        lecturerCandidateList: [],
        lecturerVideoId: ''
      }
    }
  },
  methods: {
    updateListHeight() {
      let searchHeight = this.$refs.input_search.getBoundingClientRect().height
      this.roomListHeight = (document.documentElement.clientHeight - searchHeight - 52) + 'px'
      console.log('roomListHeight', this.roomListHeight)
    },
    clearAndPushAll(list) {
      this.listForDisplay.splice(0)
      list.forEach(item => {
        this.listForDisplay.push(JSON.parse(JSON.stringify(item)))
      })
    },
    switchToMyRoomsList() {
      tango.requestGetMyRooms({
        success: resp => {
          if (undefined !== resp.data && resp.data.length > 0) {
            this.list.splice(0)
            resp.data.forEach(item => {
              if (item.roomOwnerUid === tango.getMyId()) {
                item.isMine = true;
                item.isFavorite = false
                item.isDisplayFavorite = false
                item.isDisplayDelete = item.roomTypeName !== 'Personal_Major'
              } else {
                item.isMine = false
                item.isFavorite = true
                item.isDisplayFavorite = true
                item.isDisplayDelete = false
              }
              this.list.push(JSON.parse(JSON.stringify(item)))
            })
            this.clearAndPushAll(this.list)
            tango.syncAllRoomState()
          } else {
            this.listForDisplay.splice(0)
          }
        }
      })
    },
    search() {
      console.log(`search(${this.searchKeyWord})`)
      tango.requestGetRooms({
        keyword: this.searchKeyWord,
        success: resp => {
          console.log(resp);
          if (undefined !== resp.data && resp.data.length > 0) {
            resp.data.forEach(item => {
              if (item.roomOwnerUid === tango.getMyId()) {
                item.isMine = true
                item.isFavorite = false
                item.isDisplayFavorite = false
                item.isDisplayDelete = item.roomTypeName !== 'Personal_Major'
              } else {
                item.isMine = false
                item.isFavorite = this.isInMyFavorite(item)
                item.isDisplayFavorite = true
                item.isDisplayDelete = false
              }
            })
            this.clearAndPushAll(resp.data)
            tango.syncAllRoomState()
          } else {
            ElementPlus.ElMessage({
              message: `没有找到：${this.searchKeyWord}`,
              type: 'warning',
              offset: 150,
              duration: 1000
            })
          }
        },
        failed: data => {
          console.log(data);
        }
      })
    },
    isInMyFavorite: function (item) {
      for (let i = 0; i < this.list.length; i++) {
        if (this.list[i].id === item.id) {
          return true;
        }
      }
      return false;
    },
    onSearchChanged: debounce(function (keyword) {
      if (keyword === undefined || keyword === null || keyword.length === 0) {
        this.switchToMyRoomsList()
      } else {
        this.search()
      }
    }, 500, false),
    hasKeyword() {
      return this.searchKeyWord !== undefined && this.searchKeyWord !== null && this.searchKeyWord.length > 0
    },
    //-----------------------------------------------------------------------
    onItemClick(item) {
      console.log(item)
      this.detail.isSelected = true
      this.detail.isFavorite = item.isFavorite
      this.detail.isMine = item.isMine
      this.detail.isDisplayFavorite = item.isDisplayFavorite
      this.detail.isDisplayDelete = item.isDisplayDelete
      this.detail.data = JSON.parse(JSON.stringify(item))
      this.detail.webWebrtcSite = `${tangoWin.web_webrtc_site}?host=${item.videoPortalUrl}&roomKey=${item.extension}`
    },
    onJoinRoomClick(item) {
      // //test code
      // if (!tangoWin.isNativeOK) {
      //   tangoWin.isVideoConnected = true;
      //   this.$emit('video_state_changed', '1')//test code 模拟入会事件
      // }

      if (tangoWin.isVideoConnected) {
        ElementPlus.ElMessage('已经连接到其他房间')
        return;
      }

      tangoWin.curRoomOwnerIsMine = item.isMine
      launchVideo(this, item.roomKey, `${item.roomName}`)
    },
    openWhiteBoard(room) {
      tangoWin.openTangoDrawWindow({
        url: tango.serverConfig.subsystemTangoDrawUrl,
        username: tango.myAccount.name,
        room: room.roomKey,
        title: room.roomName,
        isStarter: false
      })
    },
    onMeetingControlClick() {
      wsapi.getModeratorURLWithToken(
          window.btoa(tango.getMyVideoAccountName() + ':' + tango.getMyVideoAccountPassword()),
          this.detail.data.videoId,
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
    },
    copyMeetingShareInfo(room) {
      this.$refs.dialogCopyMeeting.visible = true
    },
    onFavoriteChanged(checked) {
      console.log(checked)
      let vm = this;
      if (checked) {
        tango.requestSetMyFavoriteRoom({
          roomId: this.detail.data.id,
          groupId: 0,
          remarks: '',
          success: function (resp) {
            if (!vm.hasKeyword()) {
              vm.listForDisplay.push(vm.detail.data)
              tango.syncAllUserState()
            }
          },
          failed: function (resp) {
          }
        })
      } else {
        tango.requestDeleteMyFavoriteRoom({
          roomId: this.detail.data.id,
          success: function (resp) {
            vm.switchToMyRoomsList()
          },
          failed: function (resp) {
          }
        })
      }
    },
    onDeleteClick() {
      showLoading()
      tango.requestDeleteRoom({
        roomId: this.detail.data.id,
        success: resp => {
          closeLoading()
          this.switchToMyRoomsList()
          this.detail.isSelected = false
          this.detail.isDisplayFavorite = false
          this.detail.isDisplayDelete = false
        },
        failed: resp => {
          closeLoading()
        }
      })
    },
    onCreateDialogShowClick() {
      this.dialogFormVisible = !this.dialogFormVisible
      //初始化列表
      this.form.lecturerCandidateList.splice(0)
      this.form.lecturerCandidateList.push({name: `我 （${tango.myAccount.name}）`, videoId: tango.getMyVideoId()})
      this.form.lecturerVideoId = tango.getMyVideoId()
      tango.myContactsList.forEach(item => {
        this.form.lecturerCandidateList.push(JSON.parse(JSON.stringify(item)))
      })
      tango.searchMyUnrelativeUsers({
        keyword: '',
        success: data => {
          console.log(data);
          if (undefined !== data.data && data.data.length > 0) {
            data.data.forEach(item => {
              this.form.lecturerCandidateList.push(JSON.parse(JSON.stringify(item)))
            })
          }
        },
        failed: data => {
          console.log(data);
        }
      })
    },
    onCreateClick() {
      console.log('Create Room : ', this.form)
      tango.requestCreateRoom({
        roomName: this.form.name,
        roomKey: '',
        needCreateVideoAcct: true,
        usageMode: this.form.modeName,
        lecturer: this.form.lecturerVideoId,
        success: resp => {
          this.form.name = ''
          this.dialogFormVisible = false
          this.switchToMyRoomsList()
          ElementPlus.ElMessage({
            message: `创建成功`,
            type: 'warning',
            offset: 150,
            duration: 1000
          })
        },
        failed: resp => {
          // this.dialogFormVisible = false
          console.log('创建房间失败')
          ElementPlus.ElMessage({
            message: resp.msg,
            type: 'warning',
            offset: 150,
            duration: 1000
          })
        }
      })
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
  }
}
</script>
<style>
.el-aside_list_rooms {
  background-color: #d3dce6;
  border-radius: 5px;
  color: var(--el-text-color-primary);
  width: 30%;
  overflow: hidden;
}

#main_detail {
  background-color: #e9eef3;
  color: var(--el-text-color-primary);
  text-align: center;
  line-height: 160px;
  height: 100%;
}

#div_detail {
  position: relative;
  height: 100%;
}

.list_item {
  padding: 8px 16px;
}

.list_item_icon {
  text-align: center;
}

.list_item:hover {
  background-color: #5ea6ff;
  color: white;
}

.list_item p {
  margin: 0;
  font-size: 12px;
  line-height: normal;
}

.my_row {
  margin-bottom: 20px;

  &:last-child {
    margin-bottom: 0;
  }
}

.my_row_button {
  margin-bottom: 0;

  &:last-child {
    margin-bottom: 0;
  }
}

</style>