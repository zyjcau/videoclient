function Medical() {
    this.version = '';
    this.sdkVersion = '';
}

Medical.prototype = {
    init: function (config) {
        this.tangoHost=config.tangoHost
        this.host = config.host
        this.token = config.myAccount.token
        this.roomKey = config.myAccount.extension
        this.name = config.myAccount.name
        console.log("Medical--->config",config)
    },
    
    getMyRoomKey(){
        console.log(this.roomKey)
        return this.roomKey
    },
    getMyDisplayName(){
        console.log(this.name)
        return this.name
    },
    requestByPost: function (method, data, {dataType = 'text', error, success}) {
        $.ajax({
            url: this.host + '/' + method,
            type: 'post',
            headers: {
                "Content-Type": "application/x-www-form-urlencoded"
            },
            dataType: dataType,
            contentType: 'text/xml;charset="UTF-8"',
            data: data,
            error: error,
            success: success
        });
    },

    requestByPostFile: function (method, data, {dataType = 'text', error, success}) {
        // console.log("data", data)
        $.ajax({
            url: this.host + '/' + method,
            type: 'post',
            headers: {
                Authorization: 'Bearer ' + this.token,
            },
            dataType: "json",
            cache: false,
            processData: false,
            contentType: false,//'text/xml;charset="UTF-8"',
            data: data,
            error: error,
            success: success
        });
    },

    requestByPostAddMation: function (method, data, {dataType = 'text', error, success}) {
        // console.log("data", data)
        $.ajax({
            url: this.host + '/' + method,
            type: 'post',
            headers: {
                Authorization: 'Bearer ' + this.token,
                "Content-Type": "application/json"
            },
            dataType: "json",
            cache: false,
            processData: false,
            contentType: false,
            data: data,
            error: error,
            success: success
        });
    },

    requestByGet: function (method, {dataType = 'text', error, success}) {
        $.ajax({
            url: this.host + '/' + method,
            type: 'get',
            headers: {
                Authorization: 'Bearer ' + this.token,
                "Content-Type": "application/x-www-form-urlencoded"
            },
            dataType: dataType,
            contentType: 'text/xml;charset="UTF-8"',
            error: error,
            success: success
        });
    },

    requestByDel: function (method, data, {dataType = 'text', error, success}) {
        $.ajax({
            url: this.host + '/' + method,
            type: 'delete',
            headers: {
                // "Authorization": "Bearer 123456.gff.Test",
                Authorization: 'Bearer ' + this.token,
                "Content-Type": "application/x-www-form-urlencoded"
            },
            dataType: dataType,
            contentType: 'text/xml;charset="UTF-8"',
            data: data,
            error: error,
            success: success
        });
    },

    requestByModify: function (method, data, {dataType = 'text', error, success}) {
        $.ajax({
            url: this.host + '/' + method,
            type: 'put',
            headers: {
                // "Authorization": "Bearer 123456.gff.Test",
                Authorization: 'Bearer ' + this.token,
                // "Content-Type": "application/x-www-form-urlencoded"
                "Content-Type": "application/json"
            },
            // dataType: dataType,
            dataType: "json",
            // contentType: 'text/xml;charset="UTF-8"',
            contentType: false,
            data: data,
            error: error,
            success: success
        });
    },

    getMedicalRecordsList: function (pageNum,pageSize,{error, success}) {
        //获取档案列表无参数
        this.requestByGet(
            `medicalRecords/list?pageNum=${pageNum}&pageSize=${pageSize}`,
            {error: error, success: success})
    },
    //pageNum分页的第几页，pageSize分页每一页数据的数量
    getPaginAtionList:function (pageNum,pageSize,{error,success}){
        this.requestByGet(
            `medicalRecords/list?pageNum=${pageNum}&pageSize=${pageSize}`,
            {error:error,success:success}
        )
    },
    
    addMedicalRecords: function (form, {error, success}) {
        // console.log("form", form)
        this.requestByPostFile(
            `medicalRecords/add`,
            // "file":file,
            form,
            {error: error, success: success}
        )
    },
    getMedicalDetails: function (id, {error, success}) {
        //获取档案详情
        this.requestByGet(
            `medicalRecords/details?id=${id}`,
            {error: error, success: success}
        )
    },

    delMedicalDetails: function (id, {error, success}) {
        //删除档案详情
        this.requestByDel(
            `medicalRecords/delete?id=${id}`,
            {},
            {error: error, success: success}
        )
    },
    addMedicalLog: function (data, {error, success}) {
        //添加日志
        this.requestByPostAddMation(
            `medicalLog/add`,
            data,
            {error: error, success: success}
        )
    },
    addRemoteExpert: function (data, {error, success}) {
        //添加远端信息
        this.requestByPostAddMation(
            `medicalExperts/add`,
            data,
            {error: error, success: success}
        )
    },
    exportModulePDFReturnUrl:function (moduleId){
        //导出pdf返回url
        return this.host + '/' + `medicalRecords/lss/load/pdf?id=${moduleId}&&token=${this.token}`
    },
    searchMedicalFileByPatientName:function (patientName,{error,success}){
        this.requestByGet(
            `medicalRecords/list?params%5BpatientName%5D=${patientName}`,
            {error,success}
        )
    },
    addCorrectMedicalRecords:function (form,{error,success}){
        this.requestByPostFile(
            `/approve/add`,
            form,
            {error:error,success:success}
        )
    },
    addCorrect:function (data,{error,success}){
      this.requestByPostAddMation(
          `approve/add`,
          data,
          {error:error,success:success}
      )  
    },
    modifyCorrect(data,{error,success}){
        this.requestByModify(
            `approve/edit`,
            data,
            {error:error,success:success}
        )
    },

    getAllUser:function ({error,success}){
        this.requestByGetSelect(
            `${this.tangoHost}/user/all`,
            {error:error,success:success}
        )
    },
    getCompany:function ({error,success}){
        // this.requestByGetSelect(
        //     `https://hrst.lssvc.cn/manager/getCompany`,
        //     {
        //         error:error,success:success
        //     }
        // )
        $.ajax({
            url:`${this.tangoHost}/manager/getCompany`,
            type: 'get',
            headers: {
                Authorization: this.token,
            },
            dataType: 'json',
            contentType: 'text/xml;charset="UTF-8"',
            error: error,
            success: success
        });
    },
    getEnterprise:function (companyId,{error,success}){
        this.requestByGetSelect(
            `${this.tangoHost}/manager/getDepartment?companyId=${companyId}`,
            {
                error:error,success:success
            }
        )
    },
    getListVideoUser:function (companyId,officeId,{error,success}){
        this.requestByGetSelect(
            `${this.tangoHost}/user/listVideoUser?companyId=${companyId}&officeId=${officeId}`,
            {
                error:error,success:success
            }
        )
    },
    requestByGetSelect: function (method, {dataType = 'text', error, success}) {
        $.ajax({
            url:method,
            type: 'get',
            headers: {
                Authorization: 'Bearer ' + this.token,
            },
            dataType: dataType,
            contentType: 'text/xml;charset="UTF-8"',
            error: error,
            success: success
        });
    },
    // getEnterprise:function ({error,success}){
    //     this.requestByGet(
    //         `https://hrst.lssvc.cn//manager/getDepartment?companyId=18`,
    //         {
    //             error:error,success:success
    //         }
    //     )
    // }
}
