<template>
<!--  添加会诊申请具体相关信息:-->
  <div class="margin">
    <el-row :gutter="20">
      <el-col :span="6">
        <div>
          <span>申请人员：</span>
          <el-input style="width: 70%" :disabled="true" v-model="param.doctor.name"></el-input>
        </div>
      </el-col>
      <el-col :span="6">
        <div>
          <span>申请科室：</span>
          <el-input style="width: 70%" :disabled="true" v-model="param.doctor.departmentName"></el-input>
        </div>
      </el-col>
      <el-col :span="6">
        <div>
          <span>申请医院：</span>
          <el-input style="width: 70%" :disabled="true" v-model="param.doctor.hospitalName"></el-input>
        </div>
      </el-col>
      <el-col :span="6">
        <div>
          <span>联系电话：</span>
          <el-input style="width: 70%" v-model="doctor.phone"></el-input>
        </div>
      </el-col>
    </el-row>
  </div>
  <div class="margin">
    <span>请选择审批人：</span>
    <el-select v-model="allUserValue" placeholder="请选择审批人员" @change="">
      <el-option
          v-for="item in allUserOptions"
          :key="item.value"
          :label="item.label"
          :value="item"
      >
      </el-option>
    </el-select>
  </div>
  <div class="block margin">
    <span class="demonstration">会诊时间：</span>
    <el-date-picker
        v-model="doctor.applyTime"
        type="datetime"
        placeholder="选择日期时间"
        format="YYYY-MM-DD HH:mm:ss"
        value-format="YYYY-MM-DD HH:mm:ss"
    >
    </el-date-picker>
  </div>
  <div class="margin">
    <span>会诊目的：</span>
    <el-input
        type="textarea"
        :rows="4"
        placeholder="请输入内容"
        v-model="doctor.appleContent">
    </el-input>
  </div>
  <div class="margin" style="text-align: right">
    <el-button @click="emptyFormData">
      取消
    </el-button>
    <el-button type="primary" @click="addFiles">
      提交
    </el-button>
  </div>
  
<!--    <el-button @click="aaa">打印</el-button>-->
</template>

<script>
module.exports = {
  mounted() {
    this.getAllUserSelect()
    this.setDefault()
  },
  props: {
    param: {
      type: Object,
      required: true
    }
  },
  data() {
    return {
      dialogVisible:true,
      allUserValue: "",
      allUserOptions: [],
      doctor: {
        medicalId: "",//档案id
        approveById:"",//申请人id
        approveBy:"",
        appleHospital: "",
        phone: "",
        applyTime: "",
        appleContent: "",
        status:"1"
      },
    }
  },
  methods: {
    setDefault(){
      //每次进来此页面设置默认值
      this.doctor.medicalId = this.param.id
      this.doctor.appleHospital = this.param.doctor.hospitalName 
    },
    emptyFormData(){
      this.$emit('appcorrect-success')
    },
    addFiles(){
      if(this.allUserValue){
        this.doctor.approveBy = this.allUserValue.label
        this.doctor.approveById = this.allUserValue.value
        console.log(this.doctor)
      }
      med.addCorrect(
          JSON.stringify(this.doctor),
          {
            error:resp=>{
              console.log(resp)
            },
            success:resp=>{
              if(resp.code === 200){
                console.log(resp)
                ElementPlus.ElMessage({
                  message:`申请审批成功`,
                  type:`success`,
                })
                this.dialogVisible = false
                this.$emit('appcorrect-success')
              }else{
                ElementPlus.ElMessage({
                  message:`申请审批失败，请联系管理员`,
                  type:`error`,
                })
              }
              
            }
          }
      )
    },
    getAllUserSelect() {
      med.getAllUser(
          {
            error: resp => {
              console.log("getAllUser  error-->", resp)
            },
            success: resp => {
              console.log("getAllUser success-->", JSON.parse(resp))
              let arr = JSON.parse(resp).data
              for (let i = 0; i < arr.length; i++) {
                let obj = {}
                // console.log(arr[i].id);
                obj.value = arr[i].id
                obj.label = arr[i].name
                this.allUserOptions.push(obj)
              }
            }
          }
      )
    },
    // getAllUser: function ({error, success}) {
    //   this.requestByGet(
    //       `https://hrst.lssvc.cn/user/all`,
    //       {error: error, success: success}
    //   )
    // },
    // requestByGet: function (method, {dataType = 'text', error, success}) {
    //   $.ajax({
    //     url: method,
    //     type: 'get',
    //     headers: {
    //       Authorization: 'Bearer ' + 'eyJhbGciOiJIUzUxMiJ9.eyJzdWIiOiJ6ZW5nIiwiZXhwIjoxNjkxOTkyNDIwLCJjcmVhdGVkIjoxNjkxNzMzMjIwODc1LCJhdXRob3JpdGllcyI6W3siYXV0aG9yaXR5Ijoic3lzOmNsaWVudDpkZWxldGUifSx7ImF1dGhvcml0eSI6InN5czpjbGllbnQ6dXBsb2FkIn0seyJhdXRob3JpdHkiOiJzeXM6bWFuYWdlcjphZGQifSx7ImF1dGhvcml0eSI6InN5czptYW5hZ2VyOmRlbGV0ZSJ9LHsiYXV0aG9yaXR5Ijoic3lzOm1hbmFnZXI6ZWRpdCJ9LHsiYXV0aG9yaXR5Ijoic3lzOm1hbmFnZXI6dmlldyJ9LHsiYXV0aG9yaXR5Ijoic3lzOnJvb206YWRkIn0seyJhdXRob3JpdHkiOiJzeXM6cm9vbTpkZWxldGUifSx7ImF1dGhvcml0eSI6InN5czpyb29tOmVkaXQifSx7ImF1dGhvcml0eSI6InN5czpyb29tOnZpZXcifSx7ImF1dGhvcml0eSI6InN5czp1c2VyOmFkZCJ9LHsiYXV0aG9yaXR5Ijoic3lzOnVzZXI6ZGVsZXRlIn0seyJhdXRob3JpdHkiOiJzeXM6dXNlcjplZGl0In0seyJhdXRob3JpdHkiOiJzeXM6dXNlcjp2aWV3In0seyJhdXRob3JpdHkiOiJzeXM6dnM6ZnVuYyJ9XX0.mb5fya2ptDw5yBiIMpaeEOaNO7-neJDq-fxbICPrUa2ZblXu5uPA8Q3LH3QBWzPZ1vAxKKTC8b20i98SO0Srug',
    //     },
    //     dataType: dataType,
    //     contentType: 'text/xml;charset="UTF-8"',
    //     error: error,
    //     success: success
    //   });
    // },
  }
}
</script>

<style>

.margin{
  margin: 0 0 15px 10px;
}

</style>