using EDNA.Investigation.Domain;
using UnityEngine;

namespace EDNA.Investigation
{
    public static class InvestigationEvidenceIconLibrary
    {
        private static Sprite link;
        private static Sprite map;
        private static Sprite beaker;
        private static Sprite signal;

        public static Sprite FishingLine => link != null ? link : link = Load("link");
        public static Sprite Seafloor => map != null ? map : map = Load("map");
        public static Sprite Laboratory => beaker != null ? beaker : beaker = Load("beaker");
        public static Sprite EDNASignal => signal != null ? signal : signal = Load("signal");

        public static Sprite ForObservation(InvestigationObservationDefinition observation)
        {
            if (observation == null) return InvestigationStatusIconLibrary.Question;
            if (observation.EvidenceId == "E07_FISHING_LINE") return FishingLine;
            if (observation.EvidenceId == "E08_SEAFLOOR_INTACT") return Seafloor;

            switch (observation.Source)
            {
                case ObservationSource.EDNA: return EDNASignal;
                case ObservationSource.CTDLog: return Laboratory;
                case ObservationSource.ROV: return Seafloor;
                default: return InvestigationStatusIconLibrary.Question;
            }
        }

        private static Sprite Load(string name)
        {
            return Resources.Load<Sprite>($"Investigation/Icons/Heroicons/{name}");
        }
    }
}
