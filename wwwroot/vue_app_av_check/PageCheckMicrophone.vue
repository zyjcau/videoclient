<template>
  <el-container>
    <div id="container">
      <el-card shadow="hover">
        <el-row align="middle">
          <el-col :span="8">
            选择设备： <i class="el-icon-tangomicopen"></i>
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
            <div id="div_camera_preview">
              <el-progress :text-inside="true" :stroke-width="26" :percentage="audioLevel"></el-progress>
              <p>当您对着麦克风发出声音，您应该看到音量柱在运动</p>
            </div>
          </el-col>
        </el-row>
        <el-row align="middle" :gutter="20">
          <el-col :span="12" :offset="6">
            <div class="div_content_center">
              <el-button @click="onBackClick">上一步：测试摄像头</el-button>
              <el-button type="success" @click="onNextClick">下一步：测试扬声器</el-button>
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
    console.log('PageCheckMic mounted')
    this.init()
  },
  unmounted() {
    console.log('PageCheckMic unmounted')
    this.closeDevice()
  },
  data() {
    return {
      inUseDevice: 123,
      devices: [{deviceId: 123, label: 'test'}],
      scriptProcessor: null,
      mediaStreamTrack: null,
      isProcessing: false,
      audioLevel: 0
    }
  },
  methods: {
    init() {
      navigator.mediaDevices.enumerateDevices().then((devices) => {
        console.log(devices)
        //将设备信息存入devices
        this.devices = devices.filter((device) => {
          return device.kind === 'audioinput'
        })
        //选择第一个设备
        this.inUseDevice = this.devices[0].deviceId
        //初始化摄像头
        this.openDevice(this.inUseDevice)
      })
    },
    openDevice(deviceId) {
      console.log('openDevice', deviceId)
      this.isProcessing = true
      let audioContext = new (window.AudioContext || window.webkitAudioContext)()
      // let mediaStreamSource = null
      // let scriptProcessor = null
      navigator.mediaDevices.getUserMedia({
        audio: {deviceId: deviceId}
      }).then(stream => {
        // 获取当前操作的流
        this.mediaStreamTrack = stream.getTracks()
        // 将麦克风的声音输入这个对象
        let mediaStreamSource = audioContext.createMediaStreamSource(stream)
        // 创建一个音频分析对象，采样的缓冲区大小为4096，输入和输出都是单声道
        this.scriptProcessor = audioContext.createScriptProcessor(4096, 1, 1)
        // 将该分析对象与麦克风音频进行连接
        mediaStreamSource.connect(this.scriptProcessor)
        // 此举无甚效果，仅仅是因为解决 Chrome 自身的 bug
        this.scriptProcessor.connect(audioContext.destination)
        // 开始处理音频
        this.scriptProcessor.onaudioprocess = e => {
          if (!this.isProcessing) {
            return null
          }
          // 获得缓冲区的输入音频，转换为包含了PCM通道数据的32位浮点数组
          let buffer = e.inputBuffer.getChannelData(0)
          // 获取缓冲区中最大的音量值
          let maxVal = Math.max.apply(Math, buffer)
          // 显示音量值
          let ganLevel = Math.round(maxVal * 100) * 10
          this.audioLevel = ganLevel > 100 ? 100 : ganLevel
          // let volumeLevel = `您的音量值：${this.audioLevel}`
          // console.log(volumeLevel, this.scriptProcessor)
        }
      }).catch((error) => {
        console.log(error)
      })
    },
    closeDevice() {
      console.log('closeDevice')
      try {
        this.isProcessing = false
        if (this.scriptProcessor) {
          this.scriptProcessor.onaudioprocess = null
          this.scriptProcessor = null
        }
        this.mediaStreamTrack.forEach(track => track.stop());
      } catch (e) {
        console.log(e)
      }
    },
    onDeviceSelected(deviceId) {
      console.log('onDeviceSelected', deviceId)
      this.closeDevice()
      this.openDevice(deviceId)
    },
    onNextClick() {
      this.$router.push('/check_speaker')
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