"use strict";
function FileAnim() {
    ATSdisplay("filepopup", "hidden", "visible", "now");
    ATStogglefade("filepopup", { x: 0, y: -10 }, { x: 0, y: 0 }, 0.4, 0, 1);
}
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
function UpdateColor() {
    if (gtextarea.selectionStart != laststart) {
        if (glnbar == null)
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