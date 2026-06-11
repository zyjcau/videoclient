function WSAPI() {
    this.version = 'V1.02';

    this.host = '';
    this.auth = '';

    this.gatewayPrefixH323 = '';
    this.gatewayPrefixSIP = '';

    this.recorder = {//记录录制的信息，用于记录状态、停止等
        conferenceID: 0,
        started: false,
        recorderPrefix: '01',
        recorderID: 0
    }
}

WSAPI.prototype.setHost = function (host) {
    this.host = host;
}

WSAPI.prototype.setAuth = function (acct, pwd) {
    this.auth = window.btoa(acct + ':' + pwd);
}

WSAPI.prototype.setAuthDirectly = function (apiCode) {
    this.auth = apiCode;
}

WSAPI.prototype.getPortal = function () {
    if (this.host.startsWith('http')) {
        return this.host
    } else {
        return `https://${this.host}`
    }
}

WSAPI.prototype.setAuthStr = function (auth) {
    this.auth = auth;
}

WSAPI.prototype.setH323Prefix = function (number) {
    this.gatewayPrefixH323 = number;
}

WSAPI.prototype.setSIPPrefix = function (number) {
    this.gatewayPrefixSIP = number;
}

WSAPI.prototype.requestUserAPI = function (action, xml, success, error) {
    $.ajax({
        headers:
            {
                SOAPAction: action,
                Authorization: 'Basic ' + this.auth
            },
        contentType: 'text/xml;charset="UTF-8"',
        dataType: 'xml',
        type: 'post',
        url: this.getPortal() + '/services/v1_1/VidyoPortalUserService',
        data: xml,
        success: success,
        error: error
    });
}

WSAPI.prototype.requestUserAPIByAuth = function (action, auth, xml, success, error) {
    $.ajax({
        headers:
            {
                SOAPAction: action,
                Authorization: 'Basic ' + auth
            },
        contentType: 'text/xml;charset="UTF-8"',
        dataType: 'xml',
        type: 'post',
        url: this.getPortal() + '/services/v1_1/VidyoPortalUserService',
        data: xml,
        success: success,
        error: error
    });
}

WSAPI.prototype.requestAdminAPI = function (action, xml, success, error) {
    $.ajax({
        headers:
            {
                SOAPAction: action,
                Authorization: 'Basic ' + this.auth
            },
        contentType: 'text/xml;charset="UTF-8"',
        dataType: 'xml',
        type: 'post',
        url: this.getPortal() + '/services/v1_1/VidyoPortalAdminService',
        data: xml,
        success: success,
        error: error
    });
}

WSAPI.prototype.getRooms = function (query, success, error) {
    console.log(`getRooms(query:${query})`);
    let xml = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://portal.vidyo.com/admin/v1_1">' +
        '   <soapenv:Header/>' +
        '   <soapenv:Body>' +
        '      <v1:GetRoomsRequest>' +
        '         <v1:Filter>' +
        '            <v1:sortBy>extension</v1:sortBy>' +
        '            <v1:dir>ASC</v1:dir>' +
        '            <v1:query>' + query + '</v1:query>' +
        '         </v1:Filter>' +
        '      </v1:GetRoomsRequest>' +
        '   </soapenv:Body>' +
        '</soapenv:Envelope>';
    this.requestAdminAPI('getRooms', xml, success, error);
}

