<template>
  <el-container>
<!--  setFlexLayout,flexLayoutBottom弹性盒布局-->
        <div class="setFlexLayout">
          <el-header style="padding-top:10px;border-bottom: 1px solid #b4bccc">
            <el-input placeholder="档案搜索" style="width: 30%" v-model="searchArchives" @input="onSearchChanged"></el-input>
            <el-button type="primary" round style="float: right" @click="createFiles">
              新建档案
            </el-button>
          </el-header>
          <el-main class="flexLayoutBottom">
<!--            档案列表展示框-->
            <el-table :data="tableData" style="width: 100%" height="90%" border>
              <el-table-column prop="patientInfo.id" label="电子档案ID"></el-table-column>
              <el-table-column prop="patientInfo.name" label="患者名称" width="150px"></el-table-column>
              <el-table-column prop="createTime" label="创建时间"></el-table-column>
              <el-table-column prop="patientInfo.department" label="患者就诊科室"></el-table-column>
              <el-table-column prop="doctor.name" label="主治医师姓名"></el-table-column>
              <el-table-column label="操作" width="450px">
                <template #default="scope">
                  <el-button @click="fileDetails(scope.$index, scope.row)" size="small">
                    档案详情
                  </el-button>
                  <el-button @click="fileEdit(scope.$index, scope.row)" size="small">
                    编辑档案
                  </el-button>
                  <el-button type="primary" @click="joinFileToMeet(scope.$index, scope.row)" size="small">
                    加入会议
                  </el-button>
                  <el-button type="primary" @click="copyMeetingInfo(scope.$index, scope.row)" size="small">
                    复制参会信息
                  </el-button>
                </template>
              </el-table-column>
            </el-table>
<!--            分页-->
            <el-pagination
                background
                layout="prev, pager, next"
                :total="total"
                @current-change="handleCurrentChange"
                style="text-align: center;margin-top: 5px"
                :page-size="page"
                :current-page="paginationNum"
            >
            </el-pagination>
          </el-main>
          <el-dialog
              v-model="dialogfileDetaile"
              :destroy-on-close="true"
              :title="titleType"
              width="90%"
          >
            <dialog-filedetails 
                :param=transfer 
                @delete-success="requestFileList" 
                :changecustom=changeCustom
                custom-class="overflow"
                >
            </dialog-filedetails>
          </el-dialog>
          <!--          创建新建档案弹出框-->
          <el-dialog v-model="dialogVisible" title="新建档案" width="90%" :show-close="false">
            <el-form :inline="true" :model="form" :rules="formRules" ref="form">
              <div>
                <div class="titleStyle">
                  1.患者信息:
                </div>
                <el-form-item label="姓名:" prop="patientInfo.name">
                  <el-input autocomplete="off" v-model="form.patientInfo.name" style="width:110px"></el-input>
                </el-form-item>
                <el-form-item label="性别:" prop="patientInfo.sex">
                  <!--                <el-input autocomplete="off"></el-input>-->
                  <el-radio v-model="form.patientInfo.sex" label="1">男</el-radio>
                  <el-radio v-model="form.patientInfo.sex" label="2">女</el-radio>
                </el-form-item>
                <el-form-item label="年龄:" prop="patientInfo.age">
                  <el-input autocomplete="off" v-model="form.patientInfo.age" style="width:110px"></el-input>
                </el-form-item>
                <el-form-item label="民族:">
                  <el-input autocomplete="off" v-model="form.patientInfo.nation" style="width:110px"></el-input>
                </el-form-item>
                <el-form-item label="婚姻状况:" prop="patientInfo.marry">
                  <el-input autocomplete="off" v-model="form.patientInfo.marry" style="width:110px"></el-input>
                </el-form-item>
                <el-form-item label="职业:">
                  <el-input autocomplete="off" v-model="form.patientInfo.career" style="width:110px"></el-input>
                </el-form-item>
                <el-form-item label="住址:">
                  <el-input autocomplete="off" v-model="form.patientInfo.address"></el-input>
                </el-form-item>
                <el-form-item label="身份证号码:" prop="patientInfo.idNo">
                  <el-input autocomplete="off" v-model="form.patientInfo.idNo"></el-input>
                </el-form-item>
                <el-form-item label="就诊科室:">
                  <el-input autocomplete="off" v-model="form.patientInfo.department"></el-input>
                </el-form-item>
                <el-form-item label="联系电话:" prop="patientInfo.phone">
                  <el-input autocomplete="off" v-model="form.patientInfo.phone"></el-input>
                </el-form-item>
              </div>
              <div>
                <div class="titleStyle">
                  2.医疗日志:
                </div>
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
              <div>
                <div class="titleStyle">
                  3.主治医生信息:
                </div>
                <el-form-item label="姓名:" prop="doctor.name">
                  <el-input autocomplete="off" v-model="form.doctor.name"></el-input>
                </el-form-item>
                <el-form-item label="级别:">
                  <el-input autocomplete="off" v-model="form.doctor.level"></el-input>
                </el-form-item>
                <el-form-item label="工作医院:" prop="companyValue">
