import { ipcRenderer } from "electron";
var activeTab = 0;

ipcRenderer.on("tab-switch", (event, args) => {
    let data = JSON.parse(args);
    activeTab = data["active"];
    (document.getElementById("editArea") as HTMLInputElement).value = data["code"];
    (document.getElementsByClassName("active")[0] as HTMLDivElement).className = "tab";
    (document.getElementById("tabbar") as HTMLDivElement).children[activeTab].children[1].className = "tab active";
});

ipcRenderer.on("tab-status", ((event, args) => {
    let tabs = JSON.parse(args);
    console.table(tabs);
    activeTab = tabs["active"];
    
    let tabbar = document.getElementById("tabbar") as HTMLDivElement; tabbar.innerHTML = "";
    let htmltabs = "";
    
    for (let i = 0; i < tabs["tabs"].length; i++)
    {
        htmltabs += "<div class=\"tab "+ (i == activeTab ? "active" : "") +"\" onclick='SwitchTab("+ i +")'><div class=\"edge\"></div><div class=\"tabarea\"><p>"+ tabs["tabs"][i][0] as string + (tabs["tabs"][i][1] == false ? "*" : "") +"</p><button></button></div></div>";
    }
    console.log(htmltabs);
    tabbar.innerHTML = htmltabs;
}));

function SwitchTab(index: number)
{
    console.log(index);
    if (index == activeTab) return;
    console.log("switching");
    
    let json = "{ \"data\":" + JSON.stringify([index, (document.getElementById("editArea") as HTMLInputElement).value]) + "}";
    console.log(json);
    ipcRenderer.send("tab-request", json);
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

setInterval(UpdateColor, 30);

var glnbar = document.getElementById("lnbar");
var gtextarea = document.getElementById("editArea") as HTMLInputElement;
var laststart = 0;

function UpdateColor()
{
    if (gtextarea.selectionStart != laststart)
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