WSAPI.prototype.getRoomInfo = function (roomKey, success, error) {
    console.log(`getRoomInfo(roomKey:${roomKey})`, typeof roomKey);
    if (!this.isNumber(roomKey) && roomKey.length >= 10) {
        this.getEntityByRoomKey(
            roomKey,
            resp => {
                let room = {
                    roomKey: roomKey,
                    hasPin: this.parseHasPIN(resp) === 'true',
                    roomName: this.parseDisplayName(resp)
                }
                success(room, resp)
            },
            resp => {
                if (resp.status === 500) {
                    let errorMsg = this.parseErrorMessage(resp.responseText)
                    if (errorMsg.startsWith('Room not found for roomKey')) {
                        resp.msg = errorMsg
                        error(resp)
                    } else {
                        error(resp)
                    }
                } else {
                    error(resp)
                }
            })
    } else {
        // //定义解析房间信息函数
        // let api = this
        //
        // function parseRoomInfo(roomXml) {
        //     let rk = api.parseRoomKey(roomXml)
        //     if (rk) {
        //         let room = {
        //             roomKey: rk,
        //             hasPin: api.parseHasPIN(roomXml) === 'true',
        //             roomName: api.parseRoomName(roomXml)
        //         }
        //         success(room, roomXml)
        //     } else {
        //         error({msg: 'getRoomInfo failed！ parseRoomKey failed.'})
        //     }
        // }

        //根据房间号获取房间码和房间名
        this.getRooms(
            roomKey,
            resp => {
                let total = this.parseTotal(resp);
                console.log(total, resp)
                if (total >= 1) {//todo 精准查询房间号

                    let rooms = $(resp).find('ns1\\:room')
                    // console.log(rooms)

                    let isFound = false

                    rooms.each((index, room) => {
                        let extension = this.parseExtension(room)
                        roomKey = roomKey.toString()
                        if (extension === roomKey) {
                            isFound = true
                            // parseRoomInfo(room)
                            let rk = this.parseRoomKey(room)
                            this.getRoomInfo(rk, success, error)
                        }
                    })

                    if (!isFound) {
                        error({msg: 'getRoomInfo failed！ roomKey is not exist(hava some similar).'})
                    }

                } else {
                    error({msg: 'getRoomInfo failed！ roomKey is not exist.'})
                }
            }, error)
    }
}

WSAPI.prototype.inviteToConference = function (conferenceID, invite, success, error) {
    console.log(`inviteToConference( conferenceID:${conferenceID}, invite:${invite})`);
    let xml = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://portal.vidyo.com/admin/v1_1">\n' +
        '   <soapenv:Header/>\n' +
        '   <soapenv:Body>\n' +
        '      <v1:InviteToConferenceRequest>\n' +
        '         <v1:conferenceID>' + conferenceID + '</v1:conferenceID>\n' +
        '         <!--You have a CHOICE of the next 2 items at this level-->\n' +
        '         <v1:entityID>' + invite + '</v1:entityID>\n' +
        // '         <v1:invite>' + invite + '</v1:invite>\n' +
        '         <!--Optional:-->\n' +
        '         <!-- <v1:callFromIdentifier>?</v1:callFromIdentifier> -->\n' +
        '      </v1:InviteToConferenceRequest>\n' +
        '   </soapenv:Body>\n' +
        '</soapenv:Envelope>';
    this.requestAdminAPI('inviteToConference', xml, success, error);
}

WSAPI.prototype.inviteH323SIPToConference = function (conferenceID, invite, success, error) {
    console.log(`inviteH323SIPToConference( conferenceID:${conferenceID}, invite:${invite})`);
    let xml = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://portal.vidyo.com/admin/v1_1">\n' +
        '   <soapenv:Header/>\n' +
        '   <soapenv:Body>\n' +
        '      <v1:InviteToConferenceRequest>\n' +
        '         <v1:conferenceID>' + conferenceID + '</v1:conferenceID>\n' +
        '         <!--You have a CHOICE of the next 2 items at this level-->\n' +
        // '         <v1:entityID>'+invite+'</v1:entityID>\n' +
        '         <v1:invite>' + invite + '</v1:invite>\n' +
        '         <!--Optional:-->\n' +
        '         <!-- <v1:callFromIdentifier>?</v1:callFromIdentifier> -->\n' +
        '      </v1:InviteToConferenceRequest>\n' +
        '   </soapenv:Body>\n' +
        '</soapenv:Envelope>';
    this.requestAdminAPI('inviteToConference', xml, success, error);
}

