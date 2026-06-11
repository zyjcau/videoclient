<script>
module.exports = {
  mounted() {
    if (typeof (tango) != 'undefined' && tango != null && tango.serverConfig.subsystemTangoDrawUrl) {
      console.log('tango ok')
    } else {
      console.log('tango not ok')
    }
  },
  unmounted() {

  },
  data() {
    return {
      isChatVisible: true,
      isHasNewMessage: false,
      isRecording: false,
    }
  },
  methods: {
    onChatClick() {
      this.isChatVisible = !this.isChatVisible
    },
    onRecordingClick() {
    }
  }
}
</script>

<template>
  <el-container id="video_page_container">
    <el-aside>
      <el-header>
        test
      </el-header>
    </el-aside>
    <el-container>
      <el-main id="main_video">
        <el-row style="height: 100%">
          <el-col :span="isChatVisible?18:24">
            <div id="div_video" ref="divVideo" style="background: #6f42c1;width: 100%;height: 100%">
            </div>
          </el-col>
          <el-col :span="6" v-if="isChatVisible">
            <!--            <div style="background: #0b2e13;width: 100%;height: 100%">-->
            <!--              -->
            <!--            </div>-->
            <text-chat></text-chat>
          </el-col>
        </el-row>
      </el-main>
      <el-footer>
        <el-button size="small" round @click="onChatClick" :type="isHasNewMessage?'success':''">
          {{ isHasNewMessage ? "消息（新消息）" : "消息" }}
        </el-button>
        <el-button size="small" @click="onRecordingClick" :loading="true" round
                   style="margin-right: 32px">
          <template #loading>
            处理中...
            <div class="custom-loading" v-if="false">
              <svg class="circular" viewBox="-10, -10, 50, 50">
                <path class="path"
                      d="
            M 30 15
            L 28 17
            M 25.61 25.61
            A 15 15, 0, 0, 1, 15 30
            A 15 15, 0, 1, 1, 27.99 7.5
            L 15 15
          "
                    style="stroke-width: 4px; fill: rgba(0, 0, 0, 0)"/>
              </svg>
            </div>
          </template>
          <i :class="isRecording?'el-icon-tangorecordingstop':'el-icon-tangorecordingstart'"
             style="size: 24px"></i>
        </el-button>
      </el-footer>
    </el-container>
  </el-container>
</template>

<style scoped>
.el-aside {
  background-color: #1a212b;
  color: var(--el-text-color-primary);
  width: 20%;
}

.el-header {
  background-color: #01488f;
  height: 56px;
  line-height: 56px;
  color: white;
  font-size: 18px;
  text-align: center;
}

.el-footer {
  background-color: #1b1e21;
  height: 48px;
  line-height: 48px;
  color: white;
}

#main_video {
  padding: 0 !important;
  --el-main-padding: 0px;
  display: inline;
}

#div_video {
  height: 100%;
  width: 100%;
}

.item {

}

.custom-loading{
  width: 20px;
  height: 20px;
}
.circular{
  color: #1c7430;
}
</style>