"use strict";
function ATSmove(elemid, startPosition, endPosition, duration, unit) {
    if (unit === void 0) { unit = "em"; }
    var element = document.getElementById(elemid);
    element.style.transition = "none";
    element.style.transform = "translate(" + startPosition.x.toString() + unit + "," + startPosition.y.toString() + unit + ")";
    if (activators[elemid] != undefined) {
        element.style.visibility = activators[elemid].to;
        delete activators[elemid];
    }
    element.style.transition = duration.toString() + "s";
    element.style.transform = "translate(" + endPosition.x.toString() + unit + "," + endPosition.y.toString() + unit + ")";
    if (deactivators[elemid] != undefined) {
        element.style.visibility = deactivators[elemid].to;
        delete deactivators[elemid];
    }
}
function ATStoggle(elemid, startPosition, endPosition, duration, unit) {
    if (unit === void 0) { unit = "em"; }
    var element = document.getElementById(elemid);
    var isToggeling = false;
    var display = "";
    if (movetoggles.indexOf(element) > -1) {
        var sp = startPosition;
        startPosition = endPosition;
        endPosition = sp;
        delete movetoggles[movetoggles.indexOf(element)];
        isToggeling = true;
        if (activators[elemid] != undefined) {
            element.style.visibility = deactivators[elemid].to;
            delete deactivators[elemid];
        }
    }
    element.style.transition = "none";
    element.style.transform = "translate(" + startPosition.x.toString() + unit + "," + startPosition.y.toString() + unit + ")";
    if (activators[elemid] != undefined && !isToggeling)
        element.style.visibility = activators[elemid].to;
    if (deactivators[elemid] != undefined && isToggeling)
        element.style.visibility = deactivators[elemid].from;
    element.style.transition = duration.toString() + "s";
    element.style.transform = "translate(" + endPosition.x.toString() + unit + "," + endPosition.y.toString() + unit + ")";
    if (activators[elemid] != undefined && isToggeling)
        element.style.visibility = activators[elemid].from;
    if (deactivators[elemid] != undefined && !isToggeling)
        element.style.visibility = deactivators[elemid].to;
    delete activators[elemid];
    delete deactivators[elemid];
    if (!isToggeling)
        movetoggles.push(element);
}
function ATSmovefade(elemid, startPosition, endPosition, duration, startOpacity, endOpacity, unit) {
    if (unit === void 0) { unit = "em"; }
    var element = document.getElementById(elemid);
    element.style.transition = "none";
    element.style.transform = "translate(" + startPosition.x.toString() + unit + "," + startPosition.y.toString() + unit + ")";
    element.style.opacity = startOpacity.toString();
    if (activators[elemid] != undefined) {
        element.style.visibility = activators[elemid].to;
        delete activators[elemid];
    }
    element.style.transition = duration.toString() + "s";
    element.style.transform = "translate(" + endPosition.x.toString() + unit + "," + endPosition.y.toString() + unit + ")";
    element.style.opacity = endOpacity.toString();
    if (deactivators[elemid] != undefined) {
        element.style.visibility = deactivators[elemid].to;
        delete deactivators[elemid];
    }
}
function ATStogglefade(elemid, startPosition, endPosition, duration, startOpacity, endOpacity, unit) {
    if (unit === void 0) { unit = "em"; }
    var element = document.getElementById(elemid);
    var isToggeling = false;
    if (movetoggles.indexOf(element) > -1) {
        var sp = startPosition;
        startPosition = endPosition;
        endPosition = sp;
        var so = startOpacity;
        startOpacity = endOpacity;
        endOpacity = so;
        delete movetoggles[movetoggles.indexOf(element)];
        isToggeling = true;
    }
    element.style.transition = "none";
    element.style.transform = "translate(" + startPosition.x.toString() + unit + "," + startPosition.y.toString() + unit + ")";
    element.style.opacity = startOpacity.toString();
    if (activators[elemid] != undefined && !isToggeling)
        element.style.visibility = activators[elemid].to;
    if (deactivators[elemid] != undefined && isToggeling)
        element.style.visibility = deactivators[elemid].from;
    element.style.transition = duration.toString() + "s";
    element.style.transform = "translate(" + endPosition.x.toString() + unit + "," + endPosition.y.toString() + unit + ")";
    element.style.opacity = endOpacity.toString();
    if (activators[elemid] != undefined && isToggeling)
        element.style.visibility = activators[elemid].from;
    if (deactivators[elemid] != undefined && !isToggeling)
        element.style.visibility = deactivators[elemid].to;
    delete activators[elemid];
    delete deactivators[elemid];
    if (!isToggeling)
        movetoggles.push(element);
}
function ATSdisplay(elemid, from, to, now) {
    if (now == "now")
        activators[elemid] = { from: from, to: to };
    else
        deactivators[elemid] = { from: from, to: to };
}
var movetoggles = [];
var activators = {};
var deactivators = {};
//# sourceMappingURL=animaster.js.map