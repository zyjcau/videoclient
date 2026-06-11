let web_version = '20240123';
console.log(`---ui version:${web_version}---`)
let DEBUG = false;

function log(content) {
    if (DEBUG) {
        console.log(content)
    }
}

//----------------------------------SDK------------------------------------
const tango = new Tango();
tango.IM.platType = 'windows'

function isTangoLoggedIn() {
    return tango && Object.keys(tango.myAccount).length > 0;
    // return true;
}

const tangoWin = new TangoWin();
console.log(`open by cef : ${tangoWin.openByClient}`);
console.log(`launch by params : ${tangoWin.launchAndJoin}`, tangoWin.launchParams)

const med = new Medical();

const wsapi = new WSAPI();

function debounce(fn, delay, isImmediate) {
    var timer = null;  //初始化timer，作为计时清除依据
    return function () {
        let context = this;  //获取函数所在作用域this
        let args = arguments;  //取得传入参数
        clearTimeout(timer);
        if (isImmediate && timer === null) {
            //时间间隔外立即执行
            fn.apply(context, args);
            timer = 0;
            return;
        }
        timer = setTimeout(function () {
            fn.apply(context, args);
            timer = null;
        }, delay);
    }
}

//----------------------------------Vue------------------------------------
const app = Vue.createApp({
    // components: {
    //     'Camera': httpVueLoader('vue/ModuleSettingsCamera.vue')
    // }
})

const router = VueRouter.createRouter({
    history: VueRouter.createWebHashHistory(),
    routes: [
        {path: '/', name: 'Login', component: httpVueLoader('vue/PageLogin.vue')},
        {
            path: '/main',
            name: 'Main',
            component: httpVueLoader('vue/PageMain.vue'),
            children: [
                {
                    path: '/main/contacts',
                    name: 'Contacts',
                    component: httpVueLoader('vue/ModuleContacts.vue')
                },
                {
                    path: '/main/rooms',
                    name: 'Rooms',
                    component: httpVueLoader('vue/ModuleRooms.vue')
                },
                {
                    path: '/main/direct_call',
                    name: 'DirectCall',
                    component: httpVueLoader('vue/ModuleDirectCall.vue')
                },
                {
                    path: '/main/settings',
                    name: 'Settings',
                    component: httpVueLoader('vue/ModuleSettings.vue')
                },
                {
                    path: '/main/video',
                    name: 'Video',
                    component: httpVueLoader('vue/PageVideo.vue')
                },
                {
                    path: '/main/files',
                    name: 'Files',
                    component: httpVueLoader('vue/ModuleFiles.vue')
                },
                {
                    path: '/main/correct',
                    name: 'Correct',
                    component: httpVueLoader('vue/ModuleCorrectFiles.vue')
                },
            ]
        },
        {
            path: '/result',
            name: 'Result',
            component: httpVueLoader('vue/PageMeetingResult.vue')
        }
    ]
})
router.beforeEach((to, from, next) => {
    if (to.path !== '/') {//不是登录界面
        if (!isTangoLoggedIn()) {
            log('非登陆界面 并且 没有登陆')
            next('/')
        } else {
            log('非登陆界面 并且 已经登陆')
            next()
        }
    } else {//是登录界面
        log('是登录界面,直接放行')
        // log(`p -> ${JSON.stringify(tango)}`)
        next()
    }
})

function registerComponent(vueApp, vueComponentName, vueFile) {
    let c = httpVueLoader(vueFile)
    c().then(p => {
        vueApp.component(vueComponentName, p)
    })
}

function syncLayoutMode(constraint) {
    console.log('syncLayoutMode', constraint)
    //主讲人布局
    if (constraint.modeName === "lecture" && !tangoWin.tango.isLectureModeExcept) {
        let lecturerId = constraint.lecturer.videoUserId
        let isLecturerIsSelf = lecturerId === tango.getMyVideoId()
        console.log(`___is self ${isLecturerIsSelf}`)
        tangoWin.setCustomLayoutMode(isLecturerIsSelf ? 0 : 1, lecturerId, {})
    }
    //默认布局：个人模式采用剧场布局、终端模式采用网格布局
    else if (constraint.modeName === "normal") {
        tangoWin.setCustomLayoutMode(2, '', {})
    }
}

