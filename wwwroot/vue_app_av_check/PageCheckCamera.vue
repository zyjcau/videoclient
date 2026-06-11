<template>
  <el-container>
    <div id="container">
      <el-card shadow="hover">
        <el-row align="middle">
          <el-col :span="8">
            选择设备：<i class="el-icon-tangocameraopen" style="margin-right: 16px"></i>
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
            <div id="div_camera_preview" v-loading="isLoading">
              <video id="video_component" ref="video"></video>
            </div>
          </el-col>
        </el-row>
        <el-row align="middle" :gutter="20">
          <el-col :span="12" :offset="6">
            <div class="div_content_center">
              <!--              <el-button @click="onBackClick">上一步</el-button>-->
              <el-button type="success" @click="onNextClick" :loading="isLoading">
                {{ isLoading ? '正在启动设备' : '摄像头没有问题，下一步：测试麦克风' }}
              </el-button>
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
    console.log('PageCheckCamera mounted')
    this.init()
  },
  unmounted() {
    console.log('PageCheckCamera unmounted')
    this.closeDevice()
  },
  data() {
    return {
      inUseDevice: 0,
      devices: [],
      isLoading: false
    }
  },
  methods: {
    init() {
      navigator.mediaDevices.enumerateDevices().then((devices) => {
        console.log(devices)
        //将设备信息存入devices
        this.devices = devices.filter((device) => {
          return device.kind === 'videoinput'
        })
        //选择第一个设备
        this.inUseDevice = this.devices[0].deviceId
        //初始化摄像头
        this.openDevice(this.inUseDevice)
      })
    },
    openDevice(deviceId) {
      console.log('openDevice -> ', deviceId)
      this.isLoading = true
      this.closeDevice()
      navigator.mediaDevices.getUserMedia({
        // video: true
        video: {
          deviceId: deviceId,
          width: 480, height: 360
        }
      }).then(stream => {
        // 把媒体流赋值给 video 元素的 srcObj 属性，我们就能从屏幕上看到视频了
        this.$refs['video'].srcObject = stream
        // 实时拍照效果
        this.$refs['video'].play()
        //
        this.inUseDevice = deviceId
        // this.$message.success('摄像头开启成功！');
        this.isLoading = false
      }).catch(error => {
        console.log(error);
        this.$message.error('摄像头开启失败，请检查摄像头是否可用！');
        this.isLoading = false
      })
    },
    closeDevice() {
      console.log('closeDevice')
      if (this.$refs['video']) {
        if (this.$refs['video'].srcObject) {
          let stream = this.$refs['video'].srcObject
          let tracks = stream.getTracks()
          tracks.forEach(track => {
            track.stop()
          })
          this.$refs['video'].srcObject = null
        }
      }
    },
    onDeviceSelected(deviceId) {
      console.log('onDeviceSelected -> ', deviceId)
      this.closeDevice()
      this.openDevice(deviceId)
    },
    onNextClick() {
      this.$router.push('/check_microphone')
    },
    onBackClick() {
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

#div_camera_preview {
  min-width: 480px;
  min-height: 360px;
//background: black;
}

#video_component {

}

.div_content_center {
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>