using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OMIChecklist
{
    class ChecklistConfig
    {
        public const string ModName = "OMIChecklist";
        public const string Icon_Active = ModName + "/Assets/checklist_active";

        public static readonly String ROOT_PATH = KSPUtil.ApplicationRootPath;
        public static readonly String CONFIG_BASE_FOLDER = ROOT_PATH + "GameData/";
        public static readonly String CHECKLIST_FOLDER = CONFIG_BASE_FOLDER + ModName + "/checklists/";
    }
}