function launchVideo(vm, roomKey, roomName, query) {
    showLoading()
    tango.requestGetVideoRoomConfig({
        roomKey: roomKey,
        success: resp => {
            let constraint = resp.data
            if (constraint.videoMode === "lecture") {
                constraint.lecturer = JSON.parse(constraint.lecturer)
            }
            constraint.modeName = constraint.videoMode
            syncLayoutMode(constraint)
            tangoWin.join({
                portal: tango.getMyVideoPortal(),
                userName: tango.getMyVideoAccountName(),
                password: tango.getMyVideoAccountPassword(),
                displayName: tango.myAccount.name,
                roomName: roomName,
                roomKey: roomKey,
                roomPin: null,
                success: resp => {
                    console.log(`LaunchVideo result: `, resp)
                    // let json = JSON.parse(resp)
                    closeLoading()
                    if (resp.code === 0) {
                        vm.$router.push({name: 'Video', query: query})
                    } else {
                        ElementPlus.ElMessage({
                            message: `加入失败，请稍后重新尝试`,
                            type: 'warning',
                        })
                    }
                },
                error: resp => {
                    console.log(`launchVideo error result: `, resp)
                    closeLoading()
                    ElementPlus.ElMessage({
                        message: `加入失败，请稍后重新尝试`,
                        type: 'warning',
                    })
                }
            })
        },
        failed: resp => {
            closeLoading()
            ElementPlus.ElMessage({
                message: `加入失败，请稍后重新尝试`,
                type: 'warning',
            })
        }
    })
}

let elm_loading = undefined;

function showLoading() {
    if (!elm_loading) {
        elm_loading = ElementPlus.ElLoading.service({
            lock: true,
            text: '请稍等......',
            background: 'rgba(0, 0, 0, 0.7)',
        })
    }
}

function closeLoading() {
    if (elm_loading) elm_loading.close();
    elm_loading = undefined;
}


registerComponent(app, 'settings-camera', 'vue/ModuleSettingsCamera.vue')
registerComponent(app, 'settings-audio', 'vue/ModuleSettingsAudio.vue')
registerComponent(app, 'settings-layout', 'vue/ModuleSettingsLayout.vue')
registerComponent(app, 'settings-normal', 'vue/ModuleSettingsNormal.vue')
registerComponent(app, 'settings-network', 'vue/ModuleSettingsNetwork.vue')
registerComponent(app, 'settings-me', 'vue/ModuleSettingsMe.vue')
registerComponent(app, 'dialog-copymeeting', 'vue/DialogCopyMeeting.vue')
// registerComponent(app, 'dialog-answer', 'vue/DialogAnswer.vue')
registerComponent(app, 'dialog-filedetails', 'vue/ModuleFileDetails.vue')
registerComponent(app, 'dialog-applyconsultation', 'vue/ModuleApplyConsultation.vue')
registerComponent(app, 'dialog-correctconsultation', 'vue/ModuleCorrectConsultation.vue')
registerComponent(app, 'text-chat', 'vue/ModuleChat.vue')

app.use(router)
app.use(ElementPlus, {
    locale: ElementPlusLocaleZhCn
})
app.use(elementResizeDetectorMaker)
// app.mount('#app')

//----------------------------------Lifecycle------------------------------------

function beforeunload(ev) {
    let event = ev || window.event;

    tango.logout()
    tangoWin.disconnect()
}

(function () {//on load
    //验证native服务已工作
    tangoWin.check({
        success: function (resp) {
            web_version = resp.data
            console.log(`Native组件：可用。 版本->${resp.data}`)
            // tangoWin.setVideoVisible('false', {})
            tangoWin.connect()
            app.mount('#app')
        },
        error: function (resp) {
            console.log('Native组件：不可用')
            console.log(resp)
        }
    })
}())