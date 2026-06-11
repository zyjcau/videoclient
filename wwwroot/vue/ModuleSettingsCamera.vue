<template>
  <el-table :data="cameraData" style="width: 100%">
    <el-table-column label="视频源" width="180">
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
                     @change="onCameraSelected(scope.row.index,scope.row.inUse)">
            <el-option
                v-for="item in scope.row.list"
                :key="item"
                :label="item"
                :value="item"
            >
            </el-option>
          </el-select>
        </div>
      </template>
    </el-table-column>
    <el-table-column label="分辨率" width="180">
      <template #default="scope">
        <div style="display: flex; align-items: center">
          <el-select v-model="scope.row.resolution" class="m-2" placeholder="Select"
                     @change="onResolutionSelected(scope.row.index,scope.row.resolution)">
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
          <el-select v-model="scope.row.framerate" class="m-2" placeholder="Select"
                     @change="onFramerateSelected(scope.row.index,scope.row.framerate)">
            <el-option
                v-for="item in framerate"
                :key="item.value"
                :label="item.label"
                :value="item.value"
            >
            </el-option>
          </el-select>
        </div>
      </template>
    </el-table-column>
  </el-table>
</template>
<script>
module.exports = {
  mounted() {
    console.log('settings camera mounted')

    if (tangoWin.isNativeOK) {
      this.getAndRefresh()
      tangoWin.IM.socket.on('system_status_updated', this.statusUpdated)
      tangoWin.IM.socket.on('function_available_updated', this.onFunctionAvailableUpdated)
    }
  },
  unmounted() {
    tangoWin.IM.socket.off('system_status_updated', this.statusUpdated)
    tangoWin.IM.socket.off('function_available_updated', this.onFunctionAvailableUpdated)
  },
  data() {
    return {
      cameraData: [],
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
      sendMinorStreamAvailable: tangoWin.functionAvailable.sendMinorStream
    }
  },
  methods: {
    statusUpdated(jsonStr) {
      console.log('statusUpdated', jsonStr)
      this.getAndRefresh()
    },
    onFunctionAvailableUpdated(jsonStr) {
      // this.sendMinorStreamAvailable = tangoWin.functionAvailable.sendMinorStream
      this.getAndRefresh()
    },
    getAndRefresh() {
      tangoWin.getSystemStatusJson({
        success: resp => {
          if (tangoWin.isVideoConnected && tangoWin.functionAvailable.sendMinorStream) {
            this.clearAndPushAll(tangoWin.cameras)
          } else {
            let cameras = []
            cameras[0] = tangoWin.cameras[0]
            this.clearAndPushAll(cameras)
          }
        },
        error: resp => {
        }
      })
    },
    clearAndPushAll(cameraList) {
      this.cameraData.splice(0)
      cameraList.forEach(camera => {
        if (camera.enable === true) {
          this.cameraData.push(JSON.parse(JSON.stringify(camera)))
        }
      })
    },
    onCameraSelected(index, val) {
      console.log(`${index}-${val}`)
      if (tangoWin.isNativeOK) tangoWin.assignCamera(
          index, val,
          {
            success: function (resp) {
              console.log(resp)
              tangoWin.cameras.forEach(camera => {
                if (camera.index === index) {
                  camera.inUse = val
                  console.log(tangoWin.cameras)
                }
              })
            },
            error: function (resp) {
              console.log(resp)
            }
          }
      )
    },
    onResolutionSelected(index, val) {
      if (tangoWin.isNativeOK) tangoWin.setCameraConfig(
          index, val, null, -1, -1,
          {
            success: function (resp) {
              console.log(resp)
              tangoWin.cameras.forEach(camera => {
                if (camera.index === index) {
                  camera.resolution = val
                  console.log(tangoWin.cameras)
                }
              })
            },
            error: function (resp) {
              console.log(resp)
            }
          }
      )
    },
    onFramerateSelected(index, val) {
      if (tangoWin.isNativeOK) tangoWin.setCameraConfig(
          index, null, val, -1, -1,
          {
            success: function (resp) {
              console.log(resp)
              tangoWin.cameras.forEach(camera => {
                if (camera.index === index) {
                  camera.framerate = val
                  console.log(tangoWin.cameras)
                }
              })
            },
            error: function (resp) {
              console.log(resp)
            }
          }
      )
    },
  }
}
</script>
<style>

</style>