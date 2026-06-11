<template>
  <el-container v-if="visible" style="height: 100%;padding: 16px">
    <el-tabs tab-position="top" @tab-click="onTabChanged">
      <el-tab-pane v-for="display in displays" :label="`${display.deviceName}`" :name="display.index.toString()">
        <p>
          编号：{{ display.index }}、
          分辨率：{{ display.width }}x{{ display.height }}
        </p>
        <p>
          强制显示画面数量：
          <el-select
              v-model="display.forceRenderingNum"
              @change="onForceRenderingNumSelect(display,display.forceRenderingNum)">
            <el-option
                v-for="item in display.forceRenderingNumOptions"
                :key="item"
                :label="item===0?'自动':item"
                :value="item">
            </el-option>
          </el-select>
        </p>
        <el-transfer
            v-if="enableAssignVideoSource"
            style="text-align: left; display: inline-block"
            filterable
            target-order="push"
            :render-content="renderFunc"
            :titles="['视频源', '渲染中']"
            :button-texts="['取消', '显示']"
            filter-placeholder="搜索"
            :format="{
        noChecked: '${total}',
        hasChecked: '${checked}/${total}',
      }"
            v-model="display.selectedData"
            :data="display.data"
            @change="handleChange"
        >
        </el-transfer>
      </el-tab-pane>
    </el-tabs>
  </el-container>
