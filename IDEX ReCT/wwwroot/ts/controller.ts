function FileAnim()
{
    ATSdisplay("filepopup", "hidden", "visible", "now");
    ATStogglefade("filepopup", {x: 0, y: -10}, {x: 0, y: 0}, 0.4, 0, 1);
}

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
        if (glnbar == null) return;
        
        (glnbar.children[getLineNr(laststart) - 1] as HTMLParagraphElement).style.color = "#9C9C9C";
        (glnbar.children[getLineNr(gtextarea.selectionStart as number) - 1] as HTMLParagraphElement).style.color = "#FFB23F";
        
        laststart = gtextarea.selectionStart as number;
    }
}

function getLineNr(index: number)
{
    return gtextarea.value.substr(0, index).split("\n").length;
}