<template>
  <el-form :inline="true">
    <div>
      <div class="titleStyle">
        患者信息:
      </div>
      <el-descriptions border :column="5">
        <el-descriptions-item label="姓名:">
          {{details.patientInfo.name}}
        </el-descriptions-item>
        <el-descriptions-item label="性别:" v-if="details.patientInfo.sex === 1">
          男
        </el-descriptions-item>
        <el-descriptions-item label="性别:" v-else="details.patientInfo.sex === 2">
          女
        </el-descriptions-item>
        <el-descriptions-item label="年龄:">
          {{details.patientInfo.age}}
        </el-descriptions-item>
        <el-descriptions-item label="民族:" v-if="details.patientInfo.nation">
          {{details.patientInfo.nation}}
        </el-descriptions-item>
        <el-descriptions-item label="婚姻状况:" v-if="details.patientInfo.marry">
          {{details.patientInfo.marry}}
        </el-descriptions-item>
        <el-descriptions-item label="职业:" v-if="details.patientInfo.career">
          {{details.patientInfo.career}}
        </el-descriptions-item>
        <el-descriptions-item label="住址:" v-if="details.patientInfo.address">
          {{details.patientInfo.address}}
        </el-descriptions-item>
        <el-descriptions-item label="身份证号码:">
          {{details.patientInfo.idNo}}
        </el-descriptions-item>
        <el-descriptions-item label="就诊科室:" v-if="details.patientInfo.department">
          {{details.patientInfo.department}}
        </el-descriptions-item>
        <el-descriptions-item label="联系电话:">
          {{details.patientInfo.phone}}
        </el-descriptions-item>
      </el-descriptions>
    </div>
    
<!--    <div v-if="this.changecustom">-->
<!--      <el-input></el-input>-->
<!--      <div>{{this.changeCustom}}</div>-->
<!--    </div>-->
    
    <div v-if="details.logs[0]">
      <div class="titleStyle">
        医疗日志:
      </div>
        <el-collapse accordion>
          <el-collapse-item :title=`日志创建人：${item.createBy}` v-for="(item,index) in details.logs" :key="item.id">
            <div>
              日志详情：{{item.msg}}
            </div>
            <div>
              日志创建日志：{{item.createTime}}
            </div>
          </el-collapse-item>
        </el-collapse>
      <div v-if="changecustom" style="margin-top: 5px">
        <el-form-item>
          <el-input
              type="textarea"
              :rows="2"
              size="medium"
              placeholder="请输入内容"
              :autosize="{ minRows: 6, maxRows: 6}"
              v-model="msg"
              style="width: 85vw"
          >
          </el-input>
        </el-form-item>
      </div>
      <div class="textAlign">
        <el-button class="inlineBlock" type="primary" @click="confirmAddLog" v-if="this.changecustom">添加日志</el-button>
      </div>
    </div>
    
    <div>
      <div class="titleStyle">
        主治医生信息:
      </div>
      <el-descriptions border :column="5">
        <el-descriptions-item label="姓名:">
          {{details.doctor.name}}
        </el-descriptions-item>
        <el-descriptions-item label="级别:">
          {{details.doctor.level}}
        </el-descriptions-item>
        <el-descriptions-item label="工作医院:">
          {{details.doctor.hospitalName}}
        </el-descriptions-item>
        <el-descriptions-item label="工作科室:">
          {{details.doctor.departmentName}}
        </el-descriptions-item>
      </el-descriptions>
    </div>
    
    <div>
      <div v-if="details.experts[0]">
        <div class="titleStyle">
          远端专家信息
        </div>
        <el-descriptions border :column="5" v-for="item in details.experts" :key="item.id">
          <el-descriptions-item label="姓名:">
            {{item.name}}
          </el-descriptions-item>
          <el-descriptions-item label="级别:">
            {{item.level}}
          </el-descriptions-item>
          <el-descriptions-item label="工作医院:">
            {{item.hospital}}
          </el-descriptions-item>
          <el-descriptions-item label="工作科室:">
            {{item.department}}
          </el-descriptions-item>
        </el-descriptions>
      </div>
      <div v-if="this.title == '编辑档案'">
<!--        <el-form-item label="姓名:" style="margin-top: 5px">-->
<!--          <el-input autocomplete="off" v-model="addExperts.name"></el-input>-->
<!--        </el-form-item>-->
        <el-form-item label="远端专家:">
          <el-select v-model="allExpertsValue" placeholder="请选择远端专家">
            <el-option
                v-for="item in allExpertsOptions"
                :key="item.value"
                :label="item.label"
                :value="item"
            >
            </el-option>
          </el-select>
        </el-form-item>
        
        <el-form-item label="级别:" style="margin-top: 5px">
          <el-input autocomplete="off" v-model="addExperts.level"></el-input>
        </el-form-item>

        <el-form-item label="工作医院:">
          <el-select v-model="companyExpertsValue" placeholder="请选择工作医院" @change="companyChange">
            <el-option
                v-for="item in companyExpertsOptions"
                :key="item.value"
                :label="item.label"
                :value="item"
            >
            </el-option>
          </el-select>
        </el-form-item>

        <el-form-item label="工作科室:">
          <el-select v-model="departmentValue" placeholder="请选择工作科室">
            <el-option
                v-for="item in departmentOptions"
                :key="item.value"
                :label="item.label"
                :value="item"
            >
            </el-option>
          </el-select>
        </el-form-item>