</template>
<script>
module.exports = {
  mounted() {
    console.log('settings layout mounted')

    if (!tangoWin.isVideoConnected) {
      return;
    }

    // if(!this.needLoad){
    //   console.log('settings layout not need load')
    //   return;
    // }

    //---test code begin---
    // let testData = [
    //   {key: 'abc%1', type: 'local_camera', label: '摄像机1', disabled: false},
    //   {key: 'abc%2', type: 'local_camera2', label: '摄像机2', disabled: false},
    //   {key: 'abc%3', type: 'local_virtualsource', label: '摄像机3', disabled: false},
    //   {key: 'abc%4', type: 'remote_camera', label: '参会人1', disabled: false},
    //   {key: 'abc%5', type: 'remote_share', label: '参会人的桌面', disabled: false},
    //   {key: 'abc%6', type: 'remote_share', label: '罗舒馨', disabled: false},
    //   {key: 'abc%7', type: 'remote_share', label: '徐琨乔', disabled: false},
    // ]
    // tangoWin.displays = [
    //   {
    //     deviceName: '显示器一',
    //     index: 0,
    //     width: 100,
    //     height: 100,
    //     maxRenderer: 4,
    //     data: testData,
    //     selectedData: ['abc%1']
    //   },
    //   {
    //     deviceName: '显示器二',
    //     index: 1,
    //     width: 100,
    //     height: 100,
    //     maxRenderer: 3,
    //     data: testData,
    //     selectedData: ['abc%2']
    //   },
    //   {
    //     deviceName: '显示器三',
    //     index: 2,
    //     width: 100,
    //     height: 100,
    //     maxRenderer: 2,
    //     data: testData,
    //     selectedData: ['abc%3']
    //   }
    // ]
    //---test code end---

    this.displays = JSON.parse(JSON.stringify(tangoWin.displays))
    this.displays.forEach(display => {
      //生成最大渲染数选项列表
      display.forceRenderingNumOptions = []
      for (let i = 0; i < display.supportedGridLayoutNumber.length; i++) {
        display.forceRenderingNumOptions.push(display.supportedGridLayoutNumber[i])
      }
      this.setDisabledExcept(display.index, display.selectedData, true)
    })

    this.selectedDisplay = this.displays[0].index

    let vm = this
    if (tangoWin.isNativeOK) tangoWin.getVideoSources({
      success: function (resp) {
        console.log(resp)
        let videoSourcesMap = resp.data
        vm.initData(videoSourcesMap)
      },
      error: function () {
      }
    })
    tangoWin.listeners.onVideoSourcesUpdated = function (videoSourcesMap) {
      console.log(`layout page : onVideoSourcesUpdated`)
      vm.initData(videoSourcesMap)
    }

    // for (let i = 1; i <= 9; i++) {
    //   let str = '';
    //   for (let j = 1; j <= i; j++) {
    //     str += `${j}x${i}=${i * j} , `;
    //   }
    //   console.log(str)
    // }
    //
    //
    // //打印乘法表
    // for (let i = 1; i <= 9; i++) {
    //   let str = '';
    //   for (let j = i; j <= 9; j++) {
    //     str += `${i}x${j}=${i * j} , `
    //   }
    //   console.log(str)
    // }
  },
  unmounted() {
    tangoWin.listeners.onVideoSourcesUpdated = function (videoSources) {
    }
  },
  props: {
    visible: {
      type: Boolean,
      default: true
    },
    needLoad: {
      type: Boolean,
      default: true
    }
  },
  data() {
    return {
      enableAssignVideoSource: !tangoWin.isAutoAssignRenderer,
      selectedDisplay: 0,
      displays: [],
    }
  },
  methods: {
    initData(videoSourcesMap) {
      this.displays.forEach(display => {
        display.selectedData = []
        display.data = JSON.parse(JSON.stringify(Object.values(videoSourcesMap)))
        let renderingList = []
        display.data.forEach(videoSource => {
          if (display.index === videoSource.positionOfScreen && videoSource.isRendering) {
            renderingList.push(videoSource)
            // display.selectedData.push(videoSource.key)
          }
        })
        renderingList.sort(function (a, b) {
          return a.positionOfScreen - b.positionOfScreen
        })
        renderingList.forEach(videoSource => {
          display.selectedData.push(videoSource.key)
        })
      })
      this.displays.forEach(display => {
        this.setDisabledExcept(display.index, display.selectedData, true)
      })
    },
    onTabChanged(tab, event) {
      this.selectedDisplay = parseInt(tab.props.name)
    },
    renderFunc(h, item) {
      // return h('span', null, `${item.label}(${this.translateType2Name(item.type)})`)
      return h('span', null, `${item.label}(${item.name})`)
    },
    translateType2Name(type) {
      if (type === 'local_camera') {
        return '本地1';
      } else if (type === 'local_camera2') {
        return '本地2';
      } else if (type === 'local_virtualsource') {
        return '本地3';
      } else if (type === 'remote_camera') {
        return '远端摄像机';
      } else if (type === 'remote_share') {
        return '远端共享';
      }
    },
    handleChange(
        value, //表示右侧数据key
        direction, //left或right，表示向左还是向右
        movedKeys //表示移动了的项的key
    ) {
      // console.log(value, direction, movedKeys)
      // console.log(this.selectedDisplay, this.data)
      this.setDisabledExcept(this.selectedDisplay, movedKeys, (direction === 'right'))

      for (let ki in movedKeys) {
        let movedKey = movedKeys[ki]
        if (tangoWin.isNativeOK) {
          if (direction === 'right') {
            tangoWin.startRendering(
                movedKey,
                this.selectedDisplay,
                -1,
                {
                  success: function (resp) {
                    console.log(`request start rendering success.${resp}`)
                    let json = JSON.parse(resp)
                    if (json.code === -1) {
                      ElementPlus.ElMessage('已经没有空余位置用于渲染')
                    }
                  },
                  error: function (resp) {
                    console.log(`request start rendering failed.${resp}`)
                  }
                })
          } else {
            tangoWin.stopRendering(movedKey, {})
          }
        }
      }
    },
    /**
     * 设置screenIndex以外的包含movedKeys的项是否可用（用以实现多个穿梭框连锁）
     * @param screenIndex
     * @param movedKeys
     * @param disabled
     */
    setDisabledExcept(screenIndex, movedKeys, disabled) {
      for (let ki in movedKeys) {

        let movedKey = movedKeys[ki]

        for (let di in this.displays) {

          // console.log(di, screenIndex, (parseInt(di) !== screenIndex))

          if (parseInt(di) !== screenIndex) {
            let display = this.displays[di]
            let data = display.data
            // console.log(display)

            for (let i in data) {
              if (data[i].key === movedKey) {
                data[i].disabled = disabled;
              }
            }
          }
        }
      }
    },
    onForceRenderingNumSelect(display, num) {
      console.log(display, num)
      tangoWin.displays.forEach(d => {
        if (d.index === display.index) {
          d.forceRenderingNum = num
        }
      })
      tangoWin.setRendererContainerForceLayoutNum(display.index, num, {})
    }
  }
}
</script>
<style>

</style>