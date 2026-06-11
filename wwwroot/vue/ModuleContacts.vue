<template>
  <el-container>
    <el-aside class="el-aside_list_contacts">
      <div ref="input_search" style="padding: 8px">
        <el-input
            v-model="searchKeyWord"
            placeholder="搜索"
            class="input-with-select"
            @input="onSearchChanged"
            clearable
        >
        </el-input>
      </div>
      <div>
        <el-scrollbar :height="roomListHeight">
          <el-row class="list_item" align="middle" v-for="contact in listForDisplay" v-bind:key="contact.id"
                  @click="onContactClick(contact)">
            <el-col :span="4" style="font-size: 0">
              <div class="list_item_icon">
                <el-avatar v-if="contact.state === 'free'"
                           fit="contain"
                           style="border: 2px solid green;">
                  {{ contact.name.substring(0, 1) }}
                </el-avatar>
                <el-avatar v-if="contact.state === 'offline' || contact.state === undefined"
                           fit="contain"
                           style="border: 2px solid grey;">
                  {{ contact.name.substring(0, 1) }}
                </el-avatar>
                <el-avatar v-if="contact.state === 'in_call' || contact.state === 'called_in'"
                           fit="contain"
                           style="border: 2px solid orangered;">
                  {{ contact.name.substring(0, 1) }}
                </el-avatar>
              </div>
            </el-col>
            <el-col :span="16">
              <div style="height: 100%;padding-left: 16px">
                <p>{{ contact.name }}</p>
                <p>{{ contact.department_name }}</p>
              </div>
            </el-col>
            <el-col :span="4">
              <div>
                <el-tag class="mx-1" effect="dark" size="small" :type="getContactStateColor(contact)">
                  {{ getContactStateName(contact) }}
                </el-tag>
              </div>
              <!--          <i class="el-icon-tangofangjian"></i>-->
            </el-col>
            <!--        <el-divider></el-divider>-->
          </el-row>
        </el-scrollbar>
      </div>
      <!--      <ElMessageBox showClose="true">asdasd</ElMessageBox>-->
    </el-aside>
    <el-main id="main_detail" style="padding: 20px;position: relative">
      <div id="div_detail" v-if="detail.isSelected">
        <div style="text-align: right">
          <el-checkbox
              v-model="detail.isFavorite"
              label="收藏"
              size="large"
              @change="onFavoriteChanged"
              border
              :class="!isFavoriteBtnVisible||detail.contact.isSelf?'invisible':'visible'"
          ></el-checkbox>
        </div>
        <div>
          {{ detail.contact.name }}
        </div>
        <div>
          {{ detail.contact.company_name }} - {{ detail.contact.department_name }}
        </div>
        <div>
          分机号：{{ detail.contact.extension }}
        </div>
        <div v-if="isJoinUrlVisible">
          浏览器加入链接：<span class="enable_select">{{ detail.webWebrtcSite }}</span>
        </div>
        <div id="div_button" style="margin-top: 16px">
          <span v-if="!detail.contact.isSelf">
            <el-button type="primary" @click="onCallClick(detail.contact)" :disabled="detail.isDisabledCall">
              呼叫
            </el-button>
          </span>
          <span v-if="isJoinBtnVisible">
            <el-button style="margin-left: 16px" type="primary"
                       @click="onJoinRoomClick(detail.contact)">加入到他的房间</el-button>
          </span>
        </div>
      </div>
      <div v-if="!detail.isSelected">选择联系人查看详情</div>
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
  </el-container>
</template>
<script>