<!--        <el-form-item label="工作医院:" style="margin-top: 5px">-->
<!--          <el-input autocomplete="off" v-model="addExperts.hospital"></el-input>-->
<!--        </el-form-item>-->
<!--        <el-form-item label="工作科室:" style="margin-top: 5px">-->
<!--          <el-input autocomplete="off" v-model="addExperts.department"></el-input>-->
<!--        </el-form-item>-->
        <el-button @click="addRemoteExpert" style="vertical-align: super" type="primary">添加远端专家信息</el-button>
      </div>
    </div>

    <div v-if="details.files[0]">
      <div class="titleStyle">
        文件内容
      </div>
      <el-descriptions border :column="1">
        <el-descriptions-item label="文件名称" v-for="item in details.files" :key="item.id">
          {{item.fileName}}
        </el-descriptions-item>
      </el-descriptions>
    </div>
    
    <div v-if="recordList[0]">
      <div class="titleStyle">
        录制视频文件
      </div>
      <el-descriptions border :column="1">
        <el-descriptions-item label="文件名称" v-for="item in recordList">
          {{item.name}}
        </el-descriptions-item>
      </el-descriptions>
<!--      <p>{{item.name}}</p>-->
    </div>
    
    <div class="textAlign">
      <el-button class="inlineBlock" @click="dialogVisible=true" type="danger">删除</el-button>
      <el-button class="inlineBlock" @click="onExportPdf" type="primary">导出PDF</el-button>
    </div>
<!--    是否确认删除-->
    <el-dialog
        v-model="dialogVisible"
        width="30%"
    >
      <span>是否确认删除档案！</span>
      <template #footer>
      <span class="dialog-footer">
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmDelDetail">
          确认删除
        </el-button>
      </span>
      </template>
    </el-dialog>
    
  </el-form>
</template>

