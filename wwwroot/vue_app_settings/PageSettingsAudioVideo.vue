<template>
  <el-container>
    <div class="container_div">
      <el-row class="el-row_big">
        <el-col :span="12" align="middle">

          <div style="width: 320px;height: 180px;">
            <div id="div_video" ref="divVideo" style="width: 100%;height: 100%;background-color: #1b1e21"></div>
          </div>

        </el-col>
        <el-col :span="12" align="middle">

          <el-row align="middle">
            <el-col :span="4" align="right">
              摄像头：
            </el-col>
            <el-col :span="12" align="left">
              <el-select v-model="camera.inUse" class="m-2" placeholder="Select"
                         @change="onCameraSelected(camera.inUse)">
                <el-option
                    v-for="item in camera.listObj"
                    :key="item.value"
                    :label="item.label"
                    :value="item.value"
                    :disabled="item.disabled">
                </el-option>
              </el-select>
            </el-col>
          </el-row>
          <el-row align="middle" v-if="isCameraParamShow">
            <el-col :span="4" align="right">
              清晰度：
            </el-col>
            <el-col :span="12" align="left">
              <el-select v-model="camera.resolution" class="m-2" placeholder="Select"
                         @change="onResolutionSelected(camera.resolution)">
                <el-option
                    v-for="item in resolutions"
                    :key="item.value"
                    :label="item.label"
                    :value="item.value"
                >
                </el-option>
              </el-select>
            </el-col>
          </el-row>
          <el-row align="middle" v-if="isCameraParamShow">
            <el-col :span="4" align="right">
              流畅度：
            </el-col>
            <el-col :span="12" align="left">
              <el-select v-model="camera.framerate" class="m-2" placeholder="Select"
                         @change="onFramerateSelected(camera.framerate)">
                <el-option
                    v-for="item in framerate"
                    :key="item.value"
                    :label="item.label"
                    :value="item.value">
                </el-option>
              </el-select>
            </el-col>
          </el-row>

        </el-col>
      </el-row>
      <el-row class="el-row_big">
        <el-col :span="12" align="middle">
          <i class="el-icon-tangomicopen" style="margin-right: 16px"></i>
          <el-select v-model="microphone.inUse" class="m-2" placeholder="Select"
                     @change="onMicrophoneSelected">
            <el-option
                v-for="item in microphone.list"
                :key="item.value"
                :label="item.label"
                :value="item.value"
            >
            </el-option>
          </el-select>
        </el-col>
        <el-col :span="12" align="left">
          <i class="el-icon-tangospeakeropen" style="margin-right: 16px"></i>
          <el-select v-model="speaker.inUse" class="m-2" placeholder="Select"
                     @change="onSpeakerSelected">
            <el-option
                v-for="item in speaker.list"
                :key="item.value"
                :label="item.label"
                :value="item.value"
            >
            </el-option>
          </el-select>
        </el-col>
      </el-row>
    </div>
  </el-container>
</template>
<script>