WSAPI.prototype.muteAudio = function (conferenceID, participantID, success, error) {
    console.log(`muteAudio(conferenceID:${conferenceID},participantID:${participantID})`);
    let xml = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://portal.vidyo.com/admin/v1_1">' +
        '   <soapenv:Header/>' +
        '   <soapenv:Body>' +
        '      <v1:MuteAudioRequest>' +
        '         <v1:conferenceID>' + conferenceID + '</v1:conferenceID>' +
        '         <v1:participantID>' + participantID + '</v1:participantID>' +
        '      </v1:MuteAudioRequest>' +
        '   </soapenv:Body>' +
        '</soapenv:Envelope>';
    this.requestAdminAPI('muteAudio', xml, success, error);
}

WSAPI.prototype.unmuteAudio = function (conferenceID, participantID, success, error) {
    console.log(`unmuteAudio(conferenceID:${conferenceID},participantID:${participantID})`);
    let xml = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://portal.vidyo.com/admin/v1_1">' +
        '   <soapenv:Header/>' +
        '   <soapenv:Body>' +
        '      <v1:UnmuteAudioRequest>' +
        '         <v1:conferenceID>' + conferenceID + '</v1:conferenceID>' +
        '         <v1:participantID>' + participantID + '</v1:participantID>' +
        '      </v1:UnmuteAudioRequest>' +
        '   </soapenv:Body>' +
        '</soapenv:Envelope>';
    this.requestAdminAPI('unmuteAudio', xml, success, error);
}

WSAPI.prototype.leaveConference = function (conferenceID, participantID, success, error) {
    console.log(`leaveConference(conferenceID:${conferenceID},participantID:${participantID})`);
    let xml = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://portal.vidyo.com/admin/v1_1">' +
        '   <soapenv:Header/>' +
        '   <soapenv:Body>' +
        '      <v1:LeaveConferenceRequest>' +
        '         <v1:conferenceID>' + conferenceID + '</v1:conferenceID>' +
        '         <v1:participantID>' + participantID + '</v1:participantID>' +
        '      </v1:LeaveConferenceRequest>' +
        '   </soapenv:Body>' +
        '</soapenv:Envelope>';
    this.requestAdminAPI('leaveConference', xml, success, error);
}

WSAPI.prototype.getEntityByRoomKey = function (roomKey, success, error) {
    console.log(`getEntityByRoomKey(roomKey:${roomKey})`);
    let xml = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://portal.vidyo.com/user/v1_1">' +
        '   <soapenv:Header/>' +
        '   <soapenv:Body>' +
        '      <v1:GetEntityByRoomKeyRequest>' +
        '         <v1:roomKey>' + roomKey + '</v1:roomKey>' +
        '      </v1:GetEntityByRoomKeyRequest>' +
        '   </soapenv:Body>' +
        '</soapenv:Envelope>';
    this.requestUserAPI('getEntityByRoomKey', xml, success, error);
}

WSAPI.prototype.getVidyoReplayLibrary = function (success, error) {
    console.log(`getVidyoReplayLibrary()`);
    let xml = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://portal.vidyo.com/user/v1_1">\n' +
        '   <soapenv:Header/>\n' +
        '   <soapenv:Body>\n' +
        '      <v1:GetVidyoReplayLibraryRequest/>\n' +
        '   </soapenv:Body>\n' +
        '</soapenv:Envelope>';
    this.requestUserAPI('getVidyoReplayLibrary', xml, success, error);
}

WSAPI.prototype.startRecording = function (conferenceID, recorderPrefix, success, error) {
    console.log(`startRecording(conferenceID:${conferenceID},recorderPrefix:${recorderPrefix})`);
    let xml = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://portal.vidyo.com/admin/v1_1">\n' +
        '   <soapenv:Header/>\n' +
        '   <soapenv:Body>\n' +
        '      <v1:StartRecordingRequest>\n' +
        '         <v1:conferenceID>' + conferenceID + '</v1:conferenceID>\n' +
        '         <v1:recorderPrefix>' + recorderPrefix + '</v1:recorderPrefix>\n' +
        '         <v1:webcast></v1:webcast>\n' +
        '      </v1:StartRecordingRequest>\n' +
        '   </soapenv:Body>\n' +
        '</soapenv:Envelope>';
    this.requestAdminAPI('startRecording', xml, success, error);
}
WSAPI.prototype.getParticipants = function (conferenceId, success, error) {
    console.log(`getParticipants(conferenceId:${conferenceId})`);
    let xml = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://portal.vidyo.com/admin/v1_1">' +
        '   <soapenv:Header/>' +
        '   <soapenv:Body>' +
        '      <v1:GetParticipantsRequest>' +
        '         <v1:conferenceID>' + conferenceId + '</v1:conferenceID>' +
        '         <v1:Filter>' +
        '            <v1:start></v1:start>' +
        '            <v1:limit></v1:limit>' +
        '            <v1:sortBy></v1:sortBy>' +
        '            <v1:dir></v1:dir>' +
        '            <v1:query></v1:query>' +
        '         </v1:Filter>' +
        '      </v1:GetParticipantsRequest>' +
        '   </soapenv:Body>' +
        '</soapenv:Envelope>';
    this.requestAdminAPI('getParticipants', xml, success, error);
}
WSAPI.prototype.stopRecording = function (conferenceID, recorderID, success, error) {
    console.log(`stopRecording(conferenceID:${conferenceID},recorderID:${recorderID})`);
    let xml = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://portal.vidyo.com/admin/v1_1">\n' +
        '   <soapenv:Header/>\n' +
        '   <soapenv:Body>\n' +
        '      <v1:StopRecordingRequest>\n' +
        '         <v1:conferenceID>' + conferenceID + '</v1:conferenceID>\n' +
        '         <v1:recorderID>' + recorderID + '</v1:recorderID>\n' +
        '      </v1:StopRecordingRequest>\n' +
        '   </soapenv:Body>\n' +
        '</soapenv:Envelope>';
    this.requestAdminAPI('stopRecording', xml, success, error);
}

WSAPI.prototype.updateRecord = function (id, title, tags, success, error) {
    //todo 
    let replayServerUrl = 'https://rp.lssvc.cn'
    var settings = {
        "url": `${replayServerUrl}/replay/services/VidyoReplayContentManagementService`,
        "method": "POST",
        "timeout": 0,
        "headers": {
            "Content-Type": "text/xml;charset=UTF-8",
            "SOAPAction": "UpdateRecord",
            "Authorization": `Basic ${this.auth}`
        },
        "data": "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
            "xmlns:apis=\"http://replay.vidyo.com/apiservice\">" +
            "<soapenv:Header/>" +
            "<soapenv:Body>" +
            "<apis:UpdateRecordRequest>" +
            "<apis:id>" + id + "</apis:id>" +
            "<apis:title>" + title + "</apis:title>" +
            "<apis:comments></apis:comments>" +
            "<apis:tags>" + tags + "</apis:tags>" +
            "<apis:recordScope></apis:recordScope>" +
            "<apis:pin></apis:pin>" +
            "<apis:locked></apis:locked>" +
            "</apis:UpdateRecordRequest>" +
            "</soapenv:Body>" +
            "</soapenv:Envelope>",
        success: success,
        error: error
    };

    $.ajax(settings).done(function (response) {
        console.log(response);
    });
}

WSAPI.prototype.RecordsSearch = function (id, success, error) {
    //todo 
    let replayServerUrl = 'https://rp.lssvc.cn'
    var settings = {
        "url": `${replayServerUrl}/replay/services/VidyoReplayContentManagementService`,
        "method": "POST",
        "timeout": 0,
        "headers": {
            "Content-Type": "text/xml;charset=UTF-8",
            "SOAPAction": "RecordsSearch",
            "Authorization": `Basic ${this.auth}`,
            "Cookie": "JSESSIONID=9BCED7A262019488AB540A2D1AFF5F29"
        },
        "data": "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
            "xmlns:apis=\"http://replay.vidyo.com/apiservice\">" +
            "<soapenv:Header/>" +
            "<soapenv:Body>" +
            "<apis:RecordsSearchRequest>" +
            "<apis:tenantName></apis:tenantName>" +
            "<apis:roomFilter></apis:roomFilter>" +
            "<apis:usernameFilter></apis:usernameFilter>" +
            "<apis:query>" + id + "</apis:query>" +
            "<apis:recordScope></apis:recordScope>" +
            "<apis:sortBy></apis:sortBy>" +
            "<apis:dir></apis:dir>" +
            "<apis:limit></apis:limit>" +
            "<apis:start></apis:start>" +
            "<apis:webcast></apis:webcast>" +
            "</apis:RecordsSearchRequest>" +
            "</soapenv:Body>" +
            "</soapenv:Envelope>",
        success: success,
        error: error
    };

    $.ajax(settings).done(function (response) {
        console.log(response);
    });
}

WSAPI.prototype.getModeratorURLWithToken = function (auth, roomId, success, error) {
    console.log(`> getModeratorURLWithToken(auth:${auth},roomId:${roomId})`);
    let xml = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://portal.vidyo.com/user/v1_1">' +
        '   <soapenv:Header/>' +
        '   <soapenv:Body>' +
        '      <v1:GetModeratorURLWithTokenRequest>' +
        '         <v1:roomID>' + roomId + '</v1:roomID>' +
        '      </v1:GetModeratorURLWithTokenRequest>' +
        '   </soapenv:Body>' +
        '</soapenv:Envelope>'
    this.requestUserAPIByAuth('getModeratorURLWithToken', auth, xml, success, error);
}

WSAPI.prototype.parseGuestIdToParticipantId = function (userId) {
    return userId.split('_')[1];
}

WSAPI.prototype.find = function (xml, nodeName) {
    return $(xml).find(nodeName).first().text();
}

WSAPI.prototype.parseTotal = function (roomsXml) {
    let totalStr = this.find(roomsXml, "ns1\\:total");
    return Number(totalStr);
}

WSAPI.prototype.parseErrorMessage = function (xml) {
    return this.find(xml, "ns1\\:ErrorMessage");
}

WSAPI.prototype.parseRoomName = function (roomsXml) {
    return this.find(roomsXml, "ns1\\:name");
}

WSAPI.prototype.parseDisplayName = function (roomsXml) {
    return this.find(roomsXml, "ns1\\:displayName");
}

WSAPI.prototype.parseHasPIN = function (roomsXml) {
    return this.find(roomsXml, "ns1\\:hasPIN");
}

WSAPI.prototype.parseRoomId = function (roomsXml) {
    return this.find(roomsXml, "ns1\\:roomID");
}

WSAPI.prototype.parseEntityId = function (myAccountXml) {
    return this.find(myAccountXml, "ns1\\:entityID");
}

WSAPI.prototype.parseExtension = function (myAccountXml) {
    return this.find(myAccountXml, "ns1\\:extension");
}

WSAPI.prototype.parseRoomKey = function (roomsXml) {
    // console.log(`parseRoomKey:`);
    // console.log(roomsXml);
    let roomURL = this.find(roomsXml, "ns1\\:roomURL");
    let roomKey;

    if (roomURL === undefined || roomURL === '') {
        return;
    }

    if (roomURL.indexOf('key=') !== -1) {
        roomKey = roomURL.substring(roomURL.lastIndexOf('=') + 1, roomURL.length);
    } else {
        roomKey = roomURL.substring(roomURL.lastIndexOf('join/') + 5, roomURL.length);
    }

    return roomKey
}

WSAPI.prototype.isNumber = function (val) {
    let regPos = /^\d+(\.\d+)?$/ //非负浮点数
    let regNeg = /^(-(([0-9]+\.[0-9]*[1-9][0-9]*)|([0-9]*[1-9][0-9]*\.[0-9]+)|([0-9]*[1-9][0-9]*)))$/ //负浮点数
    return regPos.test(val) || regNeg.test(val)
}