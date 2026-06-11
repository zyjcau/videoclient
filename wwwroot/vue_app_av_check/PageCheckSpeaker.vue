<template>
  <el-container>
    <div id="container">
      <el-card shadow="hover">
        <el-row align="middle">
          <el-col :span="8">
            选择设备： <i class="el-icon-tangospeakeropen"></i>
          </el-col>
          <el-col :span="16">
            <el-select v-model="inUseDevice" class="m-2" placeholder="Select"
                       @change="onDeviceSelected">
              <el-option
                  v-for="item in devices"
                  :key="item.deviceId"
                  :label="item.label"
                  :value="item.deviceId"
              >
              </el-option>
            </el-select>
          </el-col>
        </el-row>
        <el-row align="middle">
          <el-col :span="24">
            <div class="div_content_center">
              <audio id="audio_speaker" ref="audio_speaker"
                     src="test_speaker.mp3"
                     controls=""
                     autoplay="autoplay"
                     loop="loop"
                     class="audio"></audio>
            </div>
          </el-col>
        </el-row>
        <el-row align="middle">
          <el-col>
            <p>如果听不到声音，请切换设备或者检查设备是否正常</p>
          </el-col>
        </el-row>
        <el-row align="middle" :gutter="20">
          <el-col :span="12" :offset="6">
            <div class="div_content_center">
              <el-button @click="onBackClick">上一步：测试麦克风</el-button>
              <el-button type="success" @click="onNextClick">完成所有测试</el-button>
            </div>
          </el-col>
        </el-row>
      </el-card>
    </div>
  </el-container>
</template>
<script>
module.exports = {
  mounted() {
    console.log('PageCheckSpeaker mounted')
    this.init()
  },
  unmounted() {
    console.log('PageCheckSpeaker unmounted')
    this.closeDevice()
  },
  data() {
    return {
      inUseDevice: 123,
      devices: [{deviceId: 123, label: 'test'}]
    }
  },
  methods: {
    init() {
      navigator.mediaDevices.enumerateDevices().then((devices) => {
        console.log(devices)
        //将设备信息存入devices
        this.devices = devices.filter((device) => {
          return device.kind === 'audiooutput'
        })
        //选择第一个设备
        this.inUseDevice = this.devices[0].deviceId
        //初始化摄像头
        this.openDevice(this.inUseDevice)
      })
    },
    openDevice(deviceId) {
      setTimeout(() => {
        // this.$refs.audio_speaker.play()
      }, 1000)
      // this.$refs.audio_speaker.play()
    },
    closeDevice() {
      // this.$refs.audio_speaker.pause()
    },
    onDeviceSelected(deviceId) {
      this.$refs.audio_speaker.setSinkId(deviceId)
    },
    onNextClick() {
      showLoading('已完成所有测试，正在关闭窗口...')
      window.close()
    },
    onBackClick() {
      this.$router.go(-1)
    }
  }
}
</script>
<style scoped>
.el-row {
  margin-bottom: 20px;

  &:last-child {
    margin-bottom: 0;
  }
}

#container {
  width: 100vw;
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

.div_content_center {
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>