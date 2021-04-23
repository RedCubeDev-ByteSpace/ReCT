"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var electron_1 = require("electron");
var activeTab = 0;
electron_1.ipcRenderer.on("tab-switch", function (event, args) {
    var data = JSON.parse(args);
    activeTab = data["active"];
    document.getElementById("editArea").value = data["code"];
    document.getElementsByClassName("active")[0].className = "tabarea";
    document.getElementById("tabbar").children[activeTab].children[1].className = "tabarea active";
    TextChange();
    UpdateColor(true);
});
electron_1.ipcRenderer.on("tab-status", (function (event, args) {
    var tabs = JSON.parse(args);
    console.table(tabs);
    activeTab = tabs["active"];
    var tabbar = document.getElementById("tabbar");
    tabbar.innerHTML = "";
    var htmltabs = "";
    for (var i = 0; i < tabs["tabs"].length; i++) {
        htmltabs += "<div class=\"tab\" onclick='SwitchTab(" + i + ")'><div class=\"edge\"></div><div class=\"tabarea " + (i == activeTab ? "active" : "") + "\"><p>" + tabs["tabs"][i][0] + (tabs["tabs"][i][1] == false ? "*" : "") + "</p><button></button></div></div>";
    }
    console.log(htmltabs);
    tabbar.innerHTML = htmltabs;
}));
function SwitchTab(index) {
    if (index == activeTab)
        return;
    //ipcRenderer.send("transfer-code", (document.getElementById("editArea") as HTMLInputElement).value);
    electron_1.ipcRenderer.send("tab-request", index + "|" + document.getElementById("editArea").value);
}
//File Menu Animation
function FileAnim() {
    ATSdisplay("filepopup", "hidden", "visible", "now");
    ATStogglefade("filepopup", { x: 0, y: -10 }, { x: 0, y: 0 }, 0.4, 0, 1);
}
// Line Number Animation
function TextChange() {
    var lnbar = document.getElementById("lnbar");
    var textarea = document.getElementById("editArea");
    if (textarea == null || lnbar == null)
        return;
    var text = textarea.value;
    var lines = text.split("\n");
    var count = lines.length;
    // @ts-ignore
    var cursorLine = textarea.value.substr(0, textarea.selectionStart).split("\n").length;
    if (count != lnbar.childElementCount) {
        lnbar.innerHTML = ""; // absolutely obliterating all children
        for (var i = 0; i < count; i++) {
            var elem = document.createElement("p");
            elem.textContent = i.toString();
            if (i == cursorLine - 1)
                elem.style.color = "#FFB23F";
            else
                elem.style.color = "#9C9C9C";
            lnbar.appendChild(elem);
        }
    }
}
setInterval(UpdateColor, 30);
var glnbar = document.getElementById("lnbar");
var gtextarea = document.getElementById("editArea");
var laststart = 0;
function UpdateColor(force) {
    if (force === void 0) { force = false; }
    if (gtextarea.selectionStart != laststart || force) {
        if (glnbar == null || glnbar.childElementCount < getLineNr(gtextarea.selectionStart))
            return;
        glnbar.children[getLineNr(laststart) - 1].style.color = "#9C9C9C";
        glnbar.children[getLineNr(gtextarea.selectionStart) - 1].style.color = "#FFB23F";
        laststart = gtextarea.selectionStart;
    }
}
function getLineNr(index) {
    return gtextarea.value.substr(0, index).split("\n").length;
}
//# sourceMappingURL=controller.js.map