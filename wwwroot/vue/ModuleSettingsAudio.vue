<template>
  <div>
    <el-card shadow="hover">
      <el-row align="middle">
        <el-col :span="4">麦克风</el-col>
        <el-col :span="20">
          <el-select v-model="microphone.inUse" class="m-2" placeholder="Select"
                     @change="onMicrophoneSelected">
            <el-option
                v-for="item in microphone.list"
                :key="item"
                :label="item"
                :value="item"
            >
            </el-option>
          </el-select>
        </el-col>
      </el-row>
    </el-card>
    <el-card shadow="hover" style="margin-top: 16px">
      <el-row align="middle">
        <el-col :span="4">扬声器</el-col>
        <el-col :span="20">
          <el-select v-model="speaker.inUse" class="m-2" placeholder="Select"
                     @change="onSpeakerSelected">
            <el-option
                v-for="item in speaker.list"
                :key="item"
                :label="item"
                :value="item"
            >
            </el-option>
          </el-select>
        </el-col>
      </el-row>
    </el-card>
  </div>
</template>
<script>
module.exports = {
  mounted() {
    console.log('settings audio mounted')
    // if (tangoWin.isNativeOK) {
    //   this.microphone = JSON.parse(JSON.stringify(tangoWin.microphone))
    //   this.speaker = JSON.parse(JSON.stringify(tangoWin.speaker))
    // }
    if (tangoWin.isNativeOK) {
      this.getAndRefresh()
      tangoWin.IM.socket.on('system_status_updated', this.statusUpdated)
    }
  },
  unmounted() {
    tangoWin.IM.socket.off('system_status_updated', this.statusUpdated)
  },
  data() {
    return {
      microphone: {},
      speaker: {}
    }
  },
  methods: {
    statusUpdated(jsonStr) {
      console.log('statusUpdated', jsonStr)
      this.getAndRefresh()
    },
    getAndRefresh() {
      tangoWin.getSystemStatusJson({
        success: resp => {
          this.microphone = JSON.parse(JSON.stringify(tangoWin.microphone))
          this.speaker = JSON.parse(JSON.stringify(tangoWin.speaker))
        },
        error: resp => {
        }
      })
    },
    // clearAndPushAll(cameraList) {
    //   this.cameraData.splice(0)
    //   cameraList.forEach(camera => {
    //     if (camera.enable === true) {
    //       this.cameraData.push(JSON.parse(JSON.stringify(camera)))
    //     }
    //   })
    // },
    onMicrophoneSelected(val) {
      if (tangoWin.isNativeOK) tangoWin.assignMicrophone(val,
          {
            success: function (resp) {
              console.log(resp)
              tangoWin.microphone.inUse = val
            },
            error: function (resp) {
              console.log(resp)
            }
          }
      )
    },
    onSpeakerSelected(val) {
      if (tangoWin.isNativeOK) tangoWin.assignSpeaker(val,
          {
            success: function (resp) {
              console.log(resp)
              tangoWin.speaker.inUse = val
            },
            error: function (resp) {
              console.log(resp)
            }
          }
      )
    }
  }
}
</script>
<style>

</style>