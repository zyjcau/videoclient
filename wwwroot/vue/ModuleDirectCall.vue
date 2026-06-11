<template>
  <el-container>
    <el-aside class="el-aside_call">
      <div id="div_call">
        <span>输入一个 IP 或 SIP 地址以便通话</span>
        <el-input
            v-model="callNumber"
            placeholder="SIP/H323号码"
            class="input-with-select"
        >
        </el-input>
        <el-button type="primary" @click="call">
          呼叫
        </el-button>
      </div>
    </el-aside>
    <el-main>
      <el-container style="padding: 16px">
        <el-button><i class="el-icon-tangolianxiren"></i> 通讯录 紧张开发中</el-button>
      </el-container>
    </el-main>
  </el-container>
</template>
<script>
module.exports = {
  data() {
    return {
      callNumber: ''
    }
  },
  methods: {
    call() {
      console.log('> call sip/h323 -> ', this.callNumber)
      if (tangoWin.isVideoConnected) {
        tango.vs_inviteSIP323({
          callNumber: this.callNumber,
          roomId: tangoWin.curRoomId,
          success: resp => {
            console.log('> call sip/h323 result ->', resp)
            this.toast(resp.msg)
          }, failed: function (resp) {
            console.log(resp)
          }
        })
      } else {
        tango.vs_inviteSIP323({
          callNumber: this.callNumber,
          roomId: tango.getMyVideoId(),
          success: resp => {
            console.log('> call sip/h323 result ->', resp)
            if (resp.code === 0) {
              launchVideo(this, tango.getMyVideoRoomKey(), `${tango.myAccount.name} 的房间`)
            } else if (resp.code === -1) {
              this.toast('呼叫失败，请检查输入是否正确！')
            } else {
              this.toast(resp.msg)
            }
          }, failed: function (resp) {
            console.log(resp)
          }
        })
      }
    },
    toast(message) {
      ElementPlus.ElMessage({
        message: message,
        type: 'warning',
        offset: 150,
        duration: 1000
      })
    }
  }
}
</script>
<style>
.el-aside_call {
  background-color: #d3dce6;
  border-radius: 5px;
  color: var(--el-text-color-primary);
  width: 30%;
}

#div_call {
  padding: 16px;
  line-height: 48px;
}
</style>