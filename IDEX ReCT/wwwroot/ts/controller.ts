import { ipcRenderer } from "electron";
var activeTab = 0;

var wasChanged = false;
var Tabs;

var glnbar = document.getElementById("lnbar");
var gtextarea = document.getElementById("editArea") as HTMLInputElement;
var laststart = 0;

ipcRenderer.on("save-request", (event) => {
    ipcRenderer.send('save-event', (document.getElementById('editArea') as HTMLInputElement).value);
});

ipcRenderer.on("saveas-request", (event) => {
    ipcRenderer.send('saveas-event', (document.getElementById('editArea') as HTMLInputElement).value);
});

ipcRenderer.on("tab-switch", (event, args) => {
    let data = JSON.parse(args);
    activeTab = data["active"];
    (document.getElementById("editArea") as HTMLInputElement).value = data["code"];
    (document.getElementsByClassName("active")[0] as HTMLDivElement).className = "tabarea " + (Tabs[getTabIndex()][1] == false ? "unsaved" : "");
    (document.getElementById("tabbar") as HTMLDivElement).children[activeTab].children[1].className = "tabarea active";
    TextChange();
    wasChanged = false;

    if (glnbar == null) return;
    
    (glnbar.children[getLineNr(laststart) - 1] as HTMLParagraphElement).style.color = "#9C9C9C";
    (glnbar.children[getLineNr(gtextarea.selectionStart as number) - 1] as HTMLParagraphElement).style.color = "#FFB23F";

    laststart = gtextarea.selectionStart as number;
});

ipcRenderer.on("tab-status", ((event, args) => {
    let tabs = JSON.parse(args);
    console.table(tabs);
    activeTab = tabs["active"];
    
    Tabs = tabs["tabs"];
    
    let tabbar = document.getElementById("tabbar") as HTMLDivElement; tabbar.innerHTML = "";
    let htmltabs = "";
    
    for (let i = 0; i < tabs["tabs"].length; i++)
    {
        htmltabs += "<div class=\"tab\" onclick='SwitchTab("+ i +")'><div class=\"edge\"></div><div class=\"tabarea "+ (i == activeTab ? "active" : "") +" "+ (tabs["tabs"][i][1] == false ? "unsaved" : "") +"\"><p>"+ tabs["tabs"][i][0] as string + (tabs["tabs"][i][1] == false ? "*" : "") +"</p><button onclick='CloseTab("+ i +")'></button></div></div>";
    }
    console.log(htmltabs);
    tabbar.innerHTML = htmltabs;
}));

function CloseTab(index: number)
{
    ipcRenderer.send("tab-close", index + "|" + (document.getElementById("editArea") as HTMLInputElement).value);
}

function SwitchTab(index: number)
{
    if (index == activeTab) return;
    
    ipcRenderer.send("tab-request", index + "|" + (document.getElementById("editArea") as HTMLInputElement).value);
}

//File Menu Animation
function FileAnim()
{
    ATSdisplay("filepopup", "hidden", "visible", "now");
    ATStogglefade("filepopup", {x: 0, y: -10}, {x: 0, y: 0}, 0.4, 0, 1);
}

// Line Number Animation
function TextChange()
{
    wasChanged = true;
    
    let lnbar = document.getElementById("lnbar");
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
    }
}
setInterval(RefreshCode, 2000);

function RefreshCode()
{
    if (wasChanged)
    {
        wasChanged = false;
        ipcRenderer.send("transfer-code", (document.getElementById("editArea") as HTMLInputElement).value);
    }
}

setInterval(UpdateColor, 30);

function UpdateColor(force: boolean = false)
{
    if (gtextarea.selectionStart != laststart || force)
    {
        if (glnbar == null || glnbar.childElementCount < getLineNr(gtextarea.selectionStart as number)) return;
        
        (glnbar.children[getLineNr(laststart) - 1] as HTMLParagraphElement).style.color = "#9C9C9C";
        (glnbar.children[getLineNr(gtextarea.selectionStart as number) - 1] as HTMLParagraphElement).style.color = "#FFB23F";
        
        laststart = gtextarea.selectionStart as number;
    }
}

function getLineNr(index: number)
{
    return gtextarea.value.substr(0, index).split("\n").length;
}

function getTabIndex()
{
    let elem = (document.getElementsByClassName("active")[0] as HTMLDivElement);
    let tabs = (document.getElementById("tabbar") as HTMLDivElement);
    
    for (let i = 0; i < tabs.childElementCount; i++)
    {
        if (tabs.children[i].children[1].className.endsWith("active"))
            return i;
    }
    
    return 0;
}