function ATSmove(elemid: string, startPosition: {x: Number, y: Number}, endPosition: {x: Number, y: Number}, duration: Number, unit: string = "em")
{
    var element = document.getElementById(elemid) as HTMLElement;
    element.style.transition = "none";
    element.style.transform = "translate(" + startPosition.x.toString() + unit + "," + startPosition.y.toString() + unit +")";

    if (activators[elemid] != undefined)
    {
        element.style.visibility = activators[elemid].to; delete activators[elemid];
    }

    element.style.transition = duration.toString() + "s";
    element.style.transform = "translate(" + endPosition.x.toString() + unit + "," + endPosition.y.toString() + unit +")";

    if (deactivators[elemid] != undefined)
    {
        element.style.visibility = deactivators[elemid].to; delete deactivators[elemid];
    }
}

function ATStoggle(elemid: string, startPosition: {x: Number, y: Number}, endPosition: {x: Number, y: Number}, duration: Number, unit: string = "em")
{
    var element = document.getElementById(elemid) as HTMLElement;
    var isToggeling = false;
    var display = "";

    if (movetoggles.indexOf(element) > -1)
    {
        var sp = startPosition;
        startPosition = endPosition;
        endPosition = sp;
        delete movetoggles[movetoggles.indexOf(element)];
        isToggeling = true;

        if (activators[elemid] != undefined)
        {
            element.style.visibility = deactivators[elemid].to; delete deactivators[elemid];
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

function ATSmovefade(elemid: string, startPosition: {x: Number, y: Number}, endPosition: {x: Number, y: Number}, duration: Number, startOpacity: Number, endOpacity: Number, unit: string = "em")
{
    var element = document.getElementById(elemid) as HTMLElement;
    element.style.transition = "none";
    element.style.transform = "translate(" + startPosition.x.toString() + unit + "," + startPosition.y.toString() + unit +")";
    element.style.opacity = startOpacity.toString();

    if (activators[elemid] != undefined)
    {
        element.style.visibility = activators[elemid].to; delete activators[elemid];
    }

    element.style.transition = duration.toString() + "s";
    element.style.transform = "translate(" + endPosition.x.toString() + unit + "," + endPosition.y.toString() + unit +")";
    element.style.opacity = endOpacity.toString();

    if (deactivators[elemid] != undefined)
    {
        element.style.visibility = deactivators[elemid].to; delete deactivators[elemid];
    }
}

function ATStogglefade(elemid: string, startPosition: {x: Number, y: Number}, endPosition: {x: Number, y: Number}, duration: Number, startOpacity: Number, endOpacity: Number, unit: string = "em")
{
    var element = document.getElementById(elemid) as HTMLElement;
    var isToggeling = false;

    if (movetoggles.indexOf(element) > -1)
    {
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
    element.style.transform = "translate(" + startPosition.x.toString() + unit + "," + startPosition.y.toString() + unit +")";
    element.style.opacity = startOpacity.toString();

    if (activators[elemid] != undefined && !isToggeling)
        element.style.visibility = activators[elemid].to;
    if (deactivators[elemid] != undefined && isToggeling)
        element.style.visibility = deactivators[elemid].from;

    element.style.transition = duration.toString() + "s";
    element.style.transform = "translate(" + endPosition.x.toString() + unit + "," + endPosition.y.toString() + unit +")";
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

function ATSdisplay(elemid: string, from: string, to: string, now: string)
{
    if (now == "now")
        activators[elemid] = {from: from, to: to};
    else
        deactivators[elemid] = {from: from, to: to};
}

var movetoggles: Array<HTMLElement> = [];
var activators: {[id: string]: {from: string, to: string}} = {};
var deactivators: {[id: string]: {from: string, to: string}} = {};