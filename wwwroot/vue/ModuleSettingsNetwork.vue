<template>
  <div>
    本机IP地址： {{ localIp }}
  </div>
</template>
<script>
module.exports = {
  mounted() {
    console.log('settings network mounted')
    this.localIp = tangoWin.localIp
  },
  data() {
    return {
      localIp: ''
    }
  }
}
</script>
<style>

</style>