<template>
  <el-container>
    <div id="login_form_container">
      <div id="login_form">
        <img class="img_logo" :src="res.logo" alt=""/>
        <p class="p_system_name">{{ res.system_name }}</p>
        <el-alert
            v-if="alert.content"
            :type="alert.type"
            :description="alert.content"
            show-icon
        ></el-alert>
        <el-input style="margin-top: 8px" v-if="true" placeholder="服务器" type="url" v-model="server"></el-input>
        <el-input style="margin-top: 8px" placeholder="用户名" type="text" v-model="username"></el-input>
        <el-input style="margin-top: 8px" placeholder="密码" type="password" v-model="password"
                  @keyup.enter="login"></el-input>
        <el-checkbox v-model="isAutoLogin" label="自动登录" border style="margin-top: 8px"></el-checkbox>
        <br>
        <div style="display: inline">
          <el-button type="primary" @click="login" style="margin-top: 24px;width: 60%">登录</el-button>
          <el-button v-if="isGuestJoinVisible" @click="onGuestClick" style="margin-top: 24px;width: 35%">访客
          </el-button>
        </div>
      </div>
    </div>
    <div style="display:inline;position: absolute;top: 0;right: 0;padding: 16px">
      <el-tooltip content="检测设备" placement="bottom" effect="light">
        <el-button size="large" @click="onAVCheckClick" circle>
          <i :class="'el-icon-tangorepair'"></i></el-button>
      </el-tooltip>
      <el-tooltip content="设置" placement="bottom" effect="light">
        <el-button size="large" @click="onSettingClick" circle>
          <i :class="'el-icon-tangoshezhi'"></i></el-button>
      </el-tooltip>
    </div>
    <span style="position: absolute;right: 16px;bottom: 16px;font-size: 12px;color: #1b1b1b">
          {{ version }}
        </span>
  </el-container>
</template>
<script>
module.exports = {
  mounted() {
    tangoWin.getSystemStatusJson({
      success: resp => {
        this.version = `${this.version}_(${tangoWin.sdkVersion})`

        this.isGuestJoinVisible = !tangoWin.enableEndpointMode

        this.res.logo = tangoWin.web_ui_logo
        this.res.system_name = tangoWin.appName

        if (tangoWin.launchAndLogin) {//参数启动时
          this.server = tangoWin.launchParams.server
          this.username = tangoWin.launchParams.username
          this.password = tangoWin.launchParams.password
          this.isAutoLogin = true
        } else {
          this.server = tangoWin.tango.server
          this.username = tangoWin.tango.username
          this.password = tangoWin.tango.password
          this.isAutoLogin = tangoWin.tango.isAutoLogin
        }

        if (tangoWin.isAutoLoginNecessary && this.isAutoLogin) {
          showLoading()
          setTimeout(() => {
            this.login()
          }, 1200)
        }
      }, error: resp => {
      }
    })
  },
  data() {
    return {
      version: web_version,
      server: '',
      username: '',
      password: '',
      isAutoLogin: false,
      res: {
        system_name: '系统',
        logo: 'res/img/logo_lss.png'
      },
      alert: {
        content: '',
        type: 'warning'
      },
      isGuestJoinVisible: false
    }
  },
  props: {},
  methods: {
    login() {
      showLoading()

      tango._init_({host: this.server})

      let un = this.username
      let pwd = this.password
      let page = this

      let connectionTimeout;

      tango.login({
        username: un,
        password: pwd,
        clientCode: tangoWin.clientName,
        success: function (myAccount, serverConfig) {

          med.init({
            tangoHost: page.server,
            host: serverConfig.subsystemMedicalUrl,
            myAccount: myAccount
          })

          tango.requestMyContacts({
            success: function () {
              console.log('requestMyContacts success.')
              tango.serverConfig = serverConfig //todo ！！！ 注意此代码在引入tango-1.60库后，可以删除掉了。
              tangoWin.saveTangoLoginInfo(
                  page.server,
                  page.username,
                  page.password,
                  page.isAutoLogin,
                  serverConfig.subsystemTangoDrawUrl,
                  {}
              )

              tango.listeners.onAllUserStateReceived = function () {
                page.showAlert('登录成功！', 'success')

                //初始化 Vidyo api
                wsapi.setHost(tango.getMyVideoPortal())
                wsapi.setAuthDirectly(tangoWin.apiCode)

                clearTimeout(connectionTimeout)

                setTimeout(function () {
                  if (tangoWin.web_ui_module_contacts_visible) {
                    page.$router.push('/main/contacts')
                  } else if (tangoWin.web_ui_module_rooms_visible) {
                    page.$router.push('/main/rooms')
                  } else if (tangoWin.web_ui_module_callsip323_visible) {
                    page.$router.push('/main/direct_call')
                  } else {
                    page.$router.push('/main/settings')
                  }
                }, 1100)
              }
              tango.connect()

              connectionTimeout = setTimeout(function () {
                page.showAlert('连接到服务器失败，请联系管理员！', 'error')
              }, 1000)

              closeLoading()
            },
            failed: function () {
              console.log('requestMyContacts failed.')
              closeLoading()
              page.showAlert('登录失败，请稍后重试！', 'error')
            }
          })
        },
        failed: function (data, ex) {
          console.log(data)
          console.log(ex)
          closeLoading()
          page.showAlert(data.msg, 'error')
        }
      });
    },
    onGuestClick() {
      location.href = './vue_app_guest_client/guest.html?fromLoginPage=true&openByClient=true'
    },
    onAVCheckClick() {
      tangoWin.openAVCheckWindow({})
    },
    onSettingClick() {
      tangoWin.openSettingsWindow({})
    },
    showAlert(msg, type) {
      this.alert.content = msg
      if (type) this.alert.type = type
    },
    onLoginSuccess() {
      this.showAlert('登录成功！', 'success')
      setTimeout(function () {
        this.$router.push('/main')
      }, 1500)
    }
  }
}
</script>
<style scoped>
#login_form_container {
  width: 100vw;
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

#login_form {
  width: 300px;
  text-align: center;
}

#login_form input {
  margin-top: 6px;
  margin-bottom: 6px;
}

#login_form button {
  width: 200px;
  margin-top: 8px;
}

.p_system_name {
  margin: 16px;
  font-weight: bold;
}

.img_logo {
  /*display: inline-block;*/
  width: 100px;
}

</style>