module.exports = {
  mounted() {
    console.log('audio_video mounted')

    let vm = this

    //监控视频div的尺寸变化，然后通知渲染层尺寸同步
    let erd = elementResizeDetectorMaker({
      strategy: "scroll", //<- For ultra performance.
      callOnAdd: true,
      debug: false
    });
    erd.listenTo(document.getElementById("div_video"), function (element) {
      vm.syncVideoRect()
    });
    this.syncVideoRect()
    window.onresize = this.syncVideoRect
    try {
      SettingsJsProxy.setVideoPanelVisible(true)
    } catch (e) {
      console.log(e)
    }

    // //test code 摄像头模拟数据
    // this.camera = {
    //   index: '0',
    //   name: '主摄像头',
    //   enable: 'true',
    //   width: '1920',
    //   height: '1080',
    //   resolution: '1080p',
    //   framerate: '30',
    //   resolutionProfile: '0',
    //   framerateProfile: '0',
    //   inUse: 'abc',
    //   list: ['abc', 'def', 'ghi']
    // }//test code

    try {
      this.winScaling = SettingsJsProxy.getWinScaling()
      this.isCameraParamShow = !SettingsJsProxy.isUseDefaultLayout()

      this.camera = JSON.parse(SettingsJsProxy.getCamera1Json())
      let cameraList = [];
      this.camera.list.forEach((item, index) => {
        let label = item.substring(0, item.indexOf('$'))
        cameraList.push({label: label, value: item})
      })
      this.camera.list = cameraList

      this.microphone = JSON.parse(SettingsJsProxy.getMicrophoneJson())
      let micList = [];
      this.microphone.list.forEach((item, index) => {
        let label = item.substring(0, item.indexOf('$'))
        micList.push({label: label, value: item})
      })
      this.microphone.list = micList

      this.speaker = JSON.parse(SettingsJsProxy.getSpeakerJson())
      let spkList = [];
      this.speaker.list.forEach((item, index) => {
        let label = item.substring(0, item.indexOf('$'))
        spkList.push({label: label, value: item})
      })
      this.speaker.list = spkList

      console.log(`windows scaling -> ${this.winScaling}`)
    } catch (e) {
      console.log(e)
    }
  },
  unmounted() {
    try {
      SettingsJsProxy.setVideoPanelVisible(false)
    } catch (e) {
      console.log(e)
    }
    window.onresize = () => {
    }
  },
  data() {
    return {
      camera: {},
      isCameraParamShow: true,
      microphone: {},
      speaker: {},
      resolutions: [
        {label: '480p', value: '480p'},
        {label: '720p', value: '720p'},
        {label: '1080p', value: '1080p'},
        {label: '2K', value: '2K'},
        {label: '4K', value: '4K'},
      ],
      framerate: [
        {label: '5帧', value: '5'},
        {label: '10帧', value: '10'},
        {label: '15帧', value: '15'},
        {label: '25帧', value: '25'},
        {label: '30帧', value: '30'},
        // {label: '40帧', value: '40'},
        {label: '50帧', value: '50'},
        {label: '60帧', value: '60'},
      ],
      winScaling: 1
    }
  },
  methods: {
    syncVideoRect() {
      let rect = this.$refs.divVideo.getBoundingClientRect()
      // console.log(`left:${rect.left},top:${rect.top},width:${rect.width},height:${rect.height}`)
      this.refreshVideoRect(rect.left, rect.top, rect.width, rect.height)
    },
    refreshVideoRect:
        debounce(
            function (left, top, width, height) {
              console.log(`refreshVideoRect(left:${left},top:${top},width:${width},height:${height})`)
              //同步视频view尺寸
              try {
                SettingsJsProxy.setVideoPanelLocationSize(left, top, width, height);
              } catch (e) {
                console.log(e)
              }
            },
            20,
            false),
    onCameraSelected(val) {
      console.log(`onCameraSelected -> ${val}`)
      try {
        SettingsJsProxy.switchCamera1(val)
      } catch (e) {
        console.log(e)
      }
    },
    onResolutionSelected(val) {
      console.log(`onResolutionSelected -> ${val}`)
      try {
        SettingsJsProxy.setVideoSourceResolution(1, val)
      } catch (e) {
        console.log(e)
      }
    },
    onFramerateSelected(val) {
      console.log(`onFramerateSelected -> ${val}`)
      try {
        SettingsJsProxy.setVideoSourceFrameRate(1, val)
      } catch (e) {
        console.log(e)
      }
    },
    onMicrophoneSelected(val) {
      try {
        SettingsJsProxy.assignMicrophone(val)
      } catch (e) {
        console.log(e)
      }
    },
    onSpeakerSelected(val) {
      try {
        SettingsJsProxy.assignSpeaker(val)
      } catch (e) {
        console.log(e)
      }
    }
  }
}
</script>
<style scoped>
.container_div {
  width: 100vw;
  height: 100vh;
  margin: 36px;
  align-items: center;
  justify-content: center;
}

.el-row_big {
  margin-bottom: 48px;
}

.div_video {
  width: 100%;
  height: 100%;
}
</style>