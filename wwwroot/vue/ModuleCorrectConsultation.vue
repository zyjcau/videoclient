<template>
<!--  修改审批组件-->
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
          <el-input style="width: 70%" :disabled="true" v-model="param.approve.phone"></el-input>
        </div>
      </el-col>
    </el-row>
  </div>
  <div>
    <span>查房时间：</span>
    <span>{{param.approve.applyTime}}</span>
  </div>
  <div>
    <el-collapse accordion>
      <el-collapse-item :title=`查房目的：`>
        <div>
          日志详情：{{param.approve.appleContent}}
        </div>
      </el-collapse-item>
    </el-collapse>
  </div>
  <div>
    <div class="titleStyle">
      远端专家信息
    </div>
    <el-descriptions border :column="5" v-for="item in arr" :key="item.id">
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
  <div class="textAlign">
<!--    <el-button class="inlineBlock" type="danger">取消</el-button>-->
    <el-button class="inlineBlock" type="primary" @click="dialogReject = true">驳回</el-button>
    <el-button class="inlineBlock" type="primary" @click="onRatify">批准</el-button>
  </div>
<!--  驳回信息填写dialog-->
  <el-dialog
  v-model="dialogReject"
  title="驳回信息"
  >
    <div>
      <el-input
          v-model="rejectMsg"
          :rows="2"
          type="textarea"
          placeholder="请输入驳回原因"
      />
    </div>
<!--    <el-button>sssss</el-button>-->
    <div class="textAlign">
      <el-button class="inlineBlock" type="primary" @click="onDefineReject">确认驳回</el-button>
      <el-button class="inlineBlock" type="primary" @click="dialogReject=false">取消</el-button>
    </div>
  </el-dialog>
</template>

<script>
module.exports={
  mounted(){
    this.modifyCorrect()
  },
  props:{
    param:{
      type: Object,
      required: true
    },
    arr:{
      type:Array,
      required: true
    }
  },
  data(){
    return{
      ratify:{
        //审批通过
        // status:"2",
        // medicalId:'',//档案id
        // approveBy:'',//审批人
        // approveById:'',//审批人id
        // appleHospital:'',//申请医院
        // phone:''//申请人联系方式
      },
      rejectMsg:"",
      dialogReject:false,
    }
  
  },
  methods:{
    onRatify(){
      //点击批准按钮
      this.ratify.status = 2
      med.modifyCorrect(
          JSON.stringify(this.ratify),
          {
            error:resp=>{
              console.log("修改审批失败",resp)
            },
            success:resp=>{
              if(resp.code == 200){
                ElementPlus.ElMessage({
                  message:`修改审批成功`,
                  type:`success`,
                })
                this.$emit('modifycorrect-success')
              }else{
                ElementPlus.ElMessage({
                  message:`修改失败请联系管理员`,
                  type:`error`,
                })
              }
            }
          }
      )
    },
    onDefineReject(){
    //  点击驳回按钮
      this.ratify.status = 3
      this.ratify.msg = this.rejectMsg
      med.modifyCorrect(
          JSON.stringify(this.ratify),
          {
            error:resp=>{
              console.log("驳回失败")
            },
            success:resp=>{
              ElementPlus.ElMessage({
                message:`成功驳回`,
                type:`success`
              })
              this.dialogReject = false
              this.$emit('modifycorrect-success')
            }
          }
      )
    },
    modifyCorrect(){
      this.ratify.id = this.param.approve.id
      this.ratify.medicalId = this.param.id
      this.ratify.approveBy = this.param.approve.approveBy
      this.ratify.approveById = this.param.approve.approveById
      this.ratify.appleHospital = this.param.doctor.hospitalName
      this.ratify.phone = this.param.approve.phone
    }
  }
}
</script>

<style>
.margin{
  margin: 0 0 15px 10px;
}

.textAlign{
  text-align: right;
  margin-top: 10px;
}

.inlineBlock{
  display: inline-block;
}
</style>