<!--                  <el-input autocomplete="off" v-model="form.doctor.hospitalName"></el-input>-->
                  <el-select v-model="form.companyValue" placeholder="请选择医院" @change="companyChange">
                    <el-option
                        v-for="item in form.companyOptions"
                        :key="item.value"
                        :label="item.label"
                        :value="item"
                    >
                    </el-option>
                  </el-select>
                </el-form-item>
                <el-form-item label="工作科室:" prop="departmentValue">
<!--                  <el-input autocomplete="off" v-model="form.doctor.departmentName"></el-input>-->
                  <el-select v-model="form.departmentValue" placeholder="请选择科室" @change="departmentChange">
                    <el-option
                        v-for="item in form.departmentOptions"
                        :key="item.value"
                        :label="item.label"
                        :value="item"
                    >
                    </el-option>
                  </el-select>
                </el-form-item>
                
              </div>

              <div>
                <div class="titleStyle">
                  4.远端专家信息:
                </div>
                <el-table :data="form.experts" style="width: 100%">
                  <el-table-column prop="name" label="名称" width="180">
                  </el-table-column>
                  <el-table-column prop="level" label="级别" width="180">
                  </el-table-column>
                  <el-table-column prop="hospital" label="医院">
                  </el-table-column>
                  <el-table-column prop="department" label="科室">
                  </el-table-column>
                  <el-table-column label="操作">
                    <template #default="scope">
                      <el-button
                          size="small"
                          type="danger"
                          @click="delRemoteExpert(scope.$index, scope.row)"
                      >删除</el-button
                      >
                    </template>
                  </el-table-column>
                </el-table>
                  <el-form-item label="姓名:" style="margin-top: 5px">
                    <el-input autocomplete="off" v-model="addExperts.name"></el-input>
                  </el-form-item>
                  <el-form-item label="级别:" style="margin-top: 5px">
                    <el-input autocomplete="off" v-model="addExperts.level"></el-input>
                  </el-form-item>
                  <el-form-item label="工作医院:" style="margin-top: 5px">
                    <el-input autocomplete="off" v-model="addExperts.hospital"></el-input>
                  </el-form-item>
                  <el-form-item label="工作科室:" style="margin-top: 5px">
                    <el-input autocomplete="off" v-model="addExperts.department"></el-input>
                  </el-form-item>
                  <el-button @click.prevent="addRemoteExpert" style="vertical-align: super" type="primary">添加远端专家信息</el-button>
              </div>
              
              <div>
                <div class="titleStyle">
                  5.文件上传
                </div>
                <el-upload
                    v-model:file-list="fileList"
                    class="upload-demo"
                    action="#"
                    multiple
                    :http-request="addimg"
                    :limit="4"
                    :on-remove="delUploadFiles"
                    :on-exceed="uploadFilesoverflow"
                >
                  <el-button type="primary">请选择需要上传的文件</el-button>
                  <template #tip>
                    <div class="el-upload__tip">
                      最多可选择四项图片或文件!
                    </div>
                  </template>
                </el-upload>
              </div>
            </el-form>
            <template #footer>
              <span class="dialog-footer">
                <el-button @click="emptyFormData">
                  取消
                </el-button>
                <el-button type="primary" @click="addFiles()">
                  提交
                </el-button>
              </span>
            </template>
          </el-dialog>
        </div>
  </el-container>
