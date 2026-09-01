using UnityEngine;

namespace EDNA.Investigation
{
    public static class InvestigationStatusIconLibrary
    {
        private static Sprite check;
        private static Sprite cross;
        private static Sprite question;

        public static Sprite Check => check != null ? check : check = Load("check-circle");
        public static Sprite Cross => cross != null ? cross : cross = Load("x-circle");
        public static Sprite Question => question != null ? question : question = Load("question-mark-circle");

        private static Sprite Load(string name)
        {
            return Resources.Load<Sprite>($"Investigation/Icons/Heroicons/{name}");
        }
    }

    public static class InvestigationScenarioIconLibrary
    {
        private static Sprite investigate;

        public static Sprite Investigate => investigate != null
            ? investigate
            : investigate = Resources.Load<Sprite>("Investigation/Icons/OpenMoji/magnifying-glass");
    }
}
