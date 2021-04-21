using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

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

        }

        public void OnPost()
        {
            if (Request.Form.ContainsKey("code"))
                StaticData.Code = Request.Form["code"];

            if (Request.Form["mode"] == "save")
                Save();
            
            if (Request.Form["mode"] == "saveas")
                SaveAs();
        }

        void Save()
        {
            if (StaticData.FileName == "")
            {
                SaveAs();
                return;
            }

            System.IO.File.WriteAllText(StaticData.FileName, StaticData.Code);
        }

        void SaveAs()
        {
            
        }
    }
}