module.exports = {
  mounted() {
    // console.log('contacts mounted.')

    this.isJoinBtnVisible = tangoWin.web_ui_contact_detail_join_btn_visible
    this.isFavoriteBtnVisible = tangoWin.web_ui_module_contacts_favorite_btn_visible

    //初始化设备禁用状态
    tangoWin.getIODeviceState({
      success: resp => {
        this.isMuteMic = resp.data.isMuteMic
        this.isMuteSpeaker = resp.data.isMuteSpeaker
        this.isMuteCamera = resp.data.isMuteCamera
      },
      error: err => {}
    })

    this.assignMyContactsByLaunchParam = tangoWin.launchParams.myContacts

    this.updateListHeight()
    window.onresize = this.updateListHeight

    let vm = this;
    tango.listeners.onAllUserStateReceived = function (all) {
      for (let userKey in all) {
        vm.updateUserState(all[userKey])
      }
    }
    tango.listeners.onUserStateReceived = function (imUser) {
      vm.updateUserState(imUser)
    }
    console.log('set onUserStateReceived listener.')

    this.switchToMyContactsList()
  },
  unmounted() {
    tango.listeners.onAllUserStateReceived = function () {
    }
    tango.listeners.onUserStateReceived = function (imUser) {
    }
  },
  data() {
    return {
      isFavoriteBtnVisible: true,
      assignMyContactsByLaunchParam: undefined,
      roomListHeight: '80%',
      searchKeyWord: '',
      listForDisplay: [],
      isJoinUrlVisible: false,
      isJoinBtnVisible: false,
      detail: {
        isSelected: false,
        isFavorite: false,
        isDisabledCall: true,
        contact: {},
        webWebrtcSite: ''
      },
      isMuteMic: false,
      isMuteSpeaker: false,
      isMuteCamera: false,
    }
  },
  methods: {
    updateListHeight() {
      let searchHeight = this.$refs.input_search.getBoundingClientRect().height
      this.roomListHeight = (document.documentElement.clientHeight - searchHeight - 52) + 'px'
      console.log('roomListHeight', this.roomListHeight)
    },
    clearAndPushAll(contactsList) {
      this.listForDisplay.splice(0)
      contactsList.forEach(user => {
        this.listForDisplay.push(JSON.parse(JSON.stringify(user)))
      })
    },
    switchToMyContactsList() {
      if (this.assignMyContactsByLaunchParam) {
        tango.searchUsersById({
          ids: this.assignMyContactsByLaunchParam,
          success: resp => {
            this.clearAndPushAll(resp.data)
            tango.syncAllUserState()
            this.addMyToList()
          },
          failed: resp => {

          }
        })
      } else {
        this.clearAndPushAll(tango.myContactsList)
        tango.syncAllUserState()
        this.addMyToList()
      }
    },
    addMyToList() {
      //添加账号本人到列表
      let me = JSON.parse(JSON.stringify(tango.myAccount))
      me.name = `${me.name} (我)`
      me.isSelf = true//用于区分其他联系人
      this.listForDisplay.unshift(me)
    },
    search() {
      console.log(`search(${this.searchKeyWord})`)
      tango.searchMyUnrelativeUsers({
        keyword: this.searchKeyWord,
        success: data => {
          console.log(data);
          if (undefined !== data.data && data.data.length > 0) {
            this.clearAndPushAll(data.data)
            tango.syncAllUserState()
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
    onSearchChanged: debounce(function (keyword) {
      if (keyword === undefined || keyword === null || keyword.length === 0) {
        this.switchToMyContactsList()
      } else {
        this.search()
      }
    }, 500, false),
    hasKeyword() {
      return this.searchKeyWord !== undefined && this.searchKeyWord !== null && this.searchKeyWord.length > 0
    },
    updateUserState(imUser) {
      console.log('updateUserState')
      console.log(imUser)
      this.listForDisplay.forEach(user => {
        if (user.id === imUser.uid) {
          //刷新列表缓存数据
          user.state = imUser.state
          user.userKey = imUser.userKey
          user.platType = imUser.platType
          if (this.detail.isSelected && this.detail.contact.id === user.id) {
            //刷新详情缓存数据
            this.detail.contact.state = imUser.state
            this.detail.contact.userKey = imUser.userKey
            this.detail.contact.platType = imUser.platType
            this.detail.isDisabledCall = (user.state !== 'free')
            // console.log(`刷新详情:${JSON.stringify(this.detail.contact)}`)
          }
        }
      })
    },
    getContactStateColor(contact) {
      if (contact.state === 'free') {
        return 'success'
      } else if (contact.state === 'in_call') {
        return 'danger'
      } else if (contact.state === 'called_in') {
        return 'warning'
      } else if (contact.state === 'calling') {
        return 'warning'
      } else {
        return 'info'
      }
    },
    getContactStateName(contact) {
      return tango.getContactStateName(contact)
    },
    //-----------------------------------------------------------------------
    onContactClick(contact) {
      // console.log(contact)
      this.detail.isSelected = true;
      this.detail.isFavorite = tango.isInMyContact(contact)
      this.detail.isDisabledCall = (contact.state !== 'free')
      this.detail.contact = JSON.parse(JSON.stringify(contact))
      this.detail.webWebrtcSite = `${tangoWin.web_webrtc_site}?host=${contact.videoPortalUrl}&roomKey=${contact.extension}`
    },
    onJoinRoomClick(contact) {
      // //test code
      // if (!tangoWin.isNativeOK) {
      //   tangoWin.isVideoConnected = true;
      //   this.$emit('video_state_changed', '1')//test code 模拟入会事件
      // }

      if (tangoWin.isVideoConnected) {
        ElementPlus.ElMessage('已经连接到其他房间')
        return;
      }

      launchVideo(this, contact.videoRoomKey, `${contact.name} 的房间`)
    },
    onCallClick(contact) {
      if (tangoWin.isVideoConnected) {
        tango.sendCallMessage(tangoWin.curRoomKey, contact)
      } else {
        tango.sendCallMessage(tango.getMyVideoRoomKey(), contact)
        launchVideo(this, tango.getMyVideoRoomKey(), `${tango.myAccount.name} 的房间`)
      }
    },
    onFavoriteChanged(checked) {
      console.log(checked)
      let vm = this;
      if (checked) {
        tango.setMyUserRelation({
          relationUser: this.detail.contact,
          success: function (resp) {
            if (!vm.hasKeyword()) {
              vm.listForDisplay.push(vm.detail.contact)
              tango.syncAllUserState()
              // tango.requestMyContacts({
              //   success: function () {
              //     tango.syncAllUserState()
              //   },
              //   failed: function () {
              //   }
              // })
            }
          },
          failed: function (resp) {
          }
        })
      } else {
        tango.deleteMyUserRelation({
          relationUserId: this.detail.contact.id,
          success: function (resp) {
            vm.switchToMyContactsList()
          },
          failed: function (resp) {
          }
        })
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
  }
}
</script>
<style>
.el-aside_list_contacts {
  background-color: #d3dce6;
  border-radius: 5px;
  color: var(--el-text-color-primary);
  width: 30%;
}

#main_detail {
  background-color: #e9eef3;
  color: var(--el-text-color-primary);
  text-align: center;
  line-height: 160px;
  height: 100%;
}

#div_detail {
  line-height: 48px;
}

#div_button .el-button {
  /*display: inline-block;*/
  margin-left: 16px;
  width: 150px;
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

</style>