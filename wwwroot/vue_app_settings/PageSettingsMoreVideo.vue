<template>
  <el-container>
    <el-table :data="cameraData">
      <el-table-column label="" width="180">
        <template #default="scope">
          <div style="display: flex; align-items: center">
            <span style="margin-left: 10px">{{ scope.row.name }}</span>
          </div>
        </template>
      </el-table-column>
      <el-table-column label="设备选择" width="180">
        <template #default="scope">
          <div style="display: flex; align-items: center">
            <el-select v-model="scope.row.inUse" class="m-2" placeholder="Select"
                       @change="onCameraSelected(scope.row.sourceType,scope.row.inUse)">
              <el-option
                  v-for="item in scope.row.listObj"
                  :key="item.value"
                  :label="item.label"
                  :value="item.value"
                  :disabled="item.disabled">
              </el-option>
            </el-select>
          </div>
        </template>
      </el-table-column>
      <el-table-column label="分辨率" width="180">
        <template #default="scope">
          <div style="display: flex; align-items: center">
            <el-select v-model="scope.row.resolution" class="m-2" placeholder="Select" v-if="isCameraParamShow"
                       @change="onResolutionSelected(scope.row.sourceType,scope.row.resolution)">
              <el-option
                  v-for="item in resolutions"
                  :key="item.value"
                  :label="item.label"
                  :value="item.value"
              >
              </el-option>
            </el-select>
          </div>
        </template>
      </el-table-column>
      <el-table-column label="帧率" width="180">
        <template #default="scope">
          <div style="display: flex; align-items: center">
            <el-select v-model="scope.row.framerate" class="m-2" placeholder="Select" v-if="isCameraParamShow"
                       @change="onFramerateSelected(scope.row.sourceType,scope.row.framerate)">
              <el-option
                  v-for="item in framerate"
                  :key="item.value"
                  :label="item.label"
                  :value="item.value">
              </el-option>
            </el-select>
          </div>
        </template>
      </el-table-column>
    </el-table>
  </el-container>
</template>
<script>

module.exports = {
  mounted() {
    console.log('audio_video mounted')

    //test code 摄像头模拟数据
    // this.cameraData[0] = {
    //   index: '0',
    //   name: '第二路视频源',
    //   enable: 'true',
    //   width: '1920',
    //   height: '1080',
    //   resolution: '1080p',
    //   framerate: '30',
    //   resolutionProfile: '0',
    //   framerateProfile: '0',
    //   inUse: 'abc',
    //   list: ['abc', 'def', 'ghi']
    // }
    // this.cameraData[1] = {
    //   index: '0',
    //   name: '第三路视频源',
    //   enable: 'true',
    //   width: '1920',
    //   height: '1080',
    //   resolution: '1080p',
    //   framerate: '30',
    //   resolutionProfile: '0',
    //   framerateProfile: '0',
    //   inUse: 'abc',
    //   list: ['abc', 'def', 'ghi']
    // }
    //test code

    try {
      this.isCameraParamShow = !SettingsJsProxy.isUseDefaultLayout()

      this.cameraData[0] = JSON.parse(SettingsJsProxy.getCamera2Json())
      // let camera1List = [];
      // this.cameraData[0].list.forEach((item, index) => {
      //   let label = item.substring(0, item.indexOf('$'))
      //   camera1List.push({label: label, value: item})
      // })
      // this.cameraData[0].list = camera1List

      // this.cameraData[0].index = 2
      this.cameraData[1] = JSON.parse(SettingsJsProxy.getCamera3Json())
      // let camera2List = [];
      // this.cameraData[1].list.forEach((item, index) => {
      //   let label = item.substring(0, item.indexOf('$'))
      //   camera2List.push({label: label, value: item})
      // })
      // this.cameraData[1].list = camera2List

      // this.cameraData[1].index = 3
    } catch (e) {
      console.log(e)
    }

  },
  unmounted() {
  },
  data() {
    return {
      cameraData: [],
      isCameraParamShow: true,
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
    onCameraSelected(sourceType, val) {
      console.log(`onCameraSelected -> ${sourceType}-${val}`)
      try {
        SettingsJsProxy.assignVideoDevice(sourceType, val)
        
        setTimeout(()=>{
          if (sourceType === 2) {
            this.cameraData[1] = JSON.parse(SettingsJsProxy.getCamera3Json())
          } else if (sourceType === 3) {
            this.cameraData[0] = JSON.parse(SettingsJsProxy.getCamera2Json())
          }
        },500)
        
      } catch (e) {
        console.log(e)
      }
    },
    onResolutionSelected(sourceType, val) {
      console.log(`onResolutionSelected -> ${sourceType}-${val}`)
      if (sourceType === 3) this.$message.warning('第三路设置分辨率后，需要关闭再打开摄像头才能生效！')
      try {
        SettingsJsProxy.setVideoSourceResolution(sourceType, val)
      } catch (e) {
        console.log(e)
      }
    },
    onFramerateSelected(sourceType, val) {
      console.log(`onFramerateSelected -> ${sourceType}-${val}`)
      try {
        SettingsJsProxy.setVideoSourceFrameRate(sourceType, val)
      } catch (e) {
        console.log(e)
      }
    },

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
</style>