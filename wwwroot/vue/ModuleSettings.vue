<template>
  <el-container style="padding: 20px">
    <el-tabs style="width: 100%" v-model="activeName" type="border-card" @tab-click="onTabClick">
      <!--      <el-tab-pane v-if="isVideoConnected" label="布局" name="layout">-->
      <!--        <settings-layout></settings-layout>-->
      <!--      </el-tab-pane>-->
      <el-tab-pane label="视频" name="video" v-if="false">
        <settings-camera></settings-camera>
      </el-tab-pane>
      <el-tab-pane label="音频" name="audio" v-if="false">
        <settings-audio></settings-audio>
      </el-tab-pane>
      <el-tab-pane label="常规" name="normal">
        <settings-normal></settings-normal>
      </el-tab-pane>
      <el-tab-pane label="网络" name="network">
        <settings-network></settings-network>
      </el-tab-pane>
      <el-tab-pane label="关于" name="about">
        <div id="div_about">
          <img class="img_logo" :src="res.logo" alt=""/>
          <p class="p_system_name">{{ res.system_name }}</p>
          <p>{{ version }}</p>
        </div>
      </el-tab-pane>
      <el-tab-pane label="我的信息" name="my" v-if="isMeVisible">
        <settings-me></settings-me>
      </el-tab-pane>
    </el-tabs>
  </el-container>
</template>
<script>
module.exports = {
  mounted() {
    if (tangoWin.isNativeOK) {
      this.res.logo = tangoWin.web_ui_logo
      this.res.system_name = tangoWin.appName
      this.version = `${this.version} (${tangoWin.sdkVersion})`
    }

    this.isVideoConnected = tangoWin.isVideoConnected
    // this.activeName = this.isVideoConnected ? 'video' : 'normal'

    if (tangoWin.launchAndJoin) this.isMeVisible = false
  },
  data() {
    return {
      version: web_version,
      activeName: 'normal',
      isVideoConnected: false,
      res: {
        system_name: '系统',
        logo: 'res/img/logo_lss.png'
      },
      isMeVisible: true
    }
  },
  methods: {
    onTabClick(tab, event) {
    }
  }
}
</script>
<style>
#div_about {
  padding-top: 16px;
  text-align: center;

}

.img_logo {
  /*display: inline-block;*/
  width: 100px;
}
</style>