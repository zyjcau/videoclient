<template>
  <el-container>
    <el-header>
      文字沟通
    </el-header>
    <el-main id="container_msg" ref="containerMsg">
      <div class="talk-bubble" v-for="msg in messages">
        <div :class="msg.fromMe?'talk-headertext-right':'talk-headertext-left'">
          <span>{{ msg.from + '   ' + getTimeStamp() }}</span>
        </div>
        <div :class="msg.fromMe?'talk-text-right':'talk-text-left'">
          <span :class="msg.fromMe?'self_talk tri-right right-top round':'other_talk tri-right left-top round'">
            {{ msg.content }}
          </span>
        </div>
      </div>
    </el-main>
    <el-footer id="footer_chat">
      <el-row style="height: 100%" align="middle">
        <el-col :span="18">
          <el-input
              type="textarea"
              :autosize="{ minRows: 3, maxRows: 3}"
              placeholder="请输入内容"
              resize="none"
              v-model="content">
          </el-input>
        </el-col>
        <el-col :offset="1" :span="5">
          <el-button size="small" @click="onSendChatMessageClick">发送</el-button>
        </el-col>
      </el-row>
    </el-footer>
  </el-container>
</template>
<script>
module.exports = {
  mounted() {
    console.log('module chat mounted')

    // //test code
    // this.messages.push({
    //   from: 'me',
    //   content: 'aaaa',
    //   fromMe: true
    // })
    // this.messages.push({
    //   from: 'ta',
    //   content: 'bbbbb',
    //   fromMe: false
    // })

    tangoWin.messages.forEach(message => {
      this.messages.push(message)
    })

    if (tangoWin) tangoWin.on('chat_message_received', this.onMessageReceived)
  },
  unmounted() {
    console.log('module chat unmounted')
    if (tangoWin) tangoWin.off('chat_message_received', this.onMessageReceived)
  },
  data() {
    return {
      messages: [],
      content: ''
    }
  },
  methods: {
    getTimeStamp() {
      let time = new Date();
      let hour = time.getHours() % 12;
      let minutes = time.getMinutes() < 10 ? '0' + time.getMinutes() : time.getMinutes();
      let meridies = Math.floor(time.getHours() / 12) === 1 ? "下午" : "上午";
      return meridies + ' ' + hour + ":" + minutes;
    },
    onMessageReceived(message) {
      console.log(message)
      this.messages.push(JSON.parse(message))
    },
    onSendChatMessageClick() {
      console.log(`input message : ${this.content}`)
      if (this.content.length === 0) {
        return;
      }
      tangoWin.sendChatMessage({
        message: this.content
      })
      this.messages.push({
        from: '我',
        content: this.content,
        fromMe: true
      })
      this.content = ''
      //scroll to the end of chat list
      this.scrollToEnd()
    },
    scrollToEnd(){
      setTimeout(()=>{
        this.$refs.containerMsg.scrollTop = this.$refs.containerMsg.scrollHeight;
      },0)
    },
  }
}
</script>
<style>
.el-container {
  top: 0;
  height: 100%;
  /*border-left: lightgray 1px solid;*/
}

.el-header {
  background-color: #01488f;
  height: 56px;
  line-height: 56px;
  color: white;
  font-size: 18px;
  text-align: center;
}

#container_msg {
  padding: 0 !important;
  --el-main-padding: 0px;
  /*overflow-y: scroll;*/
  /*聊天框滚动条*/
  height: 5px;
  overflow: auto;
}

#footer_chat {
  background-color: white;
  height: 140px;
  padding: 16px;
  border-top: #4b4b4b 1px solid;
  text-align: center;
}

#textarea_msg {
  width: 280px;
  height: 70px;
  padding: 5px;
  border: none;
  background: #f1f1f1;
  resize: none;
  align-content: center;
  box-sizing: border-box;
}

.talk-bubble {
  margin-top: 10px;
  margin-bottom: 10px;
  margin-left: 10%;
  margin-right: 10%;
  display: inline-block;
  position: relative;
  width: 80%;
  height: auto;
  text-align: left;
}

.talk-headertext-left {
  padding: .25em;
  text-align: left;
  line-height: 1.5em;
  font-size: 10px;
}

.talk-headertext-right {
  padding: .25em;
  text-align: right;
  /*line-height: 1.5em;*/
  font-size: 10px;
}

.talk-text-left {
  padding: 6px;
  text-align: left;
  line-height: 1.5em;
  word-break: break-all;
  background-color: #F2F2F2;
  border-radius: 10px;
  float: left;
  font-family: PingFangSC-Regular;
  font-size: 14px;
  /*color: #222222;*/
  font-weight: 400;
}

.talk-text-right {
  padding: 6px;
  /*text-align: left;*/
  line-height: 1.5em;
  word-break: break-all;
  background: #5B8DE6;
  border-radius: 10px;
  float: right;
  font-size: 14px;
}

.other_talk {
  width: auto;
  /*padding: 8px 12px;*/
  /*background-color: #393A3B;*/
  color: #222222;
}

.self_talk {
  width: auto;
  /*padding: 8px 12px;*/
  /*background-color: #5B8DE6;*/
  color: white;
}

/* Right triangle placed top right flush. */
.tri-right.border.right-top:before {
  /*content: ' ';*/
  position: absolute;
  width: 0;
  height: 0;
  left: auto;
  right: -40px;
  top: -8px;
  bottom: auto;
  border: 32px solid;
  border-color: #666 transparent transparent transparent;
}

.tri-right.right-top:after {
  /*content: ' ';*/
  position: absolute;
  width: 0;
  height: 0;
  left: auto;
  right: -15px;
  top: 10px;
  bottom: auto;
  border: 20px solid;
  border-color: #5B8DE6 transparent transparent transparent;
}

.tri-right.border.left-top:before {
  /*content: ' ';*/
  position: absolute;
  width: 0;
  height: 0;
  left: -40px;
  right: auto;
  top: -8px;
  bottom: auto;
  border: 32px solid;
  border-color: #666 transparent transparent transparent;
}

.tri-right.left-top:after {
  /*content: ' ';*/
  position: absolute;
  width: 0;
  height: 0;
  left: -17px;
  right: auto;
  top: 10px;
  bottom: auto;
  border: 22px solid;
  border-color: #393A3B transparent transparent transparent;
}

.round {
  border-radius: 6px;
  -webkit-border-radius: 6px;
  -moz-border-radius: 6px;
}


</style>