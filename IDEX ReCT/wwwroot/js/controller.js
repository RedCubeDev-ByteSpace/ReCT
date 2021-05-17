"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var electron_1 = require("electron");
var activeTab = 0;
var wasChanged = false;
var Tabs;
var glnbar = document.getElementById("lnbar");
var gtextarea = document.getElementById("editArea");
var laststart = 0;
electron_1.ipcRenderer.on("save-request", function (event) {
    electron_1.ipcRenderer.send('save-event', document.getElementById('editArea').value);
});
electron_1.ipcRenderer.on("saveas-request", function (event) {
    electron_1.ipcRenderer.send('saveas-event', document.getElementById('editArea').value);
});
electron_1.ipcRenderer.on("tab-switch", function (event, args) {
    var data = JSON.parse(args);
    activeTab = data["active"];
    document.getElementById("editArea").value = data["code"];
    document.getElementsByClassName("active")[0].className = "tabarea " + (Tabs[getTabIndex()][1] == false ? "unsaved" : "");
    document.getElementById("tabbar").children[activeTab].children[1].className = "tabarea active";
    wasChanged = false;
});
electron_1.ipcRenderer.on("tab-status", (function (event, args) {
    var tabs = JSON.parse(args);
    console.table(tabs);
    activeTab = tabs["active"];
    Tabs = tabs["tabs"];
    var tabbar = document.getElementById("tabbar");
    tabbar.innerHTML = "";
    var htmltabs = "";
    for (var i = 0; i < tabs["tabs"].length; i++) {
        htmltabs += "<div class=\"tab\" onclick='SwitchTab(" + i + ")'><div class=\"edge\"></div><div class=\"tabarea " + (i == activeTab ? "active" : "") + " " + (tabs["tabs"][i][1] == false ? "unsaved" : "") + "\"><p>" + tabs["tabs"][i][0] + (tabs["tabs"][i][1] == false ? "*" : "") + "</p><button onclick='CloseTab(" + i + ")'></button></div></div>";
    }
    console.log(htmltabs);
    tabbar.innerHTML = htmltabs;
}));
function CloseTab(index) {
    electron_1.ipcRenderer.send("tab-close", index + "|" + document.getElementById("editArea").value);
}
function SwitchTab(index) {
    if (index == activeTab)
        return;
    electron_1.ipcRenderer.send("tab-request", index + "|" + document.getElementById("editArea").value);
}
//File Menu Animation
function FileAnim() {
    ATSdisplay("filepopup", "hidden", "visible", "now");
    ATStogglefade("filepopup", { x: 0, y: -10 }, { x: 0, y: 0 }, 0.4, 0, 1);
}
// Line Number Animation
function TextChange() {
    wasChanged = true;
    /*let lnbar = document.getElementById("lnbar");
    let textarea = document.getElementById("editArea") as HTMLInputElement;
    
    if (textarea == null || lnbar == null) return;

    let text = textarea.value;
    
    let lines = text.split("\n");
    let count = lines.length;

    // @ts-ignore
    let cursorLine = textarea.value.substr(0, textarea.selectionStart).split("\n").length;

    if (count != lnbar.childElementCount)
    {
        lnbar.innerHTML = ""; // absolutely obliterating all children
        
        for (let i = 0; i < count; i++)
        {
            const elem = document.createElement("p");
            elem.textContent = i.toString();
            
            if (i == cursorLine - 1)
                elem.style.color = "#FFB23F";
            else
                elem.style.color = "#9C9C9C";
            
            lnbar.appendChild(elem)
        }
    }*/
}
setInterval(RefreshCode, 2000);
function RefreshCode() {
    if (wasChanged) {
        wasChanged = false;
        electron_1.ipcRenderer.send("transfer-code", document.getElementById("editArea").value);
    }
    document.getElementsByClassName("new-button")[0].style.width = document.getElementsByClassName("CodeMirror-gutter")[0].clientWidth + "px";
}
/*setInterval(UpdateColor, 30);

function UpdateColor(force: boolean = false)
{
    if (gtextarea.selectionStart != laststart || force)
    {
        if (glnbar == null || glnbar.childElementCount < getLineNr(gtextarea.selectionStart as number)) return;
        
        (glnbar.children[getLineNr(laststart) - 1] as HTMLParagraphElement).style.color = "#9C9C9C";
        (glnbar.children[getLineNr(gtextarea.selectionStart as number) - 1] as HTMLParagraphElement).style.color = "#FFB23F";
        
        laststart = gtextarea.selectionStart as number;
    }
}*/
function getLineNr(index) {
    return gtextarea.value.substr(0, index).split("\n").length;
}
function getTabIndex() {
    var elem = document.getElementsByClassName("active")[0];
    var tabs = document.getElementById("tabbar");
    for (var i = 0; i < tabs.childElementCount; i++) {
        if (tabs.children[i].children[1].className.endsWith("active"))
            return i;
    }
    return 0;
}
//# sourceMappingURL=controller.js.map