<script>
module.exports = {
  mounted(){
    this.queryRecordingVideo()
    // console.log("aaaaaaaaaaaaaaaaa",this.title)
    this.getAllUserSelect()
  },
  
  beforeMount(){
    this.requestMedicalDetails()
  },
  props:{
    param:{
      type:String,
      required:true,
    },
    changecustom:{
      type:Boolean,
      required:true
    },
    title:{
      type:String,
      required:true
    }
  },
  data(){
    return{
      dialogVisible:false,
      details:{
        patientInfo:{},
        doctor:{},
        logs: [],
        experts:[],
        files:{}
      },
      msg:'',
      addExperts:{
        id:"",
        name:"",
        level:"",
        hospital:"",
        department:""
      },
      recordList:[],
      allExpertsValue:'',
      allExpertsOptions:[],
      companyExpertsValue:'',
      companyExpertsOptions:[],
      departmentValue:'',
      departmentOptions:[]
    }
  },
  methods:{
    requestMedicalDetails(){
      //获取档案详情
      let vm = this
     
      med.getMedicalDetails(
          this.param,
          {
            error:resp=>{
              console.log("档案详情接口请求失败",resp)
              ElementPlus.ElMessage({
                message:`请求档案详情失败，请检查网络或联系管理员`,
                type:`warning`
              })
            },
            success:resp=>{
              this.details = JSON.parse(resp).data
            }
          }
      )
    },
    confirmDelDetail(){
      //删除档案
      med.delMedicalDetails(
          this.param,
          {
            error:resp=>{
              ElementPlus.ElMessage({
                message:`删除失败，请联系管理人员`,
                type:`error`
              })
            },
            success:resp=>{
              ElementPlus.ElMessage({
                message:`删除成功`,
                type:`success`
              })
              this.dialogVisible = false
              //this.$emit删除数据之后调取父组件的请求档案列表接口
              this.$emit('delete-success')
            }
          }
      )
    },
    confirmAddLog(){
      //补充医疗日志
      // let Med = new Medical({
      //   host:"https://tmedical.lssvc.cn",
      // })
      
      let data = {}

      data['medicalId'] = this.param
      data['msg'] = this.msg

      med.addMedicalLog(
          JSON.stringify(data),
          {
            error:resp=>{
              console.log(resp)
              ElementPlus.ElMessage({
                message:`添加失败请联系管理员`,
                type:`error`
              })
            },
            success:resp=>{
              console.log(resp)
              ElementPlus.ElMessage({
                message:`添加医疗日志成功`,
                type:`success`,
              })
              this.msg = ''
              this.requestMedicalDetails()
            }
          }
      )
      
    },
    addRemoteExpert(){
    //  补充远端专家
      
      let data = {}
      data['medicalId'] = this.param
      this.addExperts.id = this.allExpertsValue.value
      this.addExperts.name = this.allExpertsValue.label
      this.addExperts.hospital = this.companyExpertsValue.label
      this.addExperts.department = this.departmentValue.label
      //assign把两个对象合并位一个对象输出
      Object.assign(data,this.addExperts)

      med.addRemoteExpert(
          JSON.stringify(data),
          {
            error:resp=>{
              ElementPlus.ElMessage({
                message:`添加远端专家失败，请联系管理员`,
                type:`warning`
              })
            },
            success:resp=>{
              if(resp.code == 200){
                ElementPlus.ElMessage({
                  message:`添加远端专家成功`,
                  type:`success`
                })
                // for(let item in this.addExperts){
                //   this.addExperts[item] = ''
                // }
                this.allExpertsValue = ''
                this.companyExpertsValue = ''
                this.departmentValue = ''
                this.requestMedicalDetails()
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

    getAllUserSelect(){
      med.getAllUser(
          {
            error:resp=>{
              console.log("getAllUser  error-->",resp)
            },
            success:resp=>{
              console.log("getAllUser success-->",JSON.parse(resp))
              let arr = JSON.parse(resp).data
              for(let i = 0; i　< arr.length; i++) {
                let obj = {}
                // console.log(arr[i].id);
                obj.value = arr[i].id
                obj.label = arr[i].name
                // this.allUserOptions.push(obj)
                this.allExpertsOptions.push(obj)
              }
              this.getCompanySelect()
            }
          }
      )
    },

    getCompanySelect(){
      //获取企业下拉框列表
      med.getCompany(
          {
            error:resp=>{
              console.log("getCompanySelect error-->",resp)
            },
            success:resp=>{
              console.log("getCompanySelect success-->",JSON.parse(resp).data)
              let arr = JSON.parse(resp).data
              for(let i = 0; i　< arr.length; i++) {
                let obj = {}
                // console.log(arr[i].id);
                obj.value = arr[i].id
                obj.label = arr[i].company_name
                this.companyExpertsOptions.push(obj)
              }
              //调用获取科室列表
              // this.getEnterpriseSelect()
            }
          }
      )
    },

    getEnterpriseSelect(){
      //获取主治医师科室下拉框列表
      med.getEnterprise(
          this.companyExpertsValue.value,
          {
            error:resp=>{
              console.log("getEnterprise error-->",resp)
            },
            success:resp=>{
              console.log("getEnterprise success-->",JSON.parse(resp).data)
              let arr = JSON.parse(resp).data
              for(let i = 0; i　< arr.length; i++) {
                let obj = {}
                // console.log(arr[i].id);
                obj.value = arr[i].id
                obj.label = arr[i].department_name
                this.departmentOptions.push(obj)
              }
            }
          }
      )
    },

    companyChange(){
      // console.log("改变了")
      // this.companyExpertsValue = ''
      // this.companyExpertsOptions = []
      this.departmentValue = ''
      this.departmentOptions = []
      this.getEnterpriseSelect()
    },

    queryRecordingVideo(){
      //搜索记录，参数档案id
      wsapi.RecordsSearch(
          this.param,
          resp=>{
            // console.log('success -> ',resp)
            // this.recordList = []
            let list = $(resp).find("ns1\\:records");
            console.log('list ->',list)
            for (let i = 0; i < list.length; i++) {
              let item = list[i]
              console.log("item",item)
              let name = $(item).find("ns1\\:title").first().text()
              let filePath = $(item).find("ns1\\:fileLink").first().text()
              this.recordList.push({name: name+"  ---  "+filePath})
            }
          },
          resp=>{
            console.log('error -> ',resp)
          }
      )
    },
    onExportPdf(){
      tangoWin.startUrl({
        url:med.exportModulePDFReturnUrl(this.param),
        error:resp=>{
          console.log("pdf error-->",resp)
        },
        success:resp=>{
          console.log("pdf success-->",resp)
        }
      })
      
      // tangoWin.openCefWindow({
      //   url: med.exportModulePDFReturnUrl(),
      //   title: `${this.details.patientInfo.name}的档案`
      // })
      
      // console.log(this.param)
      // med.exportModulePDF(
      //     this.param,
      //     {
      //       error:resp=>{
      //         console.log("error -->",resp)
      //       },
      //       success:resp=>{
      //         console.log("success -->",resp)
      //       }
      //     }
      // )
    }
  },
}
</script>

<style scoped>

.titleStyle{
  font-size: 22px;
  margin: 15px 0 15px 0;
}

el-descriptions-item{
  width: 150px;
}

.textAlign{
  text-align: right;
  margin-top: 10px;
}

.inlineBlock{
  display: inline-block;
}

</style>