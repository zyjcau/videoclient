<template>
  <el-container>
    <div id="container">
      <el-card style="margin-left: 64px">
        <el-row align="middle">
          <el-col :span="24">
            <p>通过此页面您可以测试音视频设备是否正常工作</p>
          </el-col>
        </el-row>
        <el-row align="middle">
          <el-col :span="6" :offset="9">
            <div class="div_content_center">
              <el-button @click="onStartClick" type="success" :loading="isLoading" :disabled="!isMediaAvailable">
                {{ !isLoading ? isMediaAvailable ? '开始检测' : '未授权或不支持音视频功能' : '正在检测环境' }}
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
    console.log('PageWelcome mounted')
    console.log(navigator.userAgent)
    navigator.mediaDevices.getUserMedia({
      audio: true,
      video: true
    })
        .then(stream => {
          console.log('getUserMedia success')
          setTimeout(() => {
            this.isLoading = false
            this.isMediaAvailable = true
          }, 1000)
        })
        .catch(error => {
          this.isLoading = false
          this.isMediaAvailable = false
        })
  },
  unmounted() {
    console.log('PageWelcome unmounted')
  },
  data() {
    return {
      isLoading: true,
      isMediaAvailable: false
    }
  },
  methods: {
    onStartClick() {
      this.$router.push('/check_camera')
    }
  }
}
</script>
<style scoped>
.el-row {
  margin-bottom: 40px;
}

.el-card {
  width: 360px;
  height: 360px;
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