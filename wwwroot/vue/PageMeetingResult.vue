<template>
  <el-container>
    <el-main>
      <div style="width: 60%;margin: 1px auto">
        <el-form style="text-align: left">
          <div>
            <el-form-item label="输入电子档案ID:">
              <el-input autocomplete="off" v-model="fileID"></el-input>
            </el-form-item>
            <el-form-item label="输入编写人的姓名:">
              <el-input autocomplete="off" v-model="name" disabled></el-input>
            </el-form-item>
          </div>
          <div>
            <el-form-item style="margin-top: 10px" label="输入更新日志内容">
              <el-input
                  type="textarea"
                  :rows="7"
                  placeholder="更新日志内容"
                  v-model="textarea"
              >
              </el-input>
            </el-form-item>
          </div>
        </el-form>
      </div>
    </el-main>
    <el-footer>
      <el-button @click="onClickSkip">跳过</el-button>
      <el-button @click="onClickSubmit">提交</el-button>
    </el-footer>
  </el-container>
</template>

<script>

module.exports ={
  mounted(){
    this.getFilesId()
    this.getMyName()
  },
  data(){
    return{
      textarea:"",
      name:"",
      fileID:""
    }
  },
  methods:{
    onClickSkip(){
      this.$router.go(-2)
    },
    onClickSubmit(){
    //  提交
    //  创建人就是本账号的名称，如果是从档案进入的视频那么档案id直接带过去
      let data = {}
      
      data.medicalId = this.fileID
      data.msg = this.textarea
      
      med.addMedicalLog(
          JSON.stringify(data),
          {
            error:resp=>{
              
            },
            success:resp=>{
              if(resp.code === 200){
                ElementPlus.ElMessage({
                  message:`提交成功`,
                  type:`success`
                })
                this.$router.go(-2)
              }else{
                ElementPlus.ElMessage({
                  message:resp.msg,
                  type:`error`
                })
              }
            }
          }
      )
    },
    getFilesId(){
      if(this.$route.query.id){
        this.fileID = this.$route.query.id
        // console.log("this.$route.query.id",this.$route.query.id)
      }
    },
    getMyName(){
      let me = JSON.parse(JSON.stringify(tango.myAccount))
      // console.log("my name",me.name)
      this.name = me.name
    }
  }
}

</script>

<style scoped>

.el-footer{
  background-color: #E9EEF3;
  text-align: right;
  margin: 0 20px 0 20px;
  padding-bottom: 20px;
}

.el-main {
  background-color: #E9EEF3;
  margin: 20px 20px 0 20px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  text-align: center;
}

.titleStyle{
  font-size: 22px;
  margin-bottom: 15px;
}

.font{
  font-size: var(--el-form-label-font-size);
  color: var(--el-text-color-regular);
}

</style>