# Species facts and source audit

Reviewed 2026-09-10. These short English profiles describe real organisms; they do not validate the game’s authored food-chain predictions or infer population changes from eDNA.

The local `Species List.jam` export (2026-09-10 20:16:58 UTC) supplies the roster and scientific identities. Its unfinished fields and precise depth/temperature entries are not copied without support. In particular, Atlantic herring and Atlantic bluefin are not assigned the generic “Atlantic and Pacific” range. Northern krill are not described as obligate herbivores. Prochlorococcus is identified as the representative photosynthetic cyanobacterium, not equated with all phytoplankton.

Source of truth: `game/Assets/Data/Investigation/SpeciesFacts.json`. The builder uses this file when regenerating species; **Update Species Facts** applies only the descriptions. Names, IDs, aliases, artwork, map placement and simulation rules are preserved.

Popup facts are separated from **THIS EXAMPLE SURVEY** / **IMPORTED SURVEY RECORD**. “Map layer” describes the displayed survey layer, not the organism’s depth limits. Authored High/Medium confidence is no longer presented as a scientific assessment; supplied import confidence remains labelled as supplied. No exact population, ideal temperature or universal depth range is added.

## Evidence limits

- Flapjack-octopus text uses only general fin/webbing traits supported at genus level and the NOAA observation of O. californiana on the seabed; no genus-wide size or depth range is assigned to that species.
- The king-crab feeding sentence describes a documented observation, not a complete diet or a preferred prey rule.
- No new specific predator–prey link is asserted for the hammerhead/tuna or northern-krill/Prochlorococcus pairs.
- Figma roster spelling is retained for cross-game identifiers. Taxonomic renames require a separate coordinated catalog change.

## Profiles

### shark — *Sphyrna mokarran*

A large shark of warm coastal and offshore waters. It eats rays, other sharks and bony fishes.

[Source 1](https://www.floridamuseum.ufl.edu/discover-fish/species-profiles/great-hammerhead/).

### tuna — *Thunnus thynnus*

A highly migratory fish of the Atlantic Ocean and Mediterranean Sea. It eats herring, mackerel and other prey, and can dive far below surface waters.

[Source 1](https://www.fisheries.noaa.gov/species/western-atlantic-bluefin-tuna); [Source 2](https://media.fisheries.noaa.gov/dam-migration/guide-to-tunas-of-the-western-atlantic-ocean.pdf).

### atlantic_herring — *Clupea harengus*

A schooling fish of the North Atlantic. It eats small drifting animals, including copepods and krill, and is food for larger fish, seabirds and marine mammals.

[Source 1](https://www.fisheries.noaa.gov/species/atlantic-herring); [Source 2](https://academic.oup.com/icesjms/article/57/4/843/647348).

### krill — *Meganyctiphanes norvegica*

A small North Atlantic crustacean that eats animal plankton and some algae. It often moves toward surface waters at night and deeper water by day.

[Source 1](https://pmc.ncbi.nlm.nih.gov/articles/PMC3873013/); [Source 2](https://www.int-res.com/articles/meps/214/m214p177.pdf).

### phytoplankton — *Prochlorococcus marinus*

Here, phytoplankton is represented by Prochlorococcus marinus, a microscopic marine cyanobacterium. It uses sunlight to make organic matter and releases oxygen.

[Source 1](https://www.nature.com/articles/s42003-019-0410-x).

### green_sea_urchin — *Strongylocentrotus droebachiensis*

A spiny sea urchin found on rocky seabeds. It grazes on algae and can strongly affect kelp beds when abundant.

[Source 1](https://www.marlin.ac.uk/species/detail/1547).

### reef_manta_ray — *Mobula alfredi*

A large ray that filters tiny drifting animals from seawater. Research shows its food can originate from surface waters, near the seabed and deeper water.

[Source 1](https://pmc.ncbi.nlm.nih.gov/articles/PMC6774984/).

### kitefin_shark — *Dalatias licha*

A shark of deep waters. Scientists have documented its blue light emission, produced by tiny light organs in its skin.

[Source 1](https://www.frontiersin.org/journals/marine-science/articles/10.3389/fmars.2021.633582/full).

### orange_roughy — *Hoplostethus atlanticus*

A slow-growing, long-lived fish of deep waters, including seamounts. It eats prey such as fish, squid and prawns.

[Source 1](https://www.afma.gov.au/sites/default/files/2025-02/stock-assessment-for-eastern-zone-orange-roughy-2021.pdf).

### pinecone_fish — *Monocentris japonica*

A reef-associated fish also called the Japanese pineapplefish. It is one of the fish species capable of producing light.

[Source 1](https://fishesofaustralia.net.au/home/species/3730).

### spotted_lanternfish — *Myctophum punctatum*

A small fish with light-producing organs called photophores. These organs create points of light on its body.

[Source 1](https://www.vliz.be/imisdocs/publications/ocrd/278773.pdf).

### king_crab — *Neolithodes agassizii*

A deep-sea king crab. It has been observed feeding on a mussel or its attachment threads at a deep-sea site.

[Source 1](https://repository.library.noaa.gov/view/noaa/17449/noaa_17449_DS1.pdf).

### warty_squid — *Moroteuthopsis longimana*

A large squid of the Southern Ocean. It is eaten by predators such as toothfish, seabirds and marine mammals.

[Source 1](https://nora.nerc.ac.uk/id/eprint/533806/); [Source 2](https://www.marionseals.com/blog/2021/2/10/new-paper-using-stable-isotopes-from-squid-beaks-to-reveal-the-ecology-of-a-dominant-prey-species-at-the-prince-edward-islands).

### flapjack_octopus — *Opisthoteuthis californiana*

A finned octopus associated with the deep seafloor. Like other flapjack octopuses, it has a soft body and webbing between its arms.

[Source 1](https://library.oarcloud.noaa.gov/oedv.lib/Okeanos_Explorer_2023_EX2301/EX2301_Dive_Summary_09.pdf); [Source 2](https://www.mbari.org/animal/flapjack-octopus/).

### giant_pacific_octopus — *Enteroctopus dofleini*

A large octopus of the North Pacific, found in coastal and deeper waters. It uses camouflage and strong suckers to catch prey such as crabs and shellfish.

[Source 1](https://www.montereybayaquarium.org/animals/animals-a-to-z/giant-pacific-octopus).

### bone_eating_worm — *Osedax frankpressi*

A worm first described from whale bones in Monterey Canyon. Females grow root-like tissues containing symbiotic bacteria that help them obtain nutrients from bone.

[Source 1](https://www.mbari.org/news/whale-carcass-yields-bone-devouring-worms/); [Source 2](https://www.mbari.org/project/osedax-studies/).

### tree_bubblegum_coral — *Paragorgia arborea*

A deep-water coral that forms branching colonies attached to hard surfaces. Large colonies can be very old and provide structure on the seafloor.

[Source 1](https://oceanexplorer.noaa.gov/multimedia/okeanos-explorations-ex1905-logs-aug29-media-coral/); [Source 2](https://oceanexplorer.noaa.gov/multimedia/okeanos-explorations-ex2104-dives-dive19-media-bubblegum-coral/).

### precious_coral — *Corallium rubrum*

A colonial animal whose small polyps capture suspended food from the water. It builds the skeleton associated with Mediterranean red coral.

[Source 1](https://pmc.ncbi.nlm.nih.gov/articles/PMC6625502/); [Source 2](https://pmc.ncbi.nlm.nih.gov/articles/PMC6919573/).

### zigzag_coral — *Madrepora oculata*

A cold-water stony coral that grows into branching colonies. Its skeleton adds structure to deep-sea habitats.

[Source 1](https://www.marlin.ac.uk/habitats/detail/1311/discrete_lophelia_pertusa_colonies_on_atlantic_mid_bathyal_coarse_sediment).

### moon_jellyfish — *Aurelia aurita*

A jellyfish with a translucent bell and four horseshoe-shaped reproductive organs. Its short tentacles help capture planktonic food.

[Source 1](https://www.marlin.ac.uk/species/detail/2089).