</template>
<script>

module.exports = {
  mounted(){
    this.requestFileList()
    this.getCompanySelect()
  },
  data(){
    return{
      searchArchives:'',
      tableData:[],
      dialogfileDetaile:false,
      dialogVisible:false,
      changeCustom:false,
      titleType:'',
      msg:"",
      addExperts:{
        name:"",
        level:"",
        hospital:"",
        department:""
      },
      file: {},
      form:{
        patientInfo:{
          name:"",
          sex:"",
          age:"",
          nation:"",
          marry:"",
          career:"",
          address:"",
          idNo:"",
          department:"",
          phone:"",
        },
        logs:[],
        departmentValue: "",
        departmentOptions: [],
        companyValue:"",
        companyOptions:[],
        doctor:{
          name:"",
          level:"",
          hospitalName:"",
          departmentName:""
        },
        experts: [],
      },
      formRules:{
        'patientInfo.name':[
          { required: true, message: '请输入用户名', trigger: 'blur' },
          { min: 2, max: 5, message: '姓名长度为2到5个字', trigger: 'blur' }
        ],
        'patientInfo.sex':[
          { required: true, message: '请选择性别', trigger: 'blur' },
        ],
        'patientInfo.age':[
          { required: true, message: '请输入患者年龄', trigger: 'blur' },
          { pattern:/^[1-9]\d{0,2}$/, message: '请输入正确年龄', trigger: 'blur' }
        ],
        'patientInfo.idNo': [
          { required: true, message: '请输入患者身份证号', trigger: 'blur' },
          { pattern:/^([1-6][1-9]|50)\d{4}(18|19|20)\d{2}((0[1-9])|10|11|12)(([0-2][1-9])|10|20|30|31)\d{3}[0-9Xx]$/, message: '请输入正确的身份证号码', trigger: 'blur' },
          { max:18, message: '请输入正确的身份证号码', trigger: 'blur' }
        ],
        'patientInfo.phone':[
          { required: true, message: '请输入患者手机号', trigger: 'blur' },
          { pattern:/^[1-9]\d{0,10}$/, message: '请输入正确手机号', trigger: 'blur' },
        ],
        'patientInfo.marry':[
          { required: true, message: '请输入婚姻状况', trigger: 'blur'}
        ],
        'doctor.name':[
          { required: true, message: '请输入主治医师姓名', trigger: 'blur' },
          { min: 2, max: 5, message: '姓名长度为2到5个字', trigger: 'blur' }
        ],
        companyValue:[
          { required: true, message: '请选择医院', trigger: 'change' }
        ],
        departmentValue:[
          { required: true, message: '请选择科室', trigger: 'change' }
        ]
      },
      transfer:"",
      total:"",
      page:"",
      paginationNum:""
    }
  },
  methods:{
    requestFileList(num=1,size=8){
      // console.log("页面数据大小",this.page)
      let vm = this
      // this.paginationNum = this.page
      med.getMedicalRecordsList(
          num,
          size,
          {
            error:function(resp) {
              console.log("getMedicalRecordsList error->",resp)
              ElementPlus.ElMessage({
                message:`请求档案数据失败请联系管理员！`,
                type:`warning`
              })
            },
            success:function(resp) {
              let Newresp = JSON.parse(resp)
              vm.tableData = Newresp.data.records
              vm.total =Newresp.data.total
              vm.page = Newresp.data.size
            }
          }
      )
      //因为每次请求都回到第一页，所以给paginationNum赋值为1
      this.paginationNum = 1
      this.dialogfileDetaile = false
    },
    createFiles(){
      this.dialogVisible = true
    },
    
    delRemoteExpert(index,rows){
      //删除某一行远端专家信息
      this.form.experts.splice(index,1)
    },
    
    addRemoteExpert(){
      //添加远端专家信息
      this.form.experts.push({
        name:this.addExperts.name,
        level:this.addExperts.level,
        hospital:this.addExperts.hospital,
        department:this.addExperts.department
      })
      
      this.addExperts.name = ''
      this.addExperts.level = ''
      this.addExperts.hospital = ''
      this.addExperts.department = ''
    },
    addimg(img){
      //把照片存入this.file以{"img.file.uid.toString()"：img.file}
      this.file[img.file.uid.toString()] = img.file
    },
    addFiles(){
      showLoading()
      //表单验证没有问题才提交valid的值（true，false）
      this.$refs.form.validate((valid)=>{
        if(valid){
          // console.log("提交")
          if (this.msg !== null||this.msg !=="" ||this.msg !==undefined){
            console.log(this.form)
            this.form.logs.push({msg:this.msg})
          }
          
          this.form.doctor.hospitalName = this.form.companyValue.label
          this.form.doctor.departmentName = this.form.departmentValue.label
          
          var tmpform = JSON.parse(JSON.stringify(this.form))
          
          var myformData = new FormData()

          //添加除图片信息其他的填写信息
          myformData.append("data", JSON.stringify(tmpform))
          
          // console.log("有的文件", this.file)
          // console.log("所有照片的uid",Object.values(this.file))

          //便利找到每一项图片的uid
          Object.values(this.file).forEach( f => {
            myformData.append("file", f)
          })
          
          med.addMedicalRecords(
              myformData,
              {
               error:resp => {
                 closeLoading()
               },
                success: resp =>{
                 closeLoading()
                  if(resp.code === 200){
                    ElementPlus.ElMessage({
                      message: `文件上传成功`,
                      type: 'success',
                    })
                    // vm.emptyFormData()
                    this.emptyFormData()
                    //调取出查询列表新增之后更新列表
                    this.requestFileList()
                  }else{
                    console.log(resp)
                    ElementPlus.ElMessage({
                      message:resp.msg,
                      type:'danger'
                    })
                  }
                }
              }
          )
        }else {
          console.log("valid",valid)
          ElementPlus.ElMessage({
            message:`请把必要信息全部填写完毕在提交！`,
            type:`warning`
          })
        }
      })
    },

    emptyFormData(){
      //对整个表单进行重置，将所有字段值重置为初始值并移除校验结果element的自带方法
      this.$refs.form.resetFields()
      //循环弹出框所有对象清空值保留键
      for(let item in this.form.patientInfo){
        this.form.patientInfo[item] = ''
      }
      for(let item in this.form.doctor){
        this.form.doctor[item] = ''
      }
      for(let item in this.addExperts){
        this.addExperts[item] = ''
      }
      for(let item in this.file){
        this.file[item] = ''
      }
      this.msg = ''
      this.form.logs = []
      this.form.experts = []
      this.dialogVisible = false
    },
    
    delUploadFiles(file){
      //element上传文件删除的钩子函数
      //file删除的文件
      delete this.file[file.raw.uid.toString()]
    },
    
    uploadFilesoverflow(){
    //  element文件超过设置数量的钩子函数
      ElementPlus.ElMessage({message:`上传文件不能超过4个`,type:`warning`})
    },
    
    fileDetails(index,rows){
      this.titleType = "档案详情"
      this.changeCustom = false
      this.dialogfileDetaile = true
      this.transfer = rows.id
    },

    fileEdit(index,rows){
      this.titleType = "编辑档案"
      this.changeCustom = true
      this.dialogfileDetaile = true
      this.transfer = rows.id
    },


    joinFileToMeet(index,rows){
    //  加入会议通过路由传参定义从档案进入会议的标记为true，实现会中录制弹出填写档案
      launchVideo(this,tango.getMyVideoRoomKey(),tango.myAccount.name,{
        fileId:rows.id,
        bool:true,
        doctorName:this.tableData[0].doctor.name,
        patientName:this.tableData[0].patientInfo.name
      })
      console.log("档案id",rows.id)
    },
    
    
    search() {
      console.log(`search(${this.searchArchives})`)
      med.searchMedicalFileByPatientName(
          this.searchArchives,
          {
            error:resp=>{
              console.log("search error->",resp)
            },
            success:resp=>{
              console.log("search success->",JSON.parse(resp).data)
              let data = JSON.parse(resp).data
              console.log("data.records--->",data.records)
              if(undefined !== data.records && data.records.length>0){
                this.tableData = data.records
              }else{
                console.log("没有找到对应的档案")
                ElementPlus.ElMessage({
                  message: `没有找到：${this.searchArchives}的档案`,
                  type: 'warning',
                  offset: 150,
                  duration: 1000
                })
              }
            }
          }
      )
    },

    onSearchChanged: debounce(function (keyword) {
      console.log("keyword",keyword)
      if (keyword === undefined || keyword === null || keyword.length === 0) {
        //切换到自己的档案列表
        this.requestFileList()
      } else {
        this.search()
      }
    }, 500, false),

    copyMeetingInfo(index,rows){
      //复制链接
      // const tangodrawUrl = `谷歌浏览器访问地址:https://webrtc.lssvc.cn/product/desktop/index.html?host=dev.lssvc.cn&&roomKey=`
      const tangodrawUrl = `
      谷歌浏览器访问地址:${tangoWin.web_webrtc_site}?host=dev.lssvc.cn&&roomKey=${med.getMyRoomKey()}
      昵称：任意填写
      手机端下载地址：https://hrst.lssvc.cn/client/get?filename=TangoAndroid_V1.1.25.apk
      手机端请搜索:"${med.getMyDisplayName()}"并点击加入他的房间
      此时对应开会的电子档案ID为${rows.id}
      `
      tangoWin.copyToClipboard(
          {
            content:`参会链接：${tangodrawUrl}`,
            success:()=>{
              ElementPlus.ElMessage({
                message:`复制成功请转到需要通知的程序右键粘贴`,
                type:`success`
              })
            },
            error:()=>{
              ElementPlus.ElMessage({
                message:`复制失败请联系管理员`,
                type:`error`
              })
            }
          }
      )
    },

    handleCurrentChange(val){
      //pageNum分页的第几页，pageSize分页每一页数据的数量
      console.log(`当前页: ${val}`);
      this.requestFileList(val)
      this.paginationNum = val
    },
    
    getCompanySelect(){
      //获取企业下拉框列表
      this.getCompany(
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
                this.form.companyOptions.push(obj)
              }
              //调用获取科室列表
              this.getEnterpriseSelect()
            }
          }
      )
    },

    getEnterpriseSelect(){
      console.log("请求参数",this.form.companyValue)
      //获取科室下拉框列表
      this.getEnterprise(
          this.form.companyValue.value,
          {
            error:resp=>{
              console.log("getEnterprise error-->",resp)
            },
            success:resp=>{
              console.log("getEnterprise success-->",JSON.parse(resp).data)
              let arr = JSON.parse(resp).data
              for(let i = 0; i　< arr.length; i++) {
                let obj = {}
                console.log(arr[i].id);
                obj.value = arr[i].id
                obj.label = arr[i].department_name
                this.form.departmentOptions.push(obj)
              }
            }
          }
      )
    },
    
    // getListVideoUserSelect(){
    //   this.getListVideoUser(
    //       this.companyValue,
    //       this.departmentValue,
    //       {
    //         error:resp=>{
    //           console.log("getListVideoUserSelect error-->",resp)
    //         },
    //         success:resp=>{
    //           console.log("getListVideoUserSelect success-->",JSON.parse(resp))
    //           let arr = JSON.parse(resp).data
    //           //如果arr有值的话给下拉框设置一个默认值
    //           if(arr.length !==0 ){
    //             this.listVideoUserValue = arr[0].id
    //           }
    //           for(let i = 0; i　< arr.length; i++) {
    //             let obj = {}
    //             console.log(arr[i].id);
    //             obj.value = arr[i].id
    //             console.log("arr[i].department_name2",arr[i].name);
    //             obj.label = arr[i].name
    //             this.listVideoUserOptions.push(obj)
    //           }
    //           console.log(this.listVideoUserOptions)
    //         }
    //       }
    //   )
    // },

    companyChange(){
      //医院下拉框改变的时候先清空科室的数据在请求赋值
      this.form.departmentOptions = []
      this.form.departmentValue = ''
      this.getEnterpriseSelect()
    },

    departmentChange(){
      //科室下拉框改变时候的回调
    },
    
    getCompany:function ({error,success}){
      this.requestByGet(
          `https://hrst.lssvc.cn/manager/getCompany`,
          {
            error:error,success:success
          }
      )
    },
    getEnterprise:function (companyId,{error,success}){
      this.requestByGet(
          `https://hrst.lssvc.cn/manager/getDepartment?companyId=${companyId}`,
          {
            error:error,success:success
          }
      )
    },
    getListVideoUser:function (companyId,officeId,{error,success}){
      this.requestByGet(
          `https://hrst.lssvc.cn/user/listVideoUser?companyId=${companyId}&officeId=${officeId}`,
          {
            error:error,success:success
          }
      )
    },
    requestByGet: function (method, {dataType = 'text', error, success}) {
      $.ajax({
        url:method,
        type: 'get',
        headers: {
          // "Authorization": "Bearer 123456.gff.Test",
          Authorization: 'Bearer ' + 'eyJhbGciOiJIUzUxMiJ9.eyJzdWIiOiJscyIsImV4cCI6MTY4ODg4ODg5NCwiY3JlYXRlZCI6MTY4ODYyOTY5NDc2OSwiYXV0aG9yaXRpZXMiOlt7ImF1dGhvcml0eSI6InN5czpjbGllbnQ6ZGVsZXRlIn0seyJhdXRob3JpdHkiOiJzeXM6Y2xpZW50OnVwbG9hZCJ9LHsiYXV0aG9yaXR5Ijoic3lzOm1hbmFnZXI6YWRkIn0seyJhdXRob3JpdHkiOiJzeXM6bWFuYWdlcjpkZWxldGUifSx7ImF1dGhvcml0eSI6InN5czptYW5hZ2VyOmVkaXQifSx7ImF1dGhvcml0eSI6InN5czptYW5hZ2VyOnZpZXcifSx7ImF1dGhvcml0eSI6InN5czpyb29tOmFkZCJ9LHsiYXV0aG9yaXR5Ijoic3lzOnJvb206ZGVsZXRlIn0seyJhdXRob3JpdHkiOiJzeXM6cm9vbTplZGl0In0seyJhdXRob3JpdHkiOiJzeXM6cm9vbTp2aWV3In0seyJhdXRob3JpdHkiOiJzeXM6dXNlcjphZGQifSx7ImF1dGhvcml0eSI6InN5czp1c2VyOmRlbGV0ZSJ9LHsiYXV0aG9yaXR5Ijoic3lzOnVzZXI6ZWRpdCJ9LHsiYXV0aG9yaXR5Ijoic3lzOnVzZXI6dmlldyJ9LHsiYXV0aG9yaXR5Ijoic3lzOnZzOmZ1bmMifV19.J0Qyxd-qDVqL4U1bh513XYItAzhH_njXPQcmpUR60r7_ldoYOHrrd1_ueW1AhthDHJKutKRt3_19U5ruIVtb0w',
        },
        dataType: dataType,
        contentType: 'text/xml;charset="UTF-8"',
        error: error,
        success: success
      });
    },
    
  },
  
 
}

</script>

<style scoped>

.setFlexLayout{
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.flexLayoutBottom{
  flex: 1;
  margin: 10px;
  box-sizing: border-box;
}

.titleStyle{
  font-size: 22px;
  margin-bottom: 15px;
}

.overflow{
  height: 75%;
  overflow: auto;
}

</style>