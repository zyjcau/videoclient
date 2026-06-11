<template>
  <div style="padding: 16px">
    <el-descriptions
        class="margin-top"
        :column="2"
        border
    >
      <!--            <template #extra>-->
      <!--              <el-button type="primary">修改</el-button>-->
      <!--            </template>-->
      <el-descriptions-item>
        <template #label>
          <div class="cell-item">
            用户名
          </div>
        </template>
        {{ name }}
      </el-descriptions-item>
      <el-descriptions-item>
        <template #label>
          <div class="cell-item">
            角色
          </div>
        </template>
        {{ role }}
      </el-descriptions-item>
      <el-descriptions-item>
        <template #label>
          <div class="cell-item">
            公司
          </div>
        </template>
        <el-tag size="small">{{ company }}</el-tag>
      </el-descriptions-item>
      <el-descriptions-item>
        <template #label>
          <div class="cell-item">
            部门
          </div>
        </template>
        <el-tag size="small">{{ department }}</el-tag>
      </el-descriptions-item>
      <el-descriptions-item>
        <template #label>
          <div class="cell-item">
            房间号
          </div>
        </template>
        <el-tag size="small" class="enable_select">{{ roomId }}</el-tag>
      </el-descriptions-item>
      <el-descriptions-item>
        <template #label>
          <div class="cell-item">
            视频房间码
          </div>
        </template>
        <el-tag size="small" class="enable_select">{{ roomKey }}</el-tag>
      </el-descriptions-item>
<!--      <el-descriptions-item>-->
<!--        <template #label>-->
<!--          <div class="cell-item">-->
<!--            地址-->
<!--          </div>-->
<!--        </template>-->
<!--        {{ address }}-->
<!--      </el-descriptions-item>-->
    </el-descriptions>
    <el-button style="margin-top: 32px" type="danger" @click="onLogoutClick">注销</el-button>
  </div>
</template>
<script>
module.exports = {
  mounted() {
    console.log('settings me mounted')
  },
  data() {
    return {
      name: tango.myAccount.name,
      role: tango.getMyRoleId(),
      company: tango.myAccount.company_name,
      department: tango.myAccount.department_name,
      roomId: tango.myAccount.extension,
      roomKey: tango.getMyVideoRoomKey(),
      address: tango.myAccount.address
    }
  },
  methods: {
    onLogoutClick() {
      if (tangoWin.isVideoConnected) {
        ElementPlus.ElMessage({
          message: `请先退出视频，再进行注销操作`,
          type: 'warning',
        })
        return;
      }
      ElementPlus.ElMessageBox.confirm(
          '确定注销？',
          '提示',
          {
            cancelButtonText: '取消',
            confirmButtonText: '确定注销',
            center: true
          }
      )
          .then(() => {
            tangoWin.isAutoLoginNecessary = false//控制 当主动注销回到登录页后，不再自动登录
            tango.logout()
            this.$router.go(-1)
          })
          .catch(() => {

          })
    },
  }
}
</script>
<style>

</style>