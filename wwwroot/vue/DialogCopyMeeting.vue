<template>
  <el-dialog v-model="visible" title="邀请其他人加入讨论" width="30%" center>
    <el-row class="my_row">
      <el-col>
        <el-input placeholder="主题" type="text" v-model="meeting_subject"></el-input>
      </el-col>
    </el-row>
    <el-row class="my_row">
      <el-col>
        <el-input placeholder="内容" type="textarea" :autosize="{ minRows: 2, maxRows: 6}"
                  v-model="meeting_content"></el-input>
      </el-col>
    </el-row>
    <el-row class="my_row">
      <el-col>
        <el-date-picker
            v-model="meeting_datetime"
            type="datetime"
            placeholder="选择日期时间"
            align="right">
        </el-date-picker>
      </el-col>
    </el-row>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="visible = false">取消</el-button>
        <el-button type="primary" @click="copy">
          复制邀请信息
        </el-button>
      </span>
    </template>
  </el-dialog>
</template>
<script>
module.exports = {
  beforeMount() {
    // this.visible = this.show
  },
  mounted() {
    console.log('dialog answer mounted')
  },
  props: {
    room_key: {
      type: String,
      required: true
    }
  },
  data() {
    return {
      visible: false,
      meeting_subject: '',
      meeting_content: '',
      meeting_datetime: '',

    }
  },
  methods: {
    copy() {
      //复制到剪切板的问题部分资料：https://magpcss.org/ceforum/viewtopic.php?t=18903
      tangoWin.copyToClipboard({
        content: this.generateInviteMessage(),
        success: () => {
          this.clearForm()
          ElementPlus.ElMessage({
            message: '复制成功',
            type: 'success',
            offset: 150,
            duration: 1000
          })
        },
        error: () => {
          ElementPlus.ElMessage({
            message: '复制失败',
            type: 'error',
            offset: 150,
            duration: 1000
          })
        }
      })
      this.visible = false
    },
    generateInviteMessage() {
      let
          info = `会议主题：${this.meeting_subject}\n`;
      info += `会议内容：${this.meeting_content}\n`;
      info += `会议时间：${this.meeting_datetime}\n`;
      info += `会议号：${this.room_key}\n`;
      info += `会议密码：空（无需填写）\n`;
      info += `请点击此链接参会：${this.getInviteUrl(false, tango.getMyVideoPortal(), this.room_key)}`;
      // info += '\n\t浏览器方式：${getInviteUrl(true, Tango.getMyVideoPortalUrl(), room.extension)}';
      return info;
    },
    getInviteUrl(isWebrtcMode, portal, roomKey) {
      //设置一个默认的网页端地址，如果tango服务器没有配置
      let serverUrl = 'https://webrtc.lssvc.cn';
      //获取本网页端根地址
      if (tango.serverConfig.webClientUrl) {
        serverUrl = tango.serverConfig.webClientUrl;
      }
      if (isWebrtcMode) {
        return `${serverUrl}?host=${portal}&roomKey=${roomKey}`;
      } else {
        let param =
            `enableGuestMode=true&welcomePage=false&portal=${portal}&host=${portal}&roomKey=${roomKey}`;
        return `${serverUrl}/guide?p=${window.btoa(param)}`;
      }
    },
    clearForm() {
      this.meeting_subject = ''
      this.meeting_content = ''
      this.meeting_datetime = ''
    }
  }
}
</script>
<style>
.my_row {
  margin-bottom: 16px;

  &:last-child {
    margin-bottom: 0;
  }
}
</style>