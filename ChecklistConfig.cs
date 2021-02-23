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

        public static readonly string ROOT_PATH = KSPUtil.ApplicationRootPath;
        public static readonly string CONFIG_BASE_FOLDER = ROOT_PATH + "GameData/";
        public static readonly string CHECKLIST_FOLDER = CONFIG_BASE_FOLDER + ModName + "/checklists/";
        public static readonly string STATE_NODE_NAME = "OMICHECKLIST_STATE";
        public static readonly string CHECKED_NODE_NAME = "OMICHECKLIST_CHECKED";
        public static readonly string CATEGORY_NODE_NAME = "OMICHECKLIST_CATEGORY";
    }
}
