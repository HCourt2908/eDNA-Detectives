using UnityEngine;

namespace EDNA.Investigation.V2
{
    public static class InvestigationV2StatusIconLibrary
    {
        private static Sprite check;
        private static Sprite cross;
        private static Sprite question;

        public static Sprite Check => check != null ? check : check = Load("check-circle");
        public static Sprite Cross => cross != null ? cross : cross = Load("x-circle");
        public static Sprite Question => question != null ? question : question = Load("question-mark-circle");

        private static Sprite Load(string name)
        {
            return Resources.Load<Sprite>($"InvestigationV2/Icons/Heroicons/{name}");
        }
    }

    public static class InvestigationV2ScenarioIconLibrary
    {
        private static Sprite investigate;

        public static Sprite Investigate => investigate != null
            ? investigate
            : investigate = Resources.Load<Sprite>("InvestigationV2/Icons/OpenMoji/magnifying-glass");
    }
}
