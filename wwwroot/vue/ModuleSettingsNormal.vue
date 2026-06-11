<template>
  <el-container>
    <div style="position: relative;width: 100%;height: 100%;justify-content: center;align-items: center;padding: 48px">
      <el-card style="width: 35%">

        <el-row justify="center" style="margin-bottom: 12px">
          <el-col :span="8" style="display: flex;justify-content: center;align-items: center">
            我的截图
          </el-col>
          <el-col :span="8" :offset="8">
            <el-button @click="onSnapshotSaveDirectoryClick">点击查看</el-button>
          </el-col>
        </el-row>

        <el-row justify="center" v-if="isAutoAnswerVisible" style="margin-bottom: 12px">
          <el-col :span="8" style="display: flex;justify-content: center;align-items: center">
            自动接听
          </el-col>
          <el-col :span="8" :offset="8">
            <el-switch
                v-model="isAutoAnswer"
                class="ml-2"
                active-color="#13ce66"
                inactive-color="#ff4949"
                @change="onAutoAnswerChanged"
            />
          </el-col>
        </el-row>

        <el-row justify="center" v-if="isAutoAssignRendererVisible" style="margin-bottom: 12px">
          <el-col :span="8" style="display: flex;justify-content: center;align-items: center">
            自动布局
          </el-col>
          <el-col :span="8" :offset="8">
            <el-switch
                v-model="isAutoAssignRenderer"
                class="ml-2"
                active-color="#13ce66"
                inactive-color="#ff4949"
                @change="onAutoAssignRenderer"
            />
          </el-col>
        </el-row>

        <el-row justify="center">
          <el-col :span="8" style="display: flex;justify-content: center;align-items: center">
            启用调试
          </el-col>
          <el-col :span="8" :offset="8">
            <el-switch
                v-model="isDebugEnable"
                class="ml-2"
                active-color="#13ce66"
                inactive-color="#ff4949"
                @change="onEnableDebugChanged"
            />
          </el-col>
        </el-row>

      </el-card>
    </div>
  </el-container>
</template>
<script>
module.exports = {
  mounted() {
    console.log('settings normal mounted')

    // tangoWin.getSystemStatusJson({
    //   error: err => {
    //   },
    //   success: resp => {
    //     this.isAutoAnswer = tangoWin.tango.isAutoAnswer
    //     this.isAutoAssignRenderer = tangoWin.isAutoAssignRenderer
    //     this.isAutoAssignRendererVisible = !tangoWin.useDefaultLayout
    //     this.isDebugEnable = tangoWin.enableDebug
    //   }
    // })

    this.snapshotFolder = tangoWin.snapshotFolder
    this.isAutoAnswer = tangoWin.tango.isAutoAnswer
    this.isAutoAssignRenderer = tangoWin.isAutoAssignRenderer
    this.isAutoAssignRendererVisible = !tangoWin.useDefaultLayout
    this.isDebugEnable = tangoWin.enableDebug
  },
  data() {
    return {
      snapshotFolder: '',
      isAutoAnswer: false,
      isAutoAnswerVisible: true,
      isAutoAssignRenderer: true,
      isAutoAssignRendererVisible: true,
      isDebugEnable: false
    }
  },
  methods: {
    onSnapshotSaveDirectoryClick() {
      tangoWin.openFolder({path: tangoWin.snapshotFolder})
    },
    onAutoAnswerChanged(val) {
      tangoWin.setAutoAnswer(val, {
        success: resp => {
          tangoWin.tango.isAutoAnswer = val
          this.isAutoAnswer = tangoWin.tango.isAutoAnswer
        },
        error: resp => {

        }
      })
    },
    onAutoAssignRenderer(val) {
      tangoWin.setAutoAssignRenderer(val, {
        success: resp => {
          tangoWin.isAutoAssignRenderer = val
          this.isAutoAssignRenderer = tangoWin.isAutoAssignRenderer
        },
        error: resp => {

        }
      })
    },
    onEnableDebugChanged(val) {
      tangoWin.setDebugEnable(val, {
        success: resp => {
          tangoWin.enableDebug = val
          this.isDebugEnable = tangoWin.enableDebug
          console.log('> onEnableDebugChanged result -> ', resp)
        },
        error: err => {

        }
      })
    }
  }
}
</script>
<style>

</style>