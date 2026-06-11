<template>
  <el-container style="overflow: hidden">
    <el-header id="header_main">
      <el-row>
        <el-col :span="2"></el-col>
        <el-col :span="20">设置</el-col>
        <el-col :span="2">
          <!--          <el-Button v-on:click="onCloseClick">关闭</el-Button>-->
        </el-col>
      </el-row>
    </el-header>
    <el-container>
      <el-aside class="el-aside_menu">
        <el-menu
            :default-active="activeIndex"
            @select="onMenuSelected"
            class="el-menu-vertical-demo"
            router>
          <el-menu-item index="/audio_video">
            <i class="el-icon-tangovideo_comm"></i>
            <template #title>音频/视频</template>
          </el-menu-item>
          <el-menu-item index="/more_video" v-if="isMoreVideoMenuVisible">
            <i class="el-icon-tangocameraopen"></i>
            <template #title>多路视频</template>
          </el-menu-item>
          <el-menu-item index="/more_settings">
            <i class="el-icon-tangorepair"></i>
            <template #title>更多设置</template>
          </el-menu-item>
        </el-menu>
      </el-aside>
      <el-main>
        <router-view style="height: 100%"></router-view>
      </el-main>
    </el-container>
  </el-container>
</template>
<script>

module.exports = {
  mounted() {
    console.log('main mounted')

    try {
      this.isMoreVideoMenuVisible = SettingsJsProxy.isVideoConnected()
      console.log(SettingsJsProxy.isVideoConnected())
    } catch (e) {
      console.log(e)
    }

    this.activeIndex = '/audio_video'
    console.log(`default -> ${this.activeIndex}`)

  },
  unmounted() {

  },
  data() {
    return {
      activeIndex: '',
      isMoreVideoMenuVisible: false,
    }
  },
  watch: {
    $route(to, from) {
      this.onMenuSelected(to.path)
    }
  },
  methods: {
    onMenuSelected(path) {
      // console.log(`onMenuSelected(${path})`)
      this.activeIndex = path;
    },
    onCloseClick() {
      console.log('onCloseClick')
      try {
        SettingsJsProxy.closeSettingWindow()
      } catch (e) {
      }
    }
  }
}
</script>
<style scoped>
#header_main {
  background-color: #1386ee;
  height: 52px;
  text-align: center;
  line-height: 52px;
  color: white;
}

.el-aside_menu {
  /*background-color: #d3dce7;*/
  color: var(--el-text-color-primary);
  text-align: center;
  line-height: 200px;
  width: auto;
  height: 100%;
}

.el-main {
  background-color: #f7fafc;
  color: var(--el-text-color-primary);
  padding: 0;
  /*text-align: center;*/
  /*line-height: 160px;*/
}
</style>