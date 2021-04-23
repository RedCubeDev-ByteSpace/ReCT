using System;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ElectronNET.API;
using ElectronNET.API.Entities;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace IDEX_ReCT.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            Electron.IpcMain.On("save-event", async (code) => { await Save(code.ToString()); });
            Electron.IpcMain.On("saveas-event", async (code) => { await SaveAs(code.ToString()); });
            Electron.IpcMain.On("new-event", async (code) => { await NewTab(code.ToString()); });
            Electron.IpcMain.On("tab-request", async (code) => { await SwitchTab(code.ToString()); });
            Electron.IpcMain.On("transfer-code", async (code) => { await TransferCode(code.ToString()); });
        }

        public async Task TransferCode(string code)
        {
            StaticData.Tabs[StaticData.ActiveTab].Code = code; 
        }
        
        public async Task SwitchTab(string data)
        {
            int tabnum = 0;
            int lineIndex = 0;
            
            for (int i = 0; i < data.Length; i++)
                if (data[i] == '|')
                {
                    lineIndex = i;
                    break;
                }

            tabnum = int.Parse(data.Substring(0, lineIndex));
            var code = data.Substring(lineIndex + 1);
            
            StaticData.Tabs[StaticData.ActiveTab].Code = code;
            
            StaticData.ActiveTab = tabnum;
            Electron.IpcMain.Send(Electron.WindowManager.BrowserWindows.First(), "tab-switch", JsonSerializer.Serialize(new Dictionary<string, object>() {{"code", StaticData.CurrentTab.Code}, {"active", StaticData.ActiveTab}}));
        }
        
        public async Task NewTab(string code)
        {
            StaticData.Tabs[StaticData.ActiveTab].Code = code;
            StaticData.Tabs.Add(new Tab("//ReCT Compiler v 2.2 | IDEX v1.0\npackage sys;\nsys::Print(\"Hello World!\");"));
            StaticData.ActiveTab = StaticData.Tabs.Count - 1;
            Electron.IpcMain.Send(Electron.WindowManager.BrowserWindows.First(), "tab-status", StaticData.AssembleTabs());
            Electron.IpcMain.Send(Electron.WindowManager.BrowserWindows.First(), "tab-switch", JsonSerializer.Serialize(new Dictionary<string, object>() {{"code", StaticData.CurrentTab.Code}, {"active", StaticData.ActiveTab}}));
        }
        
        public async Task Save(string code)
        {
            StaticData.Tabs[StaticData.ActiveTab].Code = code;
            if (StaticData.Tabs[StaticData.ActiveTab].FileName == "") { await SaveAs(code); return; }
            
            System.IO.File.WriteAllText(StaticData.Tabs[StaticData.ActiveTab].FileName, StaticData.Tabs[StaticData.ActiveTab].Code);
            StaticData.Tabs[StaticData.ActiveTab].Saved = true;
            
            Electron.IpcMain.Send(Electron.WindowManager.BrowserWindows.First(), "tab-status", StaticData.AssembleTabs());
        }
        public async Task SaveAs(string code)
        {
            var options = new SaveDialogOptions
            {
                Title = "Save",
                Filters = new FileFilter[]
                {
                    new FileFilter { Name = "ReCT Source File", Extensions = new string[] {"rct" } }
                }
            };

            var result = await Electron.Dialog.ShowSaveDialogAsync(Electron.WindowManager.BrowserWindows.First(), options); ;

            if (result == "") return;

            if (!result.EndsWith(".rct")) result += ".rct";
            
            StaticData.Tabs[StaticData.ActiveTab].FileName = result;
            System.IO.File.WriteAllText(StaticData.Tabs[StaticData.ActiveTab].FileName, StaticData.Tabs[StaticData.ActiveTab].Code);
            StaticData.Tabs[StaticData.ActiveTab].Saved = true;
            
            Electron.IpcMain.Send(Electron.WindowManager.BrowserWindows.First(), "tab-status", StaticData.AssembleTabs());
        }
    }
}
