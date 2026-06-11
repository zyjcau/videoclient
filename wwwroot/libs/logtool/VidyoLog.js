function VidyoLog(containerId) {
	var logLines = [];
	var xmlTrace = {};
	var logRecordsFilter = "";
	var logServersRecordsIndex = []; /* store index per server */
	var logServers = []; /* store servers */
	var serverId = 1;
	var searchString = "";
	var vidyoStats;
	var stopPoll = false;
	
	this.AddServer = function(serverName, serverHOST) {
		if (serverHOST && !serverHOST.startsWith("http"))
			serverURL = "http://" + serverHOST;
		else
			serverURL = serverHOST;
		var server = { name: serverName, url: serverURL, id: serverId, colorId: serverId%10, index: 0, outstandingRequest: false };
		serverId++;
		
		logServers[server.id] = server;
		return server;
	}
	this.RemoveServer = function(serverHOST) {
		if (serverHOST && !serverHOST.startsWith("http"))
			serverURL = "http://" + serverHOST;
		else
			serverURL = serverHOST;
		logServers.forEach(function(logServer) {
			if (logServer.url == serverURL)
				delete logServers[logServer.id];
		});
	}
	this.StartPoll = function() {
		stopPoll = false;
	}
	this.StopPoll = function() {
		stopPoll = true;
	}
	this.Search = function(string) {
		searchString = string;
		if (searchString == "") {
			$(".logLine").show();
		} else {
			for (i = 0; i < logLines.length; i++) {
				var logLine = logLines[i];
				if (SearchFilter(logLine, searchString) == true) {
					$( "#" + logLine.id).hide();
				}
			}
		}
	}
	 
	this.SetServerSideFilter = function(filter) {
		logRecordsFilter = filter;
	}
	this.AddLogRecords = function(logRecords, server) {
		for (recordId in logRecords) {
			record = logRecords[recordId];
			var logLine = new Object();
			logLine.index = record.index;
			logLine.serverName = server.name;
			logLine.serverId = "serverID-" + server.id;
			logLine.colorId = "colorID-" + server.colorId;
			logLine.id = logLine.serverId + "-" + record.index;
			logLine.time =  new Date(record.eventTime/1000000);
			logLine.level = record.level;
			logLine.category = record.categoryName;
			logLine.threadName = record.threadName;
			logLine.threadId = record.threadId;
			logLine.functionLine = record.file+record.file+ ':' + record.line;
			logLine.functionLineShort = logLine.functionLine.split("/").pop().replace(":", "-").replace(".", "_");
			
			logLine.functionName = record.functionName;
			logLine.body = record.message;
			
			logLine.levelEncoded = EncodeStringForAttr(logLine.level);
			logLine.categoryEncoded = EncodeStringForAttr(logLine.category);
			logLine.functionLineShortEncoded = EncodeStringForAttr(logLine.functionLineShort);
			logLine.functionNameEncoded = EncodeStringForAttr(logLine.functionName);
			/* order lines */
			OrderAndRender(logLine);
		}
		return;
	}
	function ParseDesktopLog(logLineUnparsed, id) {
		/* for the first 4 variables the delimiter is a space (multi) */
		var logLineTags = logLineUnparsed.split(/ +/, 5);
		if (!logLineTags || logLineTags.length != 5)
			return null;
		
		/* For some strange reason, the "MetaData" is sometimes in the wrong location. Especially for LmiSignaling */
		if (logLineTags[4] == "[") {
			/* metadata before body separate the rest of meta data after body and remove last "]"*/
			var restOfMetaData = logLineUnparsed.slice(logLineUnparsed.indexOf(" [ ") + 3, logLineUnparsed.indexOf(" ] "));
			/* The body is after metadata */
			var indexOfBodyBegin = logLineUnparsed.indexOf(" ] ") + 3;
			/* the end of body is hard to find. the best delimiter seems to be " [ " */
			var indexOfBodyEnd = logLineUnparsed.length -1;			
		} else {
			/* Body before metadata */
			var indexOfBodyBegin = logLineUnparsed.indexOf(logLineTags[4]);
			/* the end of body is hard to find. the best delimiter seems to be " [ " */
			var indexOfBodyEnd = logLineUnparsed.indexOf(" [ ");
			/* separate the rest of meta data after body and remove last "]"*/
			var restOfMetaData = logLineUnparsed.slice(indexOfBodyEnd + 3, logLineUnparsed.length -1 );
		}
		var logLine = new Object();
		var logLineRestTags = restOfMetaData.split(", ", 3);
		
		/* log does not contain year, assume current */
		var currentYear = new Date().getFullYear();
		var timeMonthDay = logLineTags[0].split(/[\/ -.:]/, 6);
		var timeHourMinSec = logLineTags[1].split(/[\/ -.:]/, 6);
		logLine.id = "logLine" + id;
		logLine.time =  new Date(currentYear, timeMonthDay[0] - 1, timeMonthDay[1], timeHourMinSec[0], timeHourMinSec[1], timeHourMinSec[2], timeHourMinSec[3]);
		
		/* check for valid header */
		if(logLine.time.getDate()) {			
			logLine.level = logLineTags[2];
			logLine.category = logLineTags[3];
			logLine.threadName = logLineRestTags[0];
			logLine.functionLine = logLineRestTags[2];
			logLine.functionLineShort = logLine.functionLine.split("/").pop().replace(":", "-").replace(".", "_");
			logLine.functionName = logLineRestTags[1];
			logLine.body = logLineUnparsed.slice(indexOfBodyBegin, indexOfBodyEnd);
			logLine.index = id;
		} else {		
			return null;
		}
		return logLine;
	}
	function ParseSDKLog(logLineUnparsed, id) {
		var logLineTags = logLineUnparsed.split(": ", 6);
		if (!logLineTags || logLineTags.length != 6)
			return null;
		var indexOfLastMarker = logLineUnparsed.indexOf(logLineTags[5]) + logLineTags[5].length + 2;
		var logLine = new Object();
		
		var timeParts = logLineTags[0].split(/[\/ -.:]/, 7);
		logLine.id = "logLine" + id;
		logLine.time =  new Date(timeParts[0], timeParts[1] - 1, timeParts[2], timeParts[3], timeParts[4], timeParts[5], timeParts[6]);
		
		/* check for valid header */
		if(logLine.time.getFullYear()) {			
			logLine.level = logLineTags[1];
			logLine.category = logLineTags[2];
			logLine.threadName = logLineTags[3];
			logLine.functionLine = logLineTags[4];
			logLine.functionLineShort = logLine.functionLine.split("/").pop().replace(":", "-").replace(".", "_");
			logLine.functionName = logLineTags[5];
			logLine.body = logLineUnparsed.slice(indexOfLastMarker);	
			logLine.index = id;
		} else {		
			return null;
		}
		return logLine;
	}
	function ParseSplunkLog(logLineUnparsed, id) {
		var logLine = new Object();
		
		var logJson = JSON.parse(logLineUnparsed);
		
		logLine.id = "logLine" + id;
		logLine.time =  VidyoParseLogDate(logJson.result.logTimeStamp);
		
		/* check for valid header */
		if(logLine.time.getFullYear()) {			
			logLine.level = "INFO";
			logLine.category = "Splunk";
			logLine.threadName = "";
			logLine.functionLine = "";
			logLine.functionLineShort = "";
			logLine.functionName = "SplunkLog";
			logLine.body = logJson.result.JSON;	
			logLine.index = id;
		} else {		
			return null;
		}
		return logLine;
	}
	function ParseErrorLog(id) {
		var logLine = new Object();
		logLine.time = new Date();
		logLine.level = "LOG_ERROR";
		logLine.category = "LogParser";
		logLine.threadName = "N/A";
		logLine.functionLine = "log-" + id;
		logLine.functionLineShort = "N/A";
		logLine.functionName = "N/A";
		logLine.body = "Error parsing log line " + id;
		logLine.index = id;
		return logLine;
	}
	this.ProcessLogFile = function (logFile) {
		var lineNumber = 1;
		logFileSplit = logFile.split('\n');
		var i=0;
		var lineProcess=function () {
		  	var logLineUnparsed = logFileSplit[i];
		  	var logLine = null;

			if (!logLineUnparsed || logLineUnparsed.length == 0) {
				i++;
				return;
			} else {
				var logLineFullUnparsed = logFileSplit[i];
				if (logLineUnparsed[2] == '-') {
					logLine = ParseDesktopLog(logLineFullUnparsed, lineNumber);
				} else if (logLineUnparsed[0] == '{') {
					logLine = ParseSplunkLog(logLineFullUnparsed, lineNumber);
				} else {
					/* if the next line does not begin with a number, concatenate both lines! */
					while (logFileSplit[i + 1] && (logFileSplit[i + 1][0] < '0' || logFileSplit[i + 1][0] > '9')) {
						logLineFullUnparsed = logLineFullUnparsed.concat(logFileSplit[i + 1]);
							i = i + 1;
					}
					logLine = ParseSDKLog(logLineFullUnparsed, lineNumber);
				}
			}
			if (!logLine) {
				logLine = ParseErrorLog(i);
			}
			logLine.levelEncoded = EncodeStringForAttr(logLine.level);
			logLine.categoryEncoded = EncodeStringForAttr(logLine.category);
			logLine.functionLineShortEncoded = EncodeStringForAttr(logLine.functionLineShort);
			logLine.functionNameEncoded = EncodeStringForAttr(logLine.functionName);
			lineNumber++;

			/* order lines */
			OrderAndRender(logLine);
	
			$("#processing").html("Loading: "+Math.round(i/logFileSplit.length*10000)/100+" %");
			i++;
			if(i==logFileSplit.length-1) {
				clearInterval(x);
				$("#processing").html("Loaded");
			} 
		};	
		var x=setInterval(lineProcess,1);
		/* for (var i = 0; i < logFileSplit.length; i++) {
			}*/
	}
	
	this.ScrollToLogRecord = function(logRecord, server) {
		var recordId = "serverID-" + server.id + "-" + logRecord.index;
		$("#" + containerId).animate({
			scrollTop: $("#" + containerId).scrollTop() - $("#" + containerId).offset().top + $("#" + recordId).offset().top
		}, 0);
	}
	this.SetNewContainer = function(containerId) {
		$("#" + containerId).html("");
		logLines = [];
		vidyoStats = new VidyoStats(containerId);
	}

	function EncodeStringForAttr(string) {
		return window.btoa(string).replace(/=+/, "");
	}

	function getDate(date) {
		function addZero(x,n) {
			if (x.toString().length < n) {
				x = "0" + x;
			}
			return x;
		}
		return addZero(date.getHours(), 2) + ":" + addZero(date.getMinutes(), 2) + ":" + addZero(date.getSeconds(), 2) + "." + addZero(date.getMilliseconds(), 3);
		
	}
	function OrderAndRender(logLine) {
		/* order lines */
		if (logLines.length) {
			var foundLogLine = null;
			for (i = logLines.length - 1; i >= 0; i--) {
				var existingLogLine = logLines[i];
				if (logLine.time >= existingLogLine.time) {
					foundLogLine = existingLogLine;
					break;
				}
			}
			if (foundLogLine) {
				AppendLogLine(logLine, foundLogLine);
				logLines.splice(i+1, 0, logLine);
			} else {
				AppendLogLine(logLine, null);
				logLines.splice(0, 0, logLine);
			}
		} else {
			AppendLogLine(logLine, null);
			logLines.push(logLine);
		}	
	}
	function AppendLogLine(logLine, currentLogLine) {
		parsedLogBodyElements = WrapXML(logLine);
		sentOrReceived = logLine.level;
		parsedLogBodyElements.forEach(function(logBody) {
			if (logBody.attributes.stats) {
				AppendLogLineHTMLStats(logLine, currentLogLine, logBody);
			} else {
				AppendLogLineHTML(logLine, currentLogLine, logBody);
			}
		});
	}
	function AppendLogLineHTMLStats(logLine, currentLogLine, logBody) {
		var tr = $('<tr/>', {
			id: logLine.id,
			class: "logLine table-light " + logLine.levelEncoded + " " + logLine.categoryEncoded + " " + logLine.functionLineShortEncoded + " " + logLine.functionNameEncoded
		});
		var tdBody = $('<td/>', {
			class: "logBody " + logLine.level,
			html: logBody.body.html(),
		});
		/* JSON stats */
		tdBody.append("<div id='statsJson_" + logLine.id + "'></div>");
		var tdSentRequest = $('<td/>', {
			class: "logGoTo"
		});
		var tdReceivedRequest = $('<td/>', {
			class: "logGoTo"
		});
		var tdSentResponse = $('<td/>', {
			class: "logGoTo"
		});
		var tdReceivedResponse = $('<td/>', {
			class: "logGoTo"
		});
		var tdIndex = $('<th/>', {
			class: "logIndex " + logLine.colorId,
			title: logLine.serverName,
			text: logLine.index,
			scope: "row"
		});
		var tdTime = $('<td/>', {
			class: "logTime",
			text: getDate(logLine.time)
		});
		
		tr.append(tdIndex).append(tdTime).append(tdSentRequest).append(tdReceivedRequest).append(tdSentResponse).append(tdReceivedResponse).append(tdBody);
		
		if (currentLogLine) {
			$( "#" + currentLogLine.id).after(tr);
		} else {
			$( "#logTableBody").prepend(tr);
		}
		if (SearchFilter(logLine, searchString) == true) {
			$( "#" + logLine.id).hide();
		}
		
		/* JSON stats must be appended at the end after the HTML has been renderered for expand/collapse to function */
		$("#statsJson_" + logLine.id).JSONView(logLine.body, { collapsed: true });		
	}
	function AppendLogLineHTML(logLine, currentLogLine, logBody) {
		var tdSentRequest = $('<td/>', {
			class: "logGoTo"
		});
		var tdReceivedRequest = $('<td/>', {
			class: "logGoTo"
		});
		var tdSentResponse = $('<td/>', {
			class: "logGoTo"
		});
		var tdReceivedResponse = $('<td/>', {
			class: "logGoTo"
		});
		var threadId = '';
		if (logLine.threadId) {
			threadId = ' (0x' + logLine.threadId.toString(16) + ')'
		}
		var tdBody = $('<td/>', {
			class: "logBody " + logLine.level,
			title: ' Level: ' + logLine.level + '\n Category: ' + logLine.category + '\n Thread: ' + logLine.threadName + threadId + '\n File: ' + logLine.functionLineShort + '\n Function: ' + logLine.functionName,
			html: logBody.body.html()
		});
		
		/* check if the xml has been parsed */
		if (logBody.attributes.to && logBody.attributes.from && logBody.attributes.id) {
			var leg1_sentRequest = false;
			var leg2_receivedRequest = false;
			var leg3_sentResponse = false;
			var leg4_receivedResponse = false;
			var traceId;
			/* traceId format = id_to_from */
			if (logLine.level == "SENT" || logLine.level == "LMI_LOGLEVEL_SENT" || logLine.level == "VIDYO_LOGLEVEL_SENT") {
				/* leg1 or leg3 */
				/* reverse from and to to see if it's been received before */
				traceId = logBody.attributes.id + "_" + logBody.attributes.from + "_" + logBody.attributes.to;
				if (xmlTrace[traceId]) {
					leg3_sentResponse = true;
				} else {
					traceId = logBody.attributes.id + "_" + logBody.attributes.to + "_" + logBody.attributes.from;
					leg1_sentRequest = true;
					xmlTrace[traceId] = traceId;
				}
			} else if (logLine.level == "RECEIVED" || logLine.level == "LMI_LOGLEVEL_RECEIVED" || logLine.level == "VIDYO_LOGLEVEL_RECEIVED") {
				/* leg2 or leg4 */
				/* reverse from and to to see if it's been sent before */
				traceId = logBody.attributes.id + "_" + logBody.attributes.from + "_" + logBody.attributes.to;
				if (xmlTrace[traceId]) {
					leg4_receivedResponse = true;
				} else {
					traceId = logBody.attributes.id + "_" + logBody.attributes.to + "_" + logBody.attributes.from;
					leg2_receivedRequest = true;
					xmlTrace[traceId] = traceId;
				}
			}
			
			var sentRequest = btoa("leg1_sentRequest" + traceId);
			var receivedRequest = btoa("leg2_receivedRequest" + traceId);
			var sentResponse = btoa("leg3_sentResponse" + traceId);
			var receivedResponse = btoa("leg4_receivedResponse" + traceId);
			if (leg1_sentRequest) {
				/* this logLine is sentRequest */
				tdSentRequest.attr("id",sentRequest);
				tdSentRequest.html("<a>&#8594;</a>");
				tdReceivedRequest.html("<a href='#" + receivedRequest + "'>&#8595;</a>");
				tdSentResponse.html("<a href='#" + sentResponse + "'>&#8595;</a>");
				tdReceivedResponse.html("<a href='#" + receivedResponse + "'>&#8595;</a>");
			} else if (leg2_receivedRequest) {
				/* this logLine is receivedRequest */
				tdSentRequest.html("<a href='#" + sentRequest + "'>&#8593;</a>");
				tdReceivedRequest.attr("id",receivedRequest);
				tdReceivedRequest.html("<a>&#8594;</a>");
				tdSentResponse.html("<a href='#" + sentResponse + "'>&#8595;</a>");
				tdReceivedResponse.html("<a href='#" + receivedResponse + "'>&#8595;</a>");
			} else if (leg3_sentResponse) {
				/* this logLine is sentResponse */
				tdSentRequest.html("<a href='#" + sentRequest + "'>&#8593;</a>");
				tdReceivedRequest.html("<a href='#" + receivedRequest + "'>&#8593;</a>");
				tdSentResponse.attr("id",sentResponse);
				tdSentResponse.html("<a>&#8594;</a>");
				tdReceivedResponse.html("<a href='#" + receivedResponse + "'>&#8595;</a>");
			} else if (leg4_receivedResponse) {
				/* this logLine is SentRequest */
				tdSentRequest.html("<a href='#" + sentRequest + "'>&#8593;</a>");
				tdReceivedRequest.html("<a href='#" + receivedRequest + "'>&#8593;</a>");
				tdSentResponse.html("<a href='#" + sentResponse + "'>&#8593;</a>");
				tdReceivedResponse.attr("id",receivedResponse);
				tdReceivedResponse.html("<a>&#8594;</a>");
			}
		}
		var colorRow = " "
		if (logLine.level == "WARNING") {
			colorRow = "table-warning ";
		} else if (logLine.level == "ERROR") {
			colorRow = "table-danger ";
		} else if (logLine.level == "SENT") {
			colorRow = "table-success ";
		} else if (logLine.level == "RECEIVED") {
			colorRow = "table-info ";
		}
		var tr = $('<tr/>', {
			id: logLine.id,
			class: "logLine " + colorRow + logLine.levelEncoded + " " + logLine.categoryEncoded + " " + logLine.functionLineShortEncoded + " " + logLine.functionNameEncoded
		});
		var tdIndex = $('<th/>', {
			class: "logIndex " + logLine.colorId,
			title: logLine.serverName,
			text: logLine.index,
			scope: "row"
		});
		var tdTime = $('<td/>', {
			class: "logTime",
			text: getDate(logLine.time)
		});
		tr.append(tdIndex).append(tdTime).append(tdSentRequest).append(tdReceivedRequest).append(tdSentResponse).append(tdReceivedResponse).append(tdBody);
		
		if (currentLogLine) {
			$( "#" + currentLogLine.id).after(tr);
		} else {
			$( "#logTableBody").prepend(tr);
		}
		if (SearchFilter(logLine, searchString) == true) {
			$( "#" + logLine.id).hide();
		}
	}
	
	function RecurseHTMLFromXMLObj(xmlObj, output, level) {
		level = level + 1;
		var tagName = xmlObj.tagName;
		var beginObj = $("<span class='xmlBeginObj " + tagName+ "'/>");
		beginObj.text(tagName);
		
		var tabs = "";
		for (var i = 0; i < level; i++) {
			tabs = tabs + '  ';
		}
		
		output.append(tabs + "<").append(beginObj);
		var attrsString = "";
		
		$.each(xmlObj.attributes, function() {
			var nameObj = $("<span class='xmlNameObj xmlName" + this.name + "'/>");
			nameObj.text(this.name);
			var valueObj = $("<span class='xmlValueObj xmlValue" + this.name + "'/>");
			valueObj.text(this.value);
			output.append(" ").append(nameObj).append("='").append(valueObj).append("'");
		});
		output.append(">");
		
		if ($(xmlObj).text()) {
			var textWithoutChildren = $(xmlObj).clone().children().remove().end().text();
			if (textWithoutChildren) {
				bodyObj = $("<span class='xmlBodyObj " + tagName+ "'/>");
				bodyObj.text(textWithoutChildren);
				output.append(bodyObj);
			}
		}
		
		if (xmlObj.children.length) /* Do not create a next line if there are no children */
			output.append("</br>");
		
		$.each(xmlObj.children, function() {
			RecurseHTMLFromXMLObj(this, output, level);
			output.append("</br>");
		});
		var endObj =  $("<span class='xmlEndObj " + tagName+ "'/>");
		if (xmlObj.children.length) {
			endObj.text(tabs + "</" + tagName + ">");
		} else {
			endObj.text("</" + tagName + ">");
		}
		
		output.append(endObj);
	}
	
	function WrapXML(logLine) {
		var textXML = logLine.body;
		var parsedLogBodyElements =[];
		if (textXML[0] == "<") {
			try {
				var xmlObj = $(textXML);
				xmlObj.each(function() {
					var output = $("<div class='rootXML' />");
					var attributes = {};
					var scipDetected = false;
					$.each(this.attributes, function() {
						if (this.name == "to") {
							attributes.to = this.value;
						} else if (this.name == "from") {
							attributes.from = this.value;
						} else if (this.name == "id") {
							attributes.id = this.value;
						} else if (this.name == "dsturi") {
							attributes.to = this.value;
							scipDetected = true;
						} else if (this.name == "srcuri") {
							attributes.from = this.value;
							scipDetected = true;
						}
					});
					if (scipDetected) {
						$.each(this.children[0].attributes, function() {
							if (this.name == "transactionid") {
								attributes.id = this.value;
							}
						});
					}
					RecurseHTMLFromXMLObj(this, output, -1);
					parsedLogBodyElements.push({body: output, attributes: attributes});
				});
			} catch (err) {
				var output = $("<div class='rootString' />");
				output.text(textXML);
				parsedLogBodyElements.push({body: output, attributes: {}});
			}
		} else {
			var output = $("<div class='rootString' />");
			var attributes = {};
			var statsHtml = vidyoStats.ProcessLogLine(logLine);
			/* Check if stats */
			if (statsHtml) {
				attributes.stats = "json";
				output.append(statsHtml);
			} else {
				output.text(textXML);
			}
			parsedLogBodyElements.push({body: output, attributes: attributes});
		}
		return parsedLogBodyElements;
	}
	
	function SearchFilter(logLine, searchString) {
		return (logLine.body.search(searchString) < 0 &&
			logLine.level.search(searchString) < 0 &&
			logLine.category.search(searchString) < 0 &&
			logLine.threadName.search(searchString) < 0 &&
			logLine.functionLine.search(searchString) < 0 &&
			logLine.functionName.search(searchString) < 0);
	}
	
	this.SetNewContainer(containerId);
	
	var vidyoLog_ = this;
	setInterval(function () {
		if (stopPoll == true)
			return;
		/* get the log if available on servers */
		for (var logServerId in logServers) {
			var server = logServers[logServerId];
			if (server.outstandingRequest == false && server.url != null) {
				/* prevent concurrent requests */
				server.outstandingRequest = true;
				$.getJSON(server.url + '/logRecords?index=' + server.index + ';filter=' + logRecordsFilter + ";callback=?", (function() {
					/* user a closure to store data */
					var requestedServer = server;
					return function(data) {
					   vidyoLog_.AddLogRecords(data.records, requestedServer);
					   requestedServer.index = data.nextIndex;
					};
				})())
				.always((function() {
					/* user a closure to store data */
					var requestedServer = server;
					return function(data) {
						/* continue requests */
						requestedServer.outstandingRequest = false;
					};
				})());
			}
		}
	}, 1000);
};

function VidyoStats(containerId) {
	var rawStats = {};
	var timelineChart;
	
	function ShowJson(date) {
		if (!date)
			return;
		var dateInSecs = date.split(/[.]+/)[0];
		var stats = rawStats[dateInSecs];
		if (!stats)
			return;
		$("#json").JSONView(stats, { collapsed: true });
	}
	
	function ShowOverview(date) {
		if (!date)
			return;
		var dateInSecs = date.split(/[.]+/)[0];
		var stats = rawStats[dateInSecs];
		if (!stats)
			return;
		/* navigate to logline */
		location.href = "#" + stats.logLineId;
	}
	
	function addTimeLineChart(canvas) {
		var timeFormat = 'MM/DD/YYYY HH:mm';
		var myChart = new Chart(canvas, {
			type: 'bar',
			data: {
				labelsData: [],
				labels: [],
				datasets: [{
					label: 'Quality %',
					data: [],
					backgroundColor: [],
					borderWidth: 0
				}]
			},
			options: {
				scales: {
					xAxes: [{
						type: "time",
						barPercentage: 0.8,
						time: {
							format: timeFormat,
							// round: 'day'
							tooltipFormat: 'll HH:mm:ss'
						},
						scaleLabel: {
							display: false,
							labelString: 'Date'
						},
						gridLines: {
							display: false,
						}
					}, ],
						yAxes: [{
							display: false,
							gridLines: {
								display: false,
							},
							ticks: {
								min: 0,
								max: 100,
								stepSize: 10,
								display: false
							}
						}]
				},
				legend: {
					display: false,
				},
				responsive: true,
				maintainAspectRatio: false,
				onClick:function(c,i){
					e = i[0];
					if (e) {
						var x_value = this.data.labelsData[e._index];
						ShowOverview(x_value);
					}
				 },
			}
		});
		return myChart;
	}
	function addPieChart(canvas, label, data) {
		var timeFormat = 'MM/DD/YYYY HH:mm';
		var myChart = new Chart(canvas, {
			type: 'pie',
			data: {
				labels: [],
				datasets: [{
					data: [data, 100-data],
					backgroundColor: ['rgba(255, 99, 132, 0.2)','rgba(75, 192, 192, 0.2)']
				}]
			},
			options: {
				responsive: true
			}
		});
		return myChart;
	}
		/* 
	
			labels: ["Red", "Blue", "Yellow", "Green", "Purple", "Orange"],
			datasets: [{
				label: '# of Votes',
				data: [12, 19, 3, 5, 2, 3],
				backgroundColor: [
					'rgba(255, 99, 132, 0.2)',
					'rgba(54, 162, 235, 0.2)',
					'rgba(255, 206, 86, 0.2)',
					'rgba(75, 192, 192, 0.2)',
					'rgba(153, 102, 255, 0.2)',
					'rgba(255, 159, 64, 0.2)'
				],
				borderColor: [
					'rgba(255,99,132,1)',
					'rgba(54, 162, 235, 1)',
					'rgba(255, 206, 86, 1)',
					'rgba(75, 192, 192, 1)',
					'rgba(153, 102, 255, 1)',
					'rgba(255, 159, 64, 1)'
				],
				borderWidth: 1
			}]
			*/
	function addData(chart, date, data, label) {
		chart.data.datasets.forEach((dataset) => {
			dataset.data.push({
				x: date,
				y: data
			});
			
			if (data < 25) {
				dataset.backgroundColor.push('rgb(255, 17, 3)');
			} else if (data < 50) {
				dataset.backgroundColor.push('rgb(255, 119, 2)');
			} else {
				dataset.backgroundColor.push('rgb(40, 182, 44)');
			}
		});
		chart.data.labelsData.push(label);
		chart.update();
	}
	
	/* Sticky div on top to contain the graph */
	stickyTopDiv = $('<div/>', { id: "stickyTop", class: "stickyTop", style: "margin-top:" + $("#" + containerId).offset().top + "px;"});
	
	/* Timeline chart */
	timeLineDiv = $('<div/>', { id: "timeLine", class: "timeLine"});
	timeLineGraphCanvas = $('<canvas></canvas>');
	timeLineDiv.append(timeLineGraphCanvas);
	stickyTopDiv.append(timeLineDiv);
	$("#" + containerId).append(stickyTopDiv);
	
	timelineChart = addTimeLineChart(timeLineGraphCanvas);
  
  	/* append table with all the log records */
	logTableDiv = $('<div/>', {
		id: "logTable",
		style: "top:" + ($("#timeLine").offset().top + $("#timeLine").height()) + "px"
	});
	logTable = $('<table/>', {
		class: "logTable table table-sm table-striped"
	});
	logTableBody = $('<tbody/>', {
		id: "logTableBody"
	});
	logTable.append(logTableBody);
	logTableDiv.append(logTable);
	$("#" + containerId).append(logTableDiv);
		
	this.ProcessLogLine = function(logLine){
		/* check for json object open bracket */
		if (logLine.body[0] != "{")
			return null;
			
		if (logLine.functionName == "VidyoRoomStatisticsAsyncRun" || logLine.functionName == "VidyoEndpointStatisticsRun") {
			/* Client Log */
			var stats = $.parseJSON(logLine.body);
			stats.logLineId = logLine.id;
			return this.ProcessStatsObject(stats);
		} else if (logLine.functionName == "UsageTrackerLog") {
			/* Server Log */
			var stats = $.parseJSON(logLine.body);
			stats.stats.logLineId = logLine.id;
			return this.ProcessStatsObject(stats.stats);
		} else if (logLine.functionName == "SplunkLog") {
			/* Splunk Log */
			var stats = $.parseJSON(logLine.body);
			stats.stats.logLineId = logLine.id;
			return this.ProcessStatsObject(stats.stats);
		}
		return null;
	}
	
	this.ProcessStatsObject = function(stats){
		var vitals = {};
		var sendStreams = {};
		var receiveStreams = {};
		var audioDebug = {};
		if (!stats)
			return null;
		ParseStats(stats, vitals, sendStreams, receiveStreams, audioDebug);
		Render(vitals, sendStreams, receiveStreams, audioDebug);
		
		var dateInSecs = vitals.timeStamp.split(/[.]+/)[0];
		rawStats[dateInSecs] = stats;
		return RenderOverview(stats, vitals);
	}
	
	function ParseStats(stats, vitals, sendStreams, receiveStreams, audioDebug)  {
		/* keep the bandwidth totals cumulative */
		var sendStreamBitRateTotal = 0;
		var sendStreamPixelRateTotal = 0;
		var receiveStreamBitRateTotal = 0;
		var receiveStreamPixelRateTotal = 0;
		
		vitals.timeStamp = stats.timeStamp;
		vitals.timeStampDateFormat = new Date(stats.timeStamp.replace(" ", "T") + "Z");
		vitals.connectTimeDateFormat =  stats.connectTime? new Date(stats.connectTime.replace(" ", "T") + "Z") : new Date(0);
		if (vitals.connectTimeDateFormat.getTime() > 0) {
			vitals.connectTime = "Connected for " + Math.round((vitals.timeStampDateFormat.getTime() - vitals.connectTimeDateFormat.getTime()) / 1000) + " secs";
		} else {
			vitals.connectTime = "Not Connected"
		}
		function LocalStreamVideoParse(deviceStat, sendStreamStat, index) {
			var key = "V:" + deviceStat.name + "_" + index;
			sendStreamBitRateTotal   += sendStreamStat["sendNetworkBitRate"];
			sendStreamPixelRateTotal += (sendStreamStat["width"] * sendStreamStat["height"] * sendStreamStat["fpsSent"]);
			sendStreams[key] = {};
			sendStreams[key].bitRate	 = sendStreamBitRateTotal;
			sendStreams[key].bitRateText = sendStreamStat["sendNetworkBitRate"]/1024 + "Kb " +  sendStreamStat["codecName"] + " rtt:" + sendStreamStat["sendNetworkRtt"]/1000000 + "ms" + " N:" + sendStreamStat["codecNacks"] + " I:" + sendStreamStat["codecIFrames"] + " F:" + sendStreamStat["codecFir"];
			sendStreams[key].pixelRate	 = sendStreamPixelRateTotal;
			sendStreams[key].pixelRateText = deviceStat["width"] + "x" + deviceStat["height"] + "@S:" + 1/deviceStat["frameIntervalSet"] + "/M:" + 1/deviceStat["frameIntervalMeasured"] + sendStreamStat["width"] + "x" + sendStreamStat["height"] + "@E:" + sendStreamStat["fps"] + "/I:" + sendStreamStat["fpsInput"] + "/S:" + sendStreamStat["fpsSent"];
		}
		function LocalStreamAudioParse(deviceStat, sendStreamStat, index) {
			var key = "A:" + deviceStat.name + "_" + index;
			sendStreamBitRateTotal += sendStreamStat["sendNetworkBitRate"];
			sendStreams[key] = {};
			sendStreams[key].bitRate	 = sendStreamBitRateTotal;
			sendStreams[key].bitRateText = sendStreamStat["sendNetworkBitRate"] + " " + sendStreamStat["codecName"] + ":" + sendStreamStat["sampleRate"] + ":" + sendStreamStat["numberOfChannels"] + " rtt:" + sendStreamStat["sendNetworkRtt"]/1000000 + "ms" + " dtx:" + sendStreamStat["codecDtx"] + " Q:" + sendStreamStat["codecQualitySetting"];
		}
				
		function RemoteStreamVideoParse(remoteDeviceStat, participantStat) {
			var key = participantStat.name + " / " + remoteDeviceStat.name + "(" + participantStat.id + " / " + remoteDeviceStat.id + ")";
			receiveStreamBitRateTotal   += remoteDeviceStat["receiveNetworkBitRate"];
			receiveStreamPixelRateTotal += (remoteDeviceStat["width"] * remoteDeviceStat["height"] * remoteDeviceStat["fpsDecoderInput"]);
			receiveStreams[key] = {};
			receiveStreams[key].bitRate	 = receiveStreamBitRateTotal;
			receiveStreams[key].bitRateText		= remoteDeviceStat["receiveNetworkBitRate"] + " " + remoteDeviceStat["codecName"] + " C:" + remoteDeviceStat["receiveNetworkPacketsConcealed"] + " L:" + remoteDeviceStat["receiveNetworkPacketsLost"] + " O:" + remoteDeviceStat["receiveNetworkPacketsReordered"] + " R:" + remoteDeviceStat["receiveNetworkRecoveredWithFec"] + " N:" + remoteDeviceStat["codecNacks"] + " I:" + remoteDeviceStat["codecIFrames"] + " F:" + remoteDeviceStat["codecFir"];
			receiveStreams[key].pixelRate	 = receiveStreamPixelRateTotal;
			receiveStreams[key].pixelRateText = remoteDeviceStat["width"] + "x" + remoteDeviceStat["height"] + "@" + remoteDeviceStat["fpsDecoderInput"] + "/" + remoteDeviceStat["fpsDecoded"] + "/" + remoteDeviceStat["fpsRendered"] + " show:" + remoteDeviceStat["showWidth"] + "x" + remoteDeviceStat["showHeight"] + "@" + remoteDeviceStat["showFrameRate"] + " pixelRate:" + remoteDeviceStat["showPixelRate"];
		}
				
		function RemoteStreamAudioParse(remoteDeviceStat, participantStat) {
			var key = participantStat.name + " / " + remoteDeviceStat.name + "(" + participantStat.id + " / " + remoteDeviceStat.id + ")";
			receiveStreamBitRateTotal += remoteDeviceStat["receiveNetworkBitRate"];
			receiveStreams[key] = {};
			receiveStreams[key].bitRate	= receiveStreamBitRateTotal;
			receiveStreams[key].bitRateText = remoteDeviceStat["receiveNetworkBitRate"] + " " + remoteDeviceStat["codecName"] + ":" + remoteDeviceStat["sampleRate"] + ":" + remoteDeviceStat["numberOfChannels"] + " rtt:" + remoteDeviceStat["sendNetworkRtt"]/1000000 + "ms" + " dtx:" + remoteDeviceStat["codecDtx"] + " Q:" + remoteDeviceStat["codecQualitySetting"];
			
			/* Audio debug for speaker streams */
			for (var k in remoteDeviceStat.localSpeakerStreams) {
				var localSpeakerStreamStat = remoteDeviceStat.localSpeakerStreams[k];
				var key = participantStat.name + " / " + remoteDeviceStat.name + "(" + localSpeakerStreamStat.name + ")";
				audioDebug[key] = {};
				audioDebug[key].delay	 = localSpeakerStreamStat["delay"]/1000000; //ms
				audioDebug[key].text = "L:" + localSpeakerStreamStat["lowestThreshold"]/1000000 + "ms H:" + localSpeakerStreamStat["highestThreshold"]/1000000 + "ms E:" + localSpeakerStreamStat["lastEnergy"] + " O:" + localSpeakerStreamStat["overrun"]/1000000 + " U:" + localSpeakerStreamStat["underrun"]/1000000 + " P:" + localSpeakerStreamStat["played"]/1000000;
			}
		}
		
		/* Sending streams */
		/* cameras */
		for (var i in stats.localCameraStats) {
			var deviceStat = stats.localCameraStats[i];
			/* iterate through all the remote streams */
			for (var j in deviceStat.remoteRendererStreams) {
				var sendStreamStat = deviceStat.remoteRendererStreams[j];
				/* find stream if already exists */
				LocalStreamVideoParse(deviceStat, sendStreamStat, j);
			}
		}
		/* window shares */
		for (var i in stats.localWindowShareStats) {
			var deviceStat = stats.localWindowShareStats[i];
			/* iterate through all the remote streams */
			for (var j in deviceStat.remoteRendererStreams) {
				var sendStreamStat = deviceStat.remoteRendererStreams[j];
				/* find stream if already exists */
				LocalStreamVideoParse(deviceStat, sendStreamStat, j);
			}
		}
		/* monitors */
		for (var i in stats.localMonitorStats) {
			var deviceStat = stats.localMonitorStats[i];
			/* iterate through all the remote streams */
			for (var j in deviceStat.remoteRendererStreams) {
				var sendStreamStat = deviceStat.remoteRendererStreams[j];
				/* find stream if already exists */
				LocalStreamVideoParse(deviceStat, sendStreamStat, j);
			}
		}
		/* microphones */
		for (var i in stats.localMicrophoneStats) {
			var deviceStat = stats.localMicrophoneStats[i];
			/* iterate through all the remote streams */
			for (var j in deviceStat.remoteSpeakerStreams) {
				var sendStreamStat = deviceStat.remoteSpeakerStreams[j];
				/* find stream if already exists */
				LocalStreamAudioParse(deviceStat, sendStreamStat, j);
			}
		}
		
		/* Receiving streams */
		for (var iu in stats.userStats) {
			var userStat = stats.userStats[iu];
			for (var ir in userStat.roomStats) {
				var roomStat = userStat.roomStats[ir];
				/* split resource Id */
				var resourceId = roomStat.id.substring(0, roomStat.id.lastIndexOf("_"));
				/* split applicaiton Id */
				var applicationId = roomStat.id.substring(roomStat.id.lastIndexOf("_") + 1, roomStat.id.lastIndexOf("@"));
				/* assign fitals from any room */
				vitals.cpuUsage						= roomStat.cpuUsage;
				vitals.userId						  = userStat.id.substring(0, userStat.id.lastIndexOf("_"));
				vitals.resourceId					  = resourceId;
				vitals.applicationId				   = applicationId;
				vitals.reflectorId					 = roomStat.reflectorId;
				vitals.transportInformation			= roomStat.transportInformation;
				vitals.latencyInformation			  = userStat.latencyTestStats;
				vitals.maxEncodePixelRate			  = roomStat.maxEncodePixelRate;
				vitals.maxDecodePixelRate			  = roomStat.maxDecodePixelRate;
				vitals.currentCpuEncodePixelRate	   = roomStat.currentCpuEncodePixelRate;
				vitals.currentBandwidthEncodePixelRate = roomStat.currentBandwidthEncodePixelRate;
				vitals.currentCpuDecodePixelRate	   = roomStat.currentCpuDecodePixelRate;
				vitals.currentBandwidthDecodePixelRate = roomStat.currentBandwidthDecodePixelRate;
				vitals.sendBitRateAvailable			= roomStat.sendBitRateAvailable;
				vitals.sendBitRateTotal				= roomStat.sendBitRateTotal;
				vitals.receiveBitRateAvailable		 = roomStat.receiveBitRateAvailable;
				vitals.receiveBitRateTotal			 = roomStat.receiveBitRateTotal;
				
				for (var i in roomStat.participantStats) {
					var participantStat = roomStat.participantStats[i];
					/* iterate through all the remote cameras */
					for (var j in participantStat.remoteCameraStats) {
						var remoteDeviceStat = participantStat.remoteCameraStats[j];
						RemoteStreamVideoParse(remoteDeviceStat, participantStat);
					}
					/* iterate through all the remote window shares */
					for (var j in participantStat.remoteWindowShareStats) {
						var remoteDeviceStat = participantStat.remoteWindowShareStats[j];
						RemoteStreamVideoParse(remoteDeviceStat, participantStat);
					}
					/* iterate through all the remote microphone */
					for (var j in participantStat.remoteMicrophoneStats) {
						var remoteDeviceStat = participantStat.remoteMicrophoneStats[j];
						RemoteStreamAudioParse(remoteDeviceStat, participantStat);

					}
				}
			}
		}
		return stats;
	}
	function RenderOverview(stats, vitals)  {
		/* keep the bandwidth totals cumulative */
		var sendStreamBitRateTotal = 0;
		var sendStreamPixelRateTotal = 0;
		var receiveStreamBitRateTotal = 0;
		var receiveStreamPixelRateTotal = 0;
		var txVideoBitRate = 0;
		var txAudioBitRate = 0;
		var txContentBitRate = 0;
		var nsecPerSec = 1000000000;
		var output = $("<div class='statsData' />");
		
		function numberWithCommas(x) {
			if (x) {
				return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
		   	} else {
		   		return "NaN";
		   	}
		}
		function convertToMs(x) {
			if (x == 9223372036854776000) {
				return "N/A";
			} else {
				return Math.round(x/1000000);
			}
		}
		function LocalStreamVideoParse(deviceStat, sendStreamStat, txVideoSourcesTableObj) {
			sendStreamBitRateTotal   += sendStreamStat["sendNetworkBitRate"];
			sendStreamPixelRateTotal += (sendStreamStat["width"] * sendStreamStat["height"] * sendStreamStat["fpsSent"]);
			var rttWarning = convertToMs(sendStreamStat["sendNetworkRtt"]) > 150 ? "text-warning" : "";
			
			var txVideoSourcesTable = '';
			txVideoSourcesTable +=		'<tr>';
			txVideoSourcesTable +=			'<td title="Name">' + deviceStat.name + '</td>';
			txVideoSourcesTable +=			'<td title="Codec">' + sendStreamStat["codecName"] + '</td>';
			txVideoSourcesTable +=			'<td title="Captured - Constrained">' + deviceStat.width + '/' + deviceStat.height + '-' + sendStreamStat["width"] + '/' + sendStreamStat["height"] + '</td>';
			txVideoSourcesTable +=			'<td title="Set/Measured/Constrained/EncoderInput/Sent">' + Math.round(nsecPerSec/deviceStat.frameIntervalSet) + '/' + Math.round(nsecPerSec/deviceStat.frameIntervalMeasured) + '/' + sendStreamStat["fps"] + '/' + sendStreamStat["fpsInput"] + '/' + sendStreamStat["fpsSent"] + '</td>';
			txVideoSourcesTable +=			'<td title="Frames Dropped">' + sendStreamStat["framesDropped"] + '</td>';
			txVideoSourcesTable +=			'<td title="FIR/NACK/IFrame">' + sendStreamStat["codecFir"] + '/' + sendStreamStat["codecNacks"] + '/' + sendStreamStat["codecIFrames"] + '</td>';
			txVideoSourcesTable +=			'<td title="RTT" class="' + rttWarning + '">' + convertToMs(sendStreamStat["sendNetworkRtt"]) + 'ms</td>';
			txVideoSourcesTable +=			'<td title="Bitrate">' + numberWithCommas(sendStreamStat["sendNetworkBitRate"]) + '</td>';
			txVideoSourcesTable +=		'</tr>';
			
			txVideoSourcesTableObj.append(txVideoSourcesTable);
		}
		function LocalStreamAudioParse(deviceStat, sendStreamStat, txAudioSourcesTableObj) {
			sendStreamBitRateTotal += sendStreamStat["sendNetworkBitRate"];
			var rttWarning = convertToMs(sendStreamStat["sendNetworkRtt"]) > 150 ? "text-warning" : "";
			
			var txAudioSourcesTable = '';
			txAudioSourcesTable +=		'<tr>';
			txAudioSourcesTable +=			'<td title="Name">' + deviceStat.name + '</td>';
			txAudioSourcesTable +=			'<td title="Codec">' + sendStreamStat["codecName"] + '</td>';
			txAudioSourcesTable +=			'<td title="Codec DTX">' + sendStreamStat["codecDtx"] + '</td>';
			txAudioSourcesTable +=			'<td title="Codec Quality">' + sendStreamStat["codecQualitySetting"] + '</td>';
			txAudioSourcesTable +=			'<td title="SampleRate">' + sendStreamStat["sampleRate"] + '</td>';
			txAudioSourcesTable +=			'<td title="Channels">' + sendStreamStat["numberOfChannels"] + '</td>';
			txAudioSourcesTable +=			'<td title="Bits Per Sample">' + sendStreamStat["bitsPerSample"] + '</td>';
			txAudioSourcesTable +=			'<td title="RoundTrip" class="' + rttWarning + '">' + convertToMs(sendStreamStat["sendNetworkRtt"]) + 'ms</td>';
			txAudioSourcesTable +=			'<td title="Bitrate">' + numberWithCommas(sendStreamStat["sendNetworkBitRate"]) + '</td>';
			txAudioSourcesTable +=		'</tr>';	
			
			txAudioSourcesTableObj.append(txAudioSourcesTable);	
		}
				
		function RemoteStreamVideoParse(remoteDeviceStat, participantStat, rxVideoSinkTableObj) {
			receiveStreamBitRateTotal   += remoteDeviceStat["receiveNetworkBitRate"];
			receiveStreamPixelRateTotal += (remoteDeviceStat["width"] * remoteDeviceStat["height"] * remoteDeviceStat["fpsDecoderInput"]);

			var rxVideoSinkTable = '';
			rxVideoSinkTable +=		'<tr>';
			rxVideoSinkTable +=			'<td title="Name">' + remoteDeviceStat.name + '</td>';
			rxVideoSinkTable +=			'<td title="Participant">' + participantStat.name + '</td>';
			rxVideoSinkTable +=			'<td title="UserId">' + participantStat.userId.substring(0, participantStat.userId.lastIndexOf("_")) + '</td>';
			rxVideoSinkTable +=			'<td title="Codec">' + remoteDeviceStat["codecName"] + '</td>';
			rxVideoSinkTable +=			'<td title="Show - Received">' + remoteDeviceStat["showWidth"] + '/' + remoteDeviceStat["showHeight"] + '-' + remoteDeviceStat["width"] + '/' + remoteDeviceStat["height"] + '</td>';
			rxVideoSinkTable +=			'<td title="Show/Received/Decoded/Displayed">' + remoteDeviceStat["showFrameRate"] + '/' + remoteDeviceStat["fpsDecoderInput"] + '/' + remoteDeviceStat["fpsDecoded"] + '/' + remoteDeviceStat["fpsRendered"] + '</td>';
			rxVideoSinkTable +=			'<td title="Show State">' + remoteDeviceStat["showState"] + '</td>';
			rxVideoSinkTable +=			'<td title="FIR/NACK/IFRAME">' + remoteDeviceStat["codecFir"] + '/' + remoteDeviceStat["codecNacks"] + '/' + remoteDeviceStat["codecIFrames"] + '</td>';
			rxVideoSinkTable +=			'<td title="Lost/Concealed/Reordered">' + remoteDeviceStat["receiveNetworkPacketsLost"] + '/' + remoteDeviceStat["receiveNetworkPacketsConcealed"] + '/' + remoteDeviceStat["receiveNetworkPacketsReordered"] + '</td>';
			rxVideoSinkTable +=			'<td title="Bitrate">' + numberWithCommas(remoteDeviceStat["receiveNetworkBitRate"]) + '</td>';
			rxVideoSinkTable +=		'</tr>';
			
			rxVideoSinkTableObj.append(rxVideoSinkTable);	
		}
				
		function RemoteStreamAudioParse(remoteDeviceStat, participantStat, rxAudioSinkTableObj) {
			receiveStreamBitRateTotal += remoteDeviceStat["receiveNetworkBitRate"];
			
			/* Audio debug for speaker streams */
			for (var k in remoteDeviceStat.localSpeakerStreams) {
				var localSpeakerStreamStat = remoteDeviceStat.localSpeakerStreams[k];
				var rxAudioSinkTable = '';
				rxAudioSinkTable +=		'<tr>';
				rxAudioSinkTable +=			'<td title="Name">' + remoteDeviceStat.name + '</td>';
				rxAudioSinkTable +=			'<td title="Participant">' + participantStat.name + '</td>';
				rxAudioSinkTable +=			'<td title="UserId">' + participantStat.userId.substring(0, participantStat.userId.lastIndexOf("_")) + '</td>';
				rxAudioSinkTable +=			'<td title="Codec">' + remoteDeviceStat["codecName"] + '</td>';
				rxAudioSinkTable +=			'<td title="SampleRate">' + remoteDeviceStat["sampleRateSet"] + '</td>';
				rxAudioSinkTable +=			'<td title="Channels">' + remoteDeviceStat["numberOfChannels"] + '</td>';
				rxAudioSinkTable +=			'<td title="Bits Per Sample">' + remoteDeviceStat["bitsPerSample"] + '</td>';
				rxAudioSinkTable +=			'<td title="Delay ms">' + Math.round(localSpeakerStreamStat["delay"]/1000000) + '</td>';
				rxAudioSinkTable +=			'<td title="Overrun ms">' + Math.round(localSpeakerStreamStat["overrun"]/1000000) + '</td>';
				rxAudioSinkTable +=			'<td title="Last Energy DBFS">' + localSpeakerStreamStat["lastEnergy"] + '</td>';
				rxAudioSinkTable +=			'<td title="Bitrate">' + numberWithCommas(remoteDeviceStat["receiveNetworkBitRate"]) + '</td>';
				rxAudioSinkTable +=		'</tr>';
			
				rxAudioSinkTableObj.append(rxAudioSinkTable);	
		
			}
		}
			
		function GenerationParse(participantGenerationStat, rxDynamicTableObj, generation) {
			var rxDynamicTable = '';
			rxDynamicTable +=		'<tr>';
			rxDynamicTable +=			'<td title="Generation">' + generation + '</td>';
			rxDynamicTable +=			'<td title="Name">'	   + participantGenerationStat.name + '</td>';
			rxDynamicTable +=			'<td title="Camera">'	 + participantGenerationStat.cameraName + '</td>';
			rxDynamicTable +=			'<td title="Show">'	  + participantGenerationStat.width + '/' + participantGenerationStat.height + '</td>';
			rxDynamicTable +=			'<td title="Show">'	   + Math.round(nsecPerSec/participantGenerationStat.frameInterval) + '</td>';
			rxDynamicTable +=			'<td title="PixelRate">'  + numberWithCommas(participantGenerationStat.pixelRate) + '</td>';
			rxDynamicTable +=		'</tr>';
			
			rxDynamicTableObj.append(rxDynamicTable);	
		}
		
		/* Sending streams */
		/* cameras */
		var txVideoTable = '';		
		txVideoTable += '<table class="stats table table-sm table-striped">';
		txVideoTable +=	  '<thead class="thead-dark">';
		txVideoTable +=		'<tr>';
		txVideoTable +=			'<th scope="col" title="Camera">Camera</th>';
		txVideoTable +=			'<th scope="col" title="Encoder">Encoder</th>';
		txVideoTable +=			'<th scope="col" title="Captured - Constrained">Resolution</th>';
		txVideoTable +=			'<th scope="col" title="Set/Measured/Constrained/EncoderInput/Sent/Dropped">FPS</th>';
		txVideoTable +=			'<th scope="col" title="Frames Dropped">Drop</th>';
		txVideoTable +=			'<th scope="col" title="FIR/NACK/IFrame">FIR</th>';
		txVideoTable +=			'<th scope="col" title="RTT">RTT</th>';
		txVideoTable +=			'<th scope="col" title="Bitrate">Bitrate</th>';
		txVideoTable +=		'</tr>';
		txVideoTable +=	  '</thead>';
		txVideoTable += '</table>';
		txVideoTable = $(txVideoTable);
		txVideoTableBody = $('<tbody></tbody>');
		
		var found = false;
		for (var i in stats.localCameraStats) {
			var deviceStat = stats.localCameraStats[i];
			/* iterate through all the remote streams */
			for (var j in deviceStat.remoteRendererStreams) {
				var sendStreamStat = deviceStat.remoteRendererStreams[j];
				/* find stream if already exists */
				LocalStreamVideoParse(deviceStat, sendStreamStat, txVideoTableBody);
				txVideoBitRate += sendStreamStat["sendNetworkBitRate"];
				found = true;
			}
		}
		if (!found) {
			txVideoTableBody +=	  '<tr>';
			txVideoTableBody +=		'<th title="None" colspan="9">None</th>';
			txVideoTableBody +=	  '</tr>';		
		}
		txVideoTable.append(txVideoTableBody);
		
		/* window shares */
		var txContentTable = '';		
		txContentTable += '<table class="stats table table-sm table-striped">';
		txContentTable +=	'<thead class="thead-dark">';
		txContentTable +=	  '<tr>';
		txContentTable +=		'<th title="Content">Content</th>';
		txContentTable +=		'<th title="Encoder">Encoder</th>';
		txContentTable +=		'<th title="Captured - Constrained">Resolution</th>';
		txContentTable +=		'<th title="Set/Measured/Constrained/EncoderInput/Sent/Dropped">FPS</th>';
		txContentTable +=		'<th title="Frames Dropped">Drop</th>';
		txContentTable +=		'<th title="FIR/NACK/IFrame">FIR</th>';
		txContentTable +=		'<th title="RTT">RTT</th>';
		txContentTable +=		'<th title="Bitrate">Bitrate</th>';
		txContentTable +=	  '</tr>';
		txContentTable +=   '</thead>';
		txContentTable += '</table>';
		txContentTable = $(txContentTable);
		txContentTableBody = $('<tbody></tbody>');
		var found = false;
		for (var i in stats.localWindowShareStats) {
			var deviceStat = stats.localWindowShareStats[i];
			/* iterate through all the remote streams */
			for (var j in deviceStat.remoteRendererStreams) {
				var sendStreamStat = deviceStat.remoteRendererStreams[j];
				/* find stream if already exists */
				LocalStreamVideoParse(deviceStat, sendStreamStat, txContentTableBody);
				txContentBitRate += sendStreamStat["sendNetworkBitRate"];
				found = true;
			}
		}
		/* monitors */
		for (var i in stats.localMonitorStats) {
			var deviceStat = stats.localMonitorStats[i];
			/* iterate through all the remote streams */
			for (var j in deviceStat.remoteRendererStreams) {
				var sendStreamStat = deviceStat.remoteRendererStreams[j];
				/* find stream if already exists */
				LocalStreamVideoParse(deviceStat, sendStreamStat, txContentTableBody);
				txContentBitRate += sendStreamStat["sendNetworkBitRate"];
				found = true;
			}
		}
		if (!found) {
			txContentTableBody +=	  '<tr>';
			txContentTableBody +=		'<th title="None" colspan="9">None</th>';
			txContentTableBody +=	  '</tr>';		
		}
		txContentTable.append(txContentTableBody);
		
		/* microphones */
		var txAudioTable = '';		
		txAudioTable += '<table class="stats table table-sm table-striped">';
		txAudioTable +=   '<thead class="thead-dark">';
		txAudioTable +=		'<tr>';
		txAudioTable +=			'<th title="Microphone">Microphone</th>';
		txAudioTable +=			'<th title="Codec">Encoder</th>';
		txAudioTable +=			'<th title="Codec DTX">DTX</th>';
		txAudioTable +=			'<th title="Codec Quality">Qual</th>';
		txAudioTable +=			'<th title="SampleRate">Rate</th>';
		txAudioTable +=			'<th title="Channels">Ch</th>';
		txAudioTable +=			'<th title="Bits Per Sample">BPS</th>';
		txAudioTable +=			'<th title="RoundTrip">RTT</th>';
		txAudioTable +=			'<th title="Bitrate">Bitrate</th>';
		txAudioTable +=		'</tr>';
		txAudioTable +=   '</thead>';
		txAudioTable += '</table>';
		txAudioTable = $(txAudioTable);
		txAudioTableBody = $('<tbody></tbody>');
		var found = false;
		for (var i in stats.localMicrophoneStats) {
			var deviceStat = stats.localMicrophoneStats[i];
			/* iterate through all the remote streams */
			for (var j in deviceStat.remoteSpeakerStreams) {
				var sendStreamStat = deviceStat.remoteSpeakerStreams[j];
				/* find stream if already exists */
				LocalStreamAudioParse(deviceStat, sendStreamStat, txAudioTableBody);
				txAudioBitRate += sendStreamStat["sendNetworkBitRate"];
				found = true;
			}
		}
		if (!found) {
			txAudioTableBody +=	  '<tr>';
			txAudioTableBody +=		'<th title="None" colspan="9">None</th>';
			txAudioTableBody +=	  '</tr>';		
		}
		txAudioTable.append(txAudioTableBody);
		
		var txBandwidthTable = '';	
		var rxBandwidthTable = '';	
		var pixelRateTable = '';	
		var vitalsTable = '';	
		var availableTable = '';	
		var sourcesTable = '';
		var rateShaperTable = '';
		var signalingLatencyTable = '';
		var latencyTable = '';
		var logTable = '';
				
		var rxVideoTable = '';		
		rxVideoTable += '<table class="stats table table-sm table-striped">';
		rxVideoTable +=   '<thead class="thead-dark">';
		rxVideoTable +=		'<tr>';
		rxVideoTable +=			'<th title="Camera">Camera</th>';
		rxVideoTable +=			'<th title="Participant">Participant</th>';
		rxVideoTable +=			'<th title="UserId">UserId</th>';
		rxVideoTable +=			'<th title="Codec">Decoder</th>';
		rxVideoTable +=			'<th title="Show - Received">Resolution</th>';
		rxVideoTable +=			'<th title="Show/Received/Decoded/Displayed">FPS</th>';
		rxVideoTable +=			'<th title="Show State">State</th>';
		rxVideoTable +=			'<th title="FIR/NACK/IFRAME">FIR</th>';
		rxVideoTable +=			'<th title="Lost/Concealed/Reordered">Packets</th>';
		rxVideoTable +=			'<th title="Bitrate">Bitrate</th>';
		rxVideoTable +=		'</tr>';
		rxVideoTable +=   '</thead>';
		rxVideoTable += '</table>';
		rxVideoTable = $(rxVideoTable);
		rxVideoTableBody = $('<tbody></tbody>');
		var foundRxVideo = false;
		
		var rxContentTable = '';		
		rxContentTable += '<table class="stats table table-sm table-striped">';
		rxContentTable +=   '<thead class="thead-dark">';
		rxContentTable +=		'<tr>';
		rxContentTable +=			'<th title="Content">Content</th>';
		rxContentTable +=			'<th title="Participant">Participant</th>';
		rxContentTable +=			'<th title="UserId">UserId</th>';
		rxContentTable +=			'<th title="Codec">Decoder</th>';
		rxContentTable +=			'<th title="Show - Received">Resolution</th>';
		rxContentTable +=			'<th title="Show/Received/Decoded/Displayed">FPS</th>';
		rxContentTable +=			'<th title="FIR/NACK/IFRAME">FIR</th>';
		rxContentTable +=			'<th title="Lost/Concealed/Reordered">Packets</th>';
		rxContentTable +=			'<th title="Bitrate">Bitrate</th>';
		rxContentTable +=		'</tr>';
		rxContentTable +=   '</thead>';
		rxContentTable += '</table>';
		rxContentTable = $(rxContentTable);
		rxContentTableBody = $('<tbody></tbody>');
		var foundRxContent = false;
		
		var rxAudioTable = '';		
		rxAudioTable += '<table class="stats table table-sm table-striped">';
		rxAudioTable +=   '<thead class="thead-dark">';
		rxAudioTable +=		'<tr>';
		rxAudioTable +=			'<th title="Microphone">Microphone</th>';
		rxAudioTable +=			'<th title="Participant">Participant</th>';
		rxAudioTable +=			'<th title="UserId">UserId</th>';
		rxAudioTable +=			'<th title="Codec">Decoder</th>';
		rxAudioTable +=			'<th title="SampleRate">Rate</th>';
		rxAudioTable +=			'<th title="Channels">Ch</th>';
		rxAudioTable +=			'<th title="Bits Per Sample">BPS</th>';
		rxAudioTable +=			'<th title="Delay ms">Delay</th>';
		rxAudioTable +=			'<th title="Overrun ms">Over</th>';
		rxAudioTable +=			'<th title="Last Energy DBFS">Energy</th>';
		rxAudioTable +=			'<th title="Bitrate">Bitrate</th>';
		rxAudioTable +=		'</tr>';
		rxAudioTable +=   '</thead>';
		rxAudioTable += '</table>';
		rxAudioTable = $(rxAudioTable);
		rxAudioTableBody = $('<tbody></tbody>');
		var foundRxAudio = false;
		
		var rxDynamicTable = '';		
		rxDynamicTable += '<table class="stats table table-sm table-striped">';
		rxDynamicTable +=   '<thead class="thead-dark">';
		rxDynamicTable +=		'<tr>';
		rxDynamicTable +=			'<th title="Generation">Generation</th>';
		rxDynamicTable +=			'<th title="Name">Name</th>';
		rxDynamicTable +=			'<th title="Camera">Camera</th>';
		rxDynamicTable +=			'<th title="Show">Resolution</th>';
		rxDynamicTable +=			'<th title="Show">FPS</th>';
		rxDynamicTable +=			'<th title="Pixelrate">Pixelrate</th>';
		rxDynamicTable +=		'</tr>';
		rxDynamicTable +=   '</thead>';
		rxDynamicTable += '</table>';
		rxDynamicTable = $(rxDynamicTable);
		rxDynamicTableBody = $('<tbody></tbody>');
		rxDynamicTable.append(rxDynamicTableBody);
		
				
		signalingLatencyTable += '<table class="stats table table-sm table-striped">';
		signalingLatencyTable +=	  '<thead class="thead-dark">';
		signalingLatencyTable +=		'<tr>';
		signalingLatencyTable +=			'<th title="Action">Action</th>';
		signalingLatencyTable +=			'<th title="Latency">Latency</th>';
		signalingLatencyTable +=		'</tr>';
		signalingLatencyTable +=	  '</thead>';
		signalingLatencyTable +=	  '<tbody>';
		signalingLatencyTable +=		'<tr>';
		signalingLatencyTable +=			'<td title="Login">Login</td>';
		signalingLatencyTable +=			'<td title="Latency">' + stats.loginTimeConsumedMs + 'ms</td>';
		signalingLatencyTable +=		'</tr>';
		signalingLatencyTable +=		'<tr>';
		signalingLatencyTable +=			'<td title="Login">Enter Room</td>';
		signalingLatencyTable +=			'<td title="Latency">' + stats.roomEnterTimeConsumedMs + 'ms</td>';
		signalingLatencyTable +=		'</tr>';
		signalingLatencyTable +=		'<tr>';
		signalingLatencyTable +=			'<td title="Login">Aquire Router</td>';
		signalingLatencyTable +=			'<td title="Latency">' + stats.mediaRouteAcquireTimeConsumedMs + 'ms</td>';
		signalingLatencyTable +=		'</tr>';
		signalingLatencyTable +=		'<tr>';
		signalingLatencyTable +=			'<td title="Login">Enable Media</td>';
		signalingLatencyTable +=			'<td title="Latency">' + stats.mediaEnableTimeConsumedMs + 'ms</td>';
		signalingLatencyTable +=		'</tr>';
		signalingLatencyTable +=	  '</tbody>';
		signalingLatencyTable += '</table>';
		/*
		cardsTable = '<div class="row">';
		function addResourceCard(title, value) {
			var card = '';
			card += '<div class="col-sm-3">';
			card += '	<div class="card" style="width: 6rem;">';
			card += '	<div class="card-header">' + title + '</div>';
			card += ' 	 <div class="card-body">';
			card += '		<h6 class="card-title">' + value + '%</h4>';
			card += ' 	 </div>';
			card += '	</div>';
			card += '</div>';
			return card;
		}
		cardsTable += addResourceCard("CPU", vitals.cpuUsage);
		*/
				
		var bandwidthEncodePct = Math.round((Math.min(vitals["currentBandwidthEncodePixelRate"], vitals["maxEncodePixelRate"])/vitals["maxEncodePixelRate"])*100);
		var bandwidthDecodePct =  Math.round((Math.min(vitals["currentBandwidthDecodePixelRate"], vitals["maxDecodePixelRate"])/vitals["maxDecodePixelRate"])*100);
		var cpuEncodePct =  Math.round((Math.min(vitals["currentCpuEncodePixelRate"], vitals["maxEncodePixelRate"])/vitals["maxEncodePixelRate"])*100);
		var cpuDecodePct =  Math.round((Math.min(vitals["currentCpuDecodePixelRate"], vitals["maxDecodePixelRate"])/vitals["maxDecodePixelRate"])*100);
		
		/* fixme the library produces 0 for bandwith when resource manager is not activated. remove when library is fixed */
		bandwidthEncodePct = bandwidthEncodePct == 0 ? 100 : bandwidthEncodePct;
		bandwidthDecodePct = bandwidthDecodePct == 0 ? 100 : bandwidthDecodePct;
		
		/* available  table */		
		availableTable += '<table class="stats table table-sm table-striped">';
		availableTable +=   '<thead class="thead-dark">';
		availableTable +=		'<tr>';
		availableTable +=			'<th title="Current">Current</th>';
		availableTable +=			'<th>Send</th>';
		availableTable +=			'<th>Receive</th>';
		availableTable +=		'</tr>';
		availableTable +=   '</thead>';
		availableTable +=   '<tbody>';
		availableTable +=		'<tr>';
		availableTable +=			'<td title="Amount of Processing available on the endpoint">CPU</td>';
		availableTable +=			'<td title="Available Encode CPU">' + cpuEncodePct + '%</td>';
		availableTable +=			'<td title="Available Decode CPU">' + cpuDecodePct + '%</td>';
		availableTable +=		'</tr>';
		availableTable +=		'<tr>';
		availableTable +=			'<td title="Amount of Bandwidth available on the endpoint">Network</td>';
		availableTable +=			'<td title="Available Encode Bandwidth">' + bandwidthEncodePct + '%</td>';
		availableTable +=			'<td title="Available Decode Bandwidth">' + bandwidthDecodePct + '%</td>';
		availableTable +=		'</tr>';
		/* Close the table after iterating rooms */
				
		
		vitalsTable += '<table class="stats table table-sm table-striped">';
		vitalsTable +=   '<thead class="thead-dark">';
		vitalsTable +=		'<tr>';
		vitalsTable +=			'<th title="Overview" colspan="2">Overview</th>';
		vitalsTable +=		'</tr>';
		vitalsTable +=   '</thead>';
		vitalsTable +=   '<tbody>';
		vitalsTable +=		'<tr>';
		vitalsTable +=			'<td>Timestamp</td>';
		vitalsTable +=			'<td title="Timestamp">' + stats.timeStamp + ' UTC</td>';
		vitalsTable +=		'</tr>';
		vitalsTable +=		'<tr>';
		vitalsTable +=			'<td>Connected</td>';
		vitalsTable +=			'<td title="Connected">' + vitals.connectTime + '</td>';
		vitalsTable +=		'</tr>';
		vitalsTable +=		'<tr>';
		vitalsTable +=			'<td>CPU</td>';
		vitalsTable +=			'<td title="CPU">' + vitals.cpuUsage + '</td>';
		vitalsTable +=		'</tr>';
		vitalsTable +=		'<tr>';
		vitalsTable +=			'<td>Application Tag</td>';
		vitalsTable +=			'<td title="Application Tag">' + stats.applicationTag + '</td>';
		vitalsTable +=		'</tr>';
		vitalsTable +=		'<tr>';
		vitalsTable +=			'<td>Version</td>';
		vitalsTable +=			'<td title="Version">' + stats.libraryVersion + '</td>';
		vitalsTable +=		'</tr>';
		vitalsTable +=		'<tr>';
		vitalsTable +=			'<td>UserId</td>';
		vitalsTable +=			'<td title="UserId">' + vitals.userId + '</td>';
		vitalsTable +=		'</tr>';
		vitalsTable +=		'<tr>';
		vitalsTable +=			'<td>ResourceId</td>';
		vitalsTable +=			'<td title="ResourceId">' + vitals.resourceId + '</td>';
		vitalsTable +=		'</tr>';
		vitalsTable +=		'<tr>';
		vitalsTable +=			'<td>ApplicationId</td>';
		vitalsTable +=			'<td title="ApplicationId">' + vitals.applicationId + '</td>';
		vitalsTable +=		'</tr>';
		vitalsTable +=		'<tr>';
		vitalsTable +=			'<td>ReflectorId</td>';
		vitalsTable +=			'<td title="ReflectorId">' + vitals.reflectorId + '</td>';
		vitalsTable +=		'</tr>';
		/* Close the table after iterating rooms */
		
		if (stats.logStats) {
			logTable += '<table class="stats table">';
			logTable +=	  '<thead class="thead-dark">';
			logTable +=		'<tr>';
			logTable +=			'<th title="File:Line(Function)">Errors</th>';
			logTable +=			'<th title="Num">Num</th>';
			logTable +=		'</tr>';
			logTable +=	  '</thead>';
			logTable +=	  '<tbody>';
			for (var i in stats.logStats.logErrorDataStats) {
				var logStat = stats.logStats.logErrorDataStats[i];
				logTable +=		'<tr>';
				logTable +=			'<td title="File:Line(Function)">' + logStat.name + '</td>';
				logTable +=			'<td title="Num">' + logStat.occurances + '</td>';
				logTable +=		'</tr>';
			}
			logTable +=	  '<thead class="thead-dark">';
			logTable +=		'<tr>';
			logTable +=			'<th title="File:Line(Function)">Warnings</th>';
			logTable +=			'<th title="Num">Num</th>';
			logTable +=		'</tr>';
			logTable +=	  '</thead>';
			for (var i in stats.logStats.logWarningDataStats) {
				var logStat = stats.logStats.logWarningDataStats[i];
				logTable +=		'<tr>';
				logTable +=			'<td title="File:Line(Function)">' + logStat.name + '</td>';
				logTable +=			'<td title="Num">' + logStat.occurances + '</td>';
				logTable +=		'</tr>';
			}
			logTable +=	  '</tbody>';
		}
		logTable += '</table>';
		
		/* Receiving streams */
		for (var iu in stats.userStats) {
			var userStat = stats.userStats[iu];
			for (var ir in userStat.roomStats) {
				var roomStat = userStat.roomStats[ir];
				var rxVideoBitRate = 0;
				var rxAudioBitRate = 0;
				var rxContentBitRate = 0;
				var lowestLatencyValue = 10000;
				var lowestLatencyName = "N/A";
					
				for (var i in roomStat.participantStats) {
					var participantStat = roomStat.participantStats[i];
					/* iterate through all the remote cameras */
					for (var j in participantStat.remoteCameraStats) {
						var remoteDeviceStat = participantStat.remoteCameraStats[j];
						RemoteStreamVideoParse(remoteDeviceStat, participantStat, rxVideoTableBody);
						rxVideoBitRate += remoteDeviceStat["receiveNetworkBitRate"];
						foundRxVideo = true;
					}
					/* iterate through all the remote window shares */
					for (var j in participantStat.remoteWindowShareStats) {
						var remoteDeviceStat = participantStat.remoteWindowShareStats[j];
						RemoteStreamVideoParse(remoteDeviceStat, participantStat, rxContentTableBody);
						rxContentBitRate += remoteDeviceStat["receiveNetworkBitRate"];
						foundRxContent = true;
					}
					/* iterate through all the remote microphone */
					for (var j in participantStat.remoteMicrophoneStats) {
						var remoteDeviceStat = participantStat.remoteMicrophoneStats[j];
						RemoteStreamAudioParse(remoteDeviceStat, participantStat, rxAudioTableBody);
						rxAudioBitRate += remoteDeviceStat["receiveNetworkBitRate"];
						foundRxAudio = true;
					}
				}
				for (var i in roomStat.participantGenerationStats) {
					var participantGenerationStat = roomStat.participantGenerationStats[i];
					GenerationParse(participantGenerationStat, rxDynamicTableBody, i);
				}
						
				txBandwidthTable += '<table class="stats table table-sm table-striped">';
				txBandwidthTable +=	  '<thead class="thead-dark">';
				txBandwidthTable +=		'<tr>';
				txBandwidthTable +=			'<th>Send</th>';
				txBandwidthTable +=			'<th title="Available bandwidth Mb/s">Avail</th>';
				txBandwidthTable +=			'<th title="Actual bitrate Mb/s">Actual</th>';
				txBandwidthTable +=			'<th title="Total Transmit bitrate Mb/s">Total</th>';
				txBandwidthTable +=			'<th title="Retransmit bitrate Mb/s">Ret</th>';
				txBandwidthTable +=			'<th title="Target Encoder bitrate Mb/s">Target</th>';
				txBandwidthTable +=			'<th>LB Delay</th>';
				txBandwidthTable +=		'</tr>';
				txBandwidthTable +=	  '</thead>';
				txBandwidthTable +=	  '<tbody>';
				txBandwidthTable +=		'<tr>';
				txBandwidthTable +=			'<td>Video</td>';
				txBandwidthTable +=			'<td title="Available bandwidth Mb/s">' + numberWithCommas(roomStat.bandwidthVideo.availableBandwidth) + '</td>';
				txBandwidthTable +=			'<td title="Actual bitrate Mb/s">' + numberWithCommas(roomStat.bandwidthVideo.actualEncoderBitRate) + '</td>';
				txBandwidthTable +=			'<td title="Total Transmit bitrate Mb/s">' + numberWithCommas(roomStat.bandwidthVideo.totalTransmitBitRate) + '</td>';
				txBandwidthTable +=			'<td title="Retransmit bitrate Mb/s">' + numberWithCommas(roomStat.bandwidthVideo.retransmitBitRate) + '</td>';
				txBandwidthTable +=			'<td title="Target Encoder bitrate Mb/s">' + numberWithCommas(roomStat.bandwidthVideo.targetEncoderBitRate) + '</td>';
				txBandwidthTable +=			'<td title="Leaky Bucket delay msec">' + numberWithCommas(roomStat.bandwidthVideo.leakyBucketDelay) + '</td>';
				txBandwidthTable +=		'</tr>';
				txBandwidthTable +=		'<tr>';
				txBandwidthTable +=			'<td>Audio</td>';
				txBandwidthTable +=			'<td title="Available bandwidth Mb/s">' + numberWithCommas(roomStat.bandwidthAudio.availableBandwidth) + '</td>';
				txBandwidthTable +=			'<td title="Actual bitrate Mb/s">' + numberWithCommas(roomStat.bandwidthAudio.actualEncoderBitRate) + '</td>';
				txBandwidthTable +=			'<td title="Total Transmit bitrate Mb/s">' + numberWithCommas(roomStat.bandwidthAudio.totalTransmitBitRate) + '</td>';
				txBandwidthTable +=			'<td title="Retransmit bitrate Mb/s">' + numberWithCommas(roomStat.bandwidthAudio.retransmitBitRate) + '</td>';
				txBandwidthTable +=			'<td title="Target Encoder bitrate Mb/s">' + numberWithCommas(roomStat.bandwidthAudio.targetEncoderBitRate) + '</td>';
				txBandwidthTable +=			'<td title="Leaky Bucket delay msec">' + numberWithCommas(roomStat.bandwidthAudio.leakyBucketDelay) + '</td>';
				txBandwidthTable +=		'</tr>';
				txBandwidthTable +=		'<tr>';
				txBandwidthTable +=			'<td>Content</td>';
				txBandwidthTable +=			'<td title="Available bandwidth Mb/s">' + numberWithCommas(roomStat.bandwidthApp.availableBandwidth) + '</td>';
				txBandwidthTable +=			'<td title="Actual bitrate Mb/s">' + numberWithCommas(roomStat.bandwidthApp.actualEncoderBitRate) + '</td>';
				txBandwidthTable +=			'<td title="Total Transmit bitrate Mb/s">' + numberWithCommas(roomStat.bandwidthApp.totalTransmitBitRate) + '</td>';
				txBandwidthTable +=			'<td title="Retransmit bitrate Mb/s">' + numberWithCommas(roomStat.bandwidthApp.retransmitBitRate) + '</td>';
				txBandwidthTable +=			'<td title="Target Encoder bitrate Mb/s">' + numberWithCommas(roomStat.bandwidthApp.targetEncoderBitRate) + '</td>';
				txBandwidthTable +=			'<td title="Leaky Bucket delay msec">' + numberWithCommas(roomStat.bandwidthApp.leakyBucketDelay) + '</td>';
				txBandwidthTable +=		'</tr>';
				txBandwidthTable +=		'<tr>';
				txBandwidthTable +=			'<td>Total</td>';
				txBandwidthTable +=			'<td title="Available bandwidth Mb/s">' + numberWithCommas(roomStat.sendBitRateAvailable) + '</td>';
				txBandwidthTable +=			'<td title="Actual bitrate Mb/s">' + numberWithCommas(roomStat.sendBitRateTotal) + '</td>';
				txBandwidthTable +=			'<td title="Total Transmit bitrate Mb/s"></td>';
				txBandwidthTable +=			'<td title="Retransmit bitrate Mb/s"></td>';
				txBandwidthTable +=			'<td title="Target Encoder bitrate Mb/s"></td>';
				txBandwidthTable +=			'<td title="Leaky Bucket delay msec"></td>';
				txBandwidthTable +=		'</tr>';
				txBandwidthTable +=	  '</tbody>';
				txBandwidthTable += '</table>';
					
				rxBandwidthTable += '<table class="stats table table-sm table-striped">';
				rxBandwidthTable +=	  '<thead class="thead-dark">';
				rxBandwidthTable +=		'<tr>';
				rxBandwidthTable +=			'<th>Receive</th>';
				rxBandwidthTable +=			'<th title="Available bandwidth Mb/s">Avail</th>';
				rxBandwidthTable +=			'<th title="Actual bitrate Mb/s">Actual</th>';
				rxBandwidthTable +=		'</tr>';
				rxBandwidthTable +=	  '</thead>';
				rxBandwidthTable +=	  '<tbody>';
				rxBandwidthTable +=		'<tr>';
				rxBandwidthTable +=			'<td>Video</td>';
				rxBandwidthTable +=			'<td title="Available bandwidth Mb/s"></td>';
				rxBandwidthTable +=			'<td title="Actual bitrate Mb/s">' + numberWithCommas(rxVideoBitRate) + '</td>';
				rxBandwidthTable +=		'</tr>';
				rxBandwidthTable +=		'<tr>';
				rxBandwidthTable +=			'<td>Audio</td>';
				rxBandwidthTable +=			'<td title="Available bandwidth Mb/s"></td>';
				rxBandwidthTable +=			'<td title="Actual bitrate Mb/s">' + numberWithCommas(rxAudioBitRate) + '</td>';
				rxBandwidthTable +=		'</tr>';
				rxBandwidthTable +=		'<tr>';
				rxBandwidthTable +=			'<td>Content</td>';
				rxBandwidthTable +=			'<td title="Available bandwidth Mb/s"></td>';
				rxBandwidthTable +=			'<td title="Actual bitrate Mb/s">' + numberWithCommas(rxContentBitRate) + '</td>';
				rxBandwidthTable +=		'</tr>';
				rxBandwidthTable +=		'<tr>';
				rxBandwidthTable +=			'<td>Total</td>';
				rxBandwidthTable +=			'<td title="Available bandwidth Mb/s">' + numberWithCommas(roomStat.receiveBitRateAvailable) + '</td>';
				rxBandwidthTable +=			'<td title="Actual bitrate Mb/s">' + numberWithCommas(roomStat.receiveBitRateTotal) + '</td>';
				rxBandwidthTable +=		'</tr>';
				rxBandwidthTable +=	  '</tbody>';
				rxBandwidthTable += '</table>';
								
				rateShaperTable += '<table class="stats table table-sm table-striped">';
				rateShaperTable +=	  '<thead class="thead-dark">';
				rateShaperTable +=		'<tr>';
				rateShaperTable +=			'<th>Rate Shaper</th>';
				rateShaperTable +=			'<th title="Delay Normal">D_N</th>';
				rateShaperTable +=			'<th title="Delay Retransmit">D_R</th>';
				rateShaperTable +=			'<th title="Packets Normal">P_N</th>';
				rateShaperTable +=			'<th title="Packets Retransmit">P_R</th>';
				rateShaperTable +=			'<th title="Drop Normal">Dr_N</th>';
				rateShaperTable +=		'</tr>';
				rateShaperTable +=	  '</thead>';
				rateShaperTable +=	  '<tbody>';
				rateShaperTable +=		'<tr>';
				rateShaperTable +=			'<td>Video</td>';
				rateShaperTable +=			'<td title="Delay Normal">' + numberWithCommas(roomStat.rateShaperVideo.delayNormal) + '</td>';
				rateShaperTable +=			'<td title="Delay Retransmit">' + numberWithCommas(roomStat.rateShaperVideo.delayRetransmit) + '</td>';
				rateShaperTable +=			'<td title="Packets Normal">' + numberWithCommas(roomStat.rateShaperVideo.packetsNormal) + '</td>';
				rateShaperTable +=			'<td title="Packets Retransmit">' + numberWithCommas(roomStat.rateShaperVideo.packetsRetransmit) + '</td>';
				rateShaperTable +=			'<td title="Drop Normal">' + numberWithCommas(roomStat.rateShaperVideo.dropNormal) + '</td>';
				rateShaperTable +=		'</tr>';
				rateShaperTable +=		'<tr>';
				rateShaperTable +=			'<td>Audio</td>';
				rateShaperTable +=			'<td title="Delay Normal">' + numberWithCommas(roomStat.rateShaperAudio.delayNormal) + '</td>';
				rateShaperTable +=			'<td title="Delay Retransmit">' + numberWithCommas(roomStat.rateShaperAudio.delayRetransmit) + '</td>';
				rateShaperTable +=			'<td title="Packets Normal">' + numberWithCommas(roomStat.rateShaperAudio.packetsNormal) + '</td>';
				rateShaperTable +=			'<td title="Packets Retransmit">' + numberWithCommas(roomStat.rateShaperAudio.packetsRetransmit) + '</td>';
				rateShaperTable +=			'<td title="Drop Normal">' + numberWithCommas(roomStat.rateShaperAudio.dropNormal) + '</td>';
				rateShaperTable +=		'</tr>';
				rateShaperTable +=		'<tr>';
				rateShaperTable +=			'<td>Content</td>';
				rateShaperTable +=			'<td title="Delay Normal">' + numberWithCommas(roomStat.rateShaperApp.delayNormal) + '</td>';
				rateShaperTable +=			'<td title="Delay Retransmit">' + numberWithCommas(roomStat.rateShaperApp.delayRetransmit) + '</td>';
				rateShaperTable +=			'<td title="Packets Normal">' + numberWithCommas(roomStat.rateShaperApp.packetsNormal) + '</td>';
				rateShaperTable +=			'<td title="Packets Retransmit">' + numberWithCommas(roomStat.rateShaperApp.packetsRetransmit) + '</td>';
				rateShaperTable +=			'<td title="Drop Normal">' + numberWithCommas(roomStat.rateShaperApp.dropNormal) + '</td>';
				rateShaperTable +=		'</tr>';
				rateShaperTable +=	  '</tbody>';
				rateShaperTable += '</table>';
				
				latencyTable += '<table class="stats table table-sm table-striped">';
				latencyTable +=	  '<thead class="thead-dark">';
				latencyTable +=		'<tr>';
				latencyTable +=			'<th title="Host">Host</th>';
				latencyTable +=			'<th title="Latency">Latency</th>';
				latencyTable +=		'</tr>';
				latencyTable +=	  '</thead>';
				latencyTable +=	  '<tbody>';
				/* Add Latency Info */
				var latencyTestDataStats = [];
				if (vitals.latencyInformation)
					latencyTestDataStats = vitals.latencyInformation.latencyTestDataStats;
					
				for (var i in latencyTestDataStats) {
					var latencyInformation = latencyTestDataStats[i];
					latencyTable +=		'<tr>';
					latencyTable +=			'<td title="Host">' + latencyInformation.name + '</td>';
					latencyTable +=			'<td title="Latency">' + latencyInformation.latencyMs + 'ms</td>';
					latencyTable +=		'</tr>';
					if (latencyInformation.latencyMs < lowestLatencyValue) {
						lowestLatencyValue = latencyInformation.latencyMs;
						lowestLatencyName = latencyInformation.latencyMs + "ms" + " to " + latencyInformation.name;
					}
				}
				latencyTable +=	  '</tbody>';
				latencyTable += '</table>';
				
				var latencyWarning = lowestLatencyValue > 150 ? "text-warning" : "";
				vitalsTable +=		'<tr>';
				vitalsTable +=			'<td title="Name">Lowest latency</td>';
				vitalsTable +=			'<td title="Interface" class="' + latencyWarning + '">' + lowestLatencyName + '</td>';
				vitalsTable +=		'</tr>';
					
				/* Add Transport Info */
				for (var i in vitals.transportInformation) {
					var transportInformation = vitals.transportInformation[i];
					var transportWarning = transportInformation.transportPlugIn != "UDP" ? "text-warning" : "";

					vitalsTable +=		'<tr>';
					vitalsTable +=			'<td title="Type">Transport ' + transportInformation.connectionType + '</td>';
					vitalsTable +=			'<td title="Interface" class="' + latencyWarning + '">' + transportInformation.interfaceType + '/'+ transportInformation.transportPlugIn + '</td>';
					vitalsTable +=		'</tr>';
				}
		
				availableTable +=		'<tr>';
				availableTable +=			'<td title="Bandwidth">Bandwidth</th>';
				availableTable +=			'<td title="Send">' + numberWithCommas(roomStat.sendBitRateTotal) + ' bps</td>';
				availableTable +=			'<td title="Receive">' + numberWithCommas(roomStat.receiveBitRateTotal) + ' bps</td>';
				availableTable +=		'</tr>';
		
				sourcesTable += '<table class="stats table table-sm table-striped">';
				sourcesTable +=	  '<thead class="thead-dark">';
				sourcesTable +=		'<tr>';
				sourcesTable +=			'<th>Source Shows</th>';
				sourcesTable +=			'<th>Ammount</th>';
				sourcesTable +=		'</tr>';
				sourcesTable +=	  '</thead>';
				sourcesTable +=	  '<tbody>';
				sourcesTable +=		'<tr>';
				sourcesTable +=			'<td>Max Dynamic</td>';
				sourcesTable +=			'<td title="Maximum dynamic sources allowed">' + (roomStat.maxVideoSources - roomStat.staticSources) + '</td>';
				sourcesTable +=		'</tr>';
				sourcesTable +=		'<tr>';
				sourcesTable +=			'<td>Static Sources</td>';
				sourcesTable +=			'<td title="Current static sources">' + roomStat.staticSources + '</td>';
				sourcesTable +=		'</tr>';
				sourcesTable +=		'<tr>';
				sourcesTable +=			'<td>Max Sources</td>';
				sourcesTable +=			'<td title="Maximum sources allowed">' + roomStat.maxVideoSources + '</td>';
				sourcesTable +=		'</tr>';
				sourcesTable +=	  '</tbody>';
				sourcesTable += '</table>';
			}
		}
		
		/* Close up the available table */
		availableTable +=	'</tbody>';
		availableTable +=   '<thead class="thead-dark">';
		availableTable +=		'<tr>';
		availableTable +=			'<th title="">Total to Date</th>';
		availableTable +=			'<th title=""></th>';
		availableTable +=			'<th title=""></th>';
		availableTable +=		'</tr>';
		availableTable +=   '</thead>';
		availableTable +=   '<tbody>';
		availableTable +=		'<tr>';
		availableTable +=			'<td title="TCP">TCP</th>';
		availableTable +=			'<td title="Send TCP">' + numberWithCommas(stats.bytesSentTcp) + ' bytes</td>';
		availableTable +=			'<td title="Receive TCP">' + numberWithCommas(stats.bytesReceivedTcp) + ' bytes</td>';
		availableTable +=		'</tr>';
		availableTable +=		'<tr>';
		availableTable +=			'<td title="UDP">UDP</th>';
		availableTable +=			'<td title="Send UDP">' + numberWithCommas(stats.bytesSentUdp) + ' bytes</td>';
		availableTable +=			'<td title="Receive UDP">' + numberWithCommas(stats.bytesReceivedUdp) + ' bytes</td>';
		availableTable +=		'</tr>';
		availableTable +=	'</tbody>';
		availableTable += '</table>';
		
		/* Close up the vitals table */
		vitalsTable +=	'</tbody>';
		vitalsTable += '</table>';
		/* Add Remote sources after the above loop */
		if (!foundRxVideo) {
			rxVideoTableBody +=		'<tr>';
			rxVideoTableBody +=			'<th title="None" colspan="9">None</th>';
			rxVideoTableBody +=		'</tr>';
		}
		rxVideoTable.append(rxVideoTableBody);
		
		if (!foundRxContent) {
			rxContentTableBody +=		'<tr>';
			rxContentTableBody +=			'<th title="None" colspan="8">None</th>';
			rxContentTableBody +=		'</tr>';
		}
		rxContentTable.append(rxContentTableBody);
		
		if (!foundRxAudio) {
			rxAudioTableBody +=		'<tr>';
			rxAudioTableBody +=			'<th title="None" colspan="10">None</th>';
			rxAudioTableBody +=		'</tr>';
		}
		rxAudioTable.append(rxAudioTableBody);
		
		txBandwidthTable = $(txBandwidthTable);
		rxBandwidthTable = $(rxBandwidthTable);
		rateShaperTable = $(rateShaperTable);
		logTable = $(logTable);
		availableTable = $(availableTable);
		vitalsTable = $(vitalsTable);
		signalingLatencyTable = $(signalingLatencyTable);
		latencyTable = $(latencyTable);
		sourcesTable = $(sourcesTable);
		
		
		var tableHeaderRow = $('<div/>', { class: "row" });
		tableHeaderRow.append($('<div class="col">')
		.append("<h1>" + vitals.timeStampDateFormat.toLocaleString() + " (" + vitals.connectTime + ")</h1>"));
		
		/* 2 columns for Local/Remote Sources and Log */
		var localRemoteSourcesRow = $('<div/>', { class: "row" });
		localRemoteSourcesRow.append($('<div class="col-4">')
			.append("<h2>Resources</h2>")
			.append(availableTable)
			.append("<h2>Endpoint</h2>")
			.append(vitalsTable)
			.append("<h2>Sources</h2>")
			.append(sourcesTable));
		localRemoteSourcesRow.append($('<div class="col-8">')
			.append("<h2>Sending</h2>")
			.append(txVideoTable)
			.append(txContentTable)
			.append(txAudioTable)
			.append("<h2>Receiving</h2>")
			.append(rxVideoTable)
			.append(rxContentTable)
			.append(rxAudioTable)
			.append(rxDynamicTable));
		
		/* 2 columns for advanced for and logs */
		var advancedRow = $('<div/>', { class: "row" });
		
		advancedRow.append($('<div class="col">').append("<h2>Log</h2>").append(logTable));
		advancedRow.append($('<div class="col">').append("<h2>Latency</h2>").append(signalingLatencyTable).append(latencyTable));
		advancedRow.append($('<div class="col">').append("<h2>Bandwidth</h2>").append(txBandwidthTable).append(rxBandwidthTable).append(rateShaperTable));

		var dashboardLayout = $('<div/>', { class: "container-fluid" });
		dashboardLayout.append(tableHeaderRow).append(localRemoteSourcesRow).append(advancedRow);
		output.append(dashboardLayout);
		
		return output;
	}
	
	function Render(vitals, sendStreams, receiveStreams, audioDebugStreams)  {
		var bandwidthEncodePct = (Math.min(vitals["currentBandwidthEncodePixelRate"], vitals["maxEncodePixelRate"])/vitals["maxEncodePixelRate"])*100;
		var bandwidthDecodePct = (Math.min(vitals["currentBandwidthDecodePixelRate"], vitals["maxDecodePixelRate"])/vitals["maxDecodePixelRate"])*100;
		var cpuEncodePct = (Math.min(vitals["currentCpuEncodePixelRate"], vitals["maxEncodePixelRate"])/vitals["maxEncodePixelRate"])*100;
		var cpuDecodePct = (Math.min(vitals["currentCpuDecodePixelRate"], vitals["maxDecodePixelRate"])/vitals["maxDecodePixelRate"])*100;
		/* fixme the library produces 0 for bandwith when resource manager is not activated. remove when library is fixed */
		bandwidthEncodePct = bandwidthEncodePct == 0 ? 100 : bandwidthEncodePct;
		bandwidthDecodePct = bandwidthDecodePct == 0 ? 100 : bandwidthDecodePct;
		var pctMin = Math.min(Math.min(Math.min(bandwidthEncodePct, bandwidthDecodePct), cpuEncodePct), cpuDecodePct);
		
		if (!isNaN(pctMin)) {
			addData(timelineChart, vitals.timeStampDateFormat, pctMin, vitals.timeStamp);
		}
	}
};

function VidyoParseLogDate(date) {
	date = date.substring(0, date.lastIndexOf('.'));
	date = date.replace(/ /g, 'T') + "Z";
	return new Date(date);
}
