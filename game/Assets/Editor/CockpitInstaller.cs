using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CockpitInstaller
{
    const string Art = "Assets/Art/Rosette-Deployment/Cockpit/";
    const string SamplingRosettePath = Art+"ctd_rosette_4_bottle.png";
    static readonly Vector2 SamplingRosetteHalfSize = new Vector2(177.5f,247.5f);
    static readonly Vector2 RecoveryRosetteHalfSize = new Vector2(150f,150f);
    const string ScenePath = "Assets/Scenes/CTD-Minigame.unity";
    static readonly Color Cyan = new Color(.1f,.85f,1f);
    static Sprite frame, bottle;
    static Material frameMaterial, bottleMaterial;

    // Deliberately explicit: never rebuild a scene on import or at runtime.
    [MenuItem("OceanX/Restore Approved Sampling Cockpit")]
    public static void RestoreApproved()
    {
        Install();
        Polish();
        PrepareCleaning();
        CleanMap();
        InstallSamplingReactionExperience();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var manager = UnityEngine.Object.FindAnyObjectByType<CTDGameManager>(FindObjectsInactive.Include);
        if (manager.samplingController.cockpit == null || manager.rosetteEntrySequence.cockpit == null)
            throw new InvalidOperationException("Saved cockpit references are missing.");
        var before = System.IO.File.ReadAllBytes(ScenePath);
        CTDSceneBuilder.Build();
        if (!before.SequenceEqual(System.IO.File.ReadAllBytes(ScenePath)))
            throw new InvalidOperationException("Legacy builder changed the saved scene.");
        Debug.Log("COCKPIT_RESTORE_VERIFIED: saved references and legacy rebuild protection passed.");
    }

    public static void Install()
    {
        AssetDatabase.Refresh();
        frame = ImportSprite(Art+"cockpit_frame_v2.png");
        bottle = ImportSprite(Art+"niskin_indicator.png");
        frameMaterial = MaterialAsset("Rosette/CockpitWindow", Art+"CockpitFrame.mat");
        bottleMaterial = MaterialAsset("Rosette/SeamountBlackKey", Art+"NiskinKey.mat");
        bottleMaterial.SetFloat("_BlackCutoff", .025f);
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var manager = UnityEngine.Object.FindAnyObjectByType<CTDGameManager>(FindObjectsInactive.Include);
        if (manager.samplingPanel.transform.Find("SamplingCockpit") != null)
            throw new InvalidOperationException("Cockpit already installed. Edit the prefab or scene directly to preserve hand-adjusted layout.");
        const string prefabPath = "Assets/Prefabs/SamplingCockpit.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            var template = CreateCockpit();
            prefab = PrefabUtility.SaveAsPrefabAsset(template.gameObject, prefabPath);
            UnityEngine.Object.DestroyImmediate(template.gameObject);
        }

        var launch = (GameObject)PrefabUtility.InstantiatePrefab(prefab, manager.launchPanel.transform);
        var sampling = (GameObject)PrefabUtility.InstantiatePrefab(prefab, manager.samplingPanel.transform);
        launch.name = sampling.name = "SamplingCockpit";
        Stretch(launch.GetComponent<RectTransform>()); Stretch(sampling.GetComponent<RectTransform>());
        var lv=launch.GetComponent<SamplingCockpitView>(); var sv=sampling.GetComponent<SamplingCockpitView>();
        foreach (Transform t in manager.launchPanel.transform)
            if(t != launch.transform && t.name != "AnimationSurface") t.gameObject.SetActive(false);
        var entry=manager.rosetteEntrySequence;
        entry.cockpit=lv; entry.readyButton=lv.actionButton; entry.readyLabel=lv.actionLabel;
        manager.transitionStatusText=lv.actionLabel;
        lv.actionLabel.text="READY";
        Stretch(entry.animationSurface.rectTransform);
        entry.animationSurface.transform.SetAsFirstSibling();

        var c=manager.samplingController;
        foreach (Transform t in manager.samplingPanel.transform)
            if(t != sampling.transform && t.name != "OceanBackground" && t.name != "CTDRosette") t.gameObject.SetActive(false);
        Stretch(c.oceanBackground.rectTransform);
        c.oceanBackground.transform.SetAsFirstSibling();
        c.cockpit=sv; c.depthGauge=sv.gauge; c.depthMarker=sv.marker; c.targetBand=sv.targetBand;
        c.depthText=sv.depthLabel; c.closeBottleButton=sv.actionButton;
        c.targetDepths=new[]{820,600,380,180};
        c.targetZones=new[]{"Deep", "Lower midwater", "Upper midwater", "Shallow"};
        c.targetTolerance=45;
        c.bottleImages=new Image[4]; c.bottleStatusTexts=new TMP_Text[4];
        for(int i=0;i<4;i++) {
            c.bottleImages[i]=sv.bottleOutlines[i].transform.Find("Bottle").GetComponent<Image>();
            var status=Text("RecordStatus", sv.bottleOutlines[i].transform, "OPEN", Vector2.zero, new Vector2(100,20), 12);
            status.gameObject.SetActive(false); c.bottleStatusTexts[i]=status;
        }
        // Retain gameplay readouts as separately movable UI, positioned in the viewport.
        PlaceText(c.phaseText, sampling.transform, new Vector2(0,365), new Vector2(1280,40),25);
        PlaceText(c.targetText,sampling.transform,new Vector2(0,-305),new Vector2(1280,45),24);
        PlaceText(c.feedbackText,sampling.transform,new Vector2(0,-350),new Vector2(1280,35),20);
        c.sensorText.gameObject.SetActive(false);
        var hint=c.tutorialHintText.transform.parent;
        hint.SetParent(sampling.transform,false); Set(hint.GetComponent<RectTransform>(),new Vector2(0,280),new Vector2(1120,60));
        c.tutorialHintText.fontSize=24; Stretch(c.tutorialHintText.rectTransform); hint.gameObject.SetActive(false);
        // Keep the already authored rosette graphic and animation separate from the HUD.
        var rosette=manager.samplingPanel.transform.Find("CTDRosette").GetComponent<RectTransform>();
        rosette.anchoredPosition=new Vector2(0,0);
        foreach(var b in rosette.GetComponentsInChildren<Image>()) b.raycastTarget=false;
        manager.launchPanel.SetActive(false); manager.samplingPanel.SetActive(false);
        manager.mapHub.gameObject.SetActive(true);
        EditorUtility.SetDirty(c); EditorUtility.SetDirty(entry); EditorUtility.SetDirty(manager);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("COCKPIT_INSTALL_OK");
    }

    public static void Polish()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var manager=UnityEngine.Object.FindAnyObjectByType<CTDGameManager>(FindObjectsInactive.Include);
        var c=manager.samplingController;
        PlaceText(c.feedbackText,c.cockpit.transform,new Vector2(0,-315),new Vector2(1280,38),20);
        PlaceText(c.targetText,c.cockpit.transform,new Vector2(0,-270),new Vector2(1280,40),24);
        var backing=Img("ReadoutBacking",c.cockpit.transform,new Vector2(0,-293),new Vector2(1320,92),new Color(.01f,.035f,.055f,.85f));
        backing.transform.SetSiblingIndex(c.targetText.transform.GetSiblingIndex());
        var phaseBacking=Img("PhaseBacking",c.cockpit.transform,new Vector2(0,365),new Vector2(1280,42),new Color(.01f,.035f,.055f,.85f));
        phaseBacking.transform.SetSiblingIndex(c.phaseText.transform.GetSiblingIndex());
        var rosette=manager.samplingPanel.transform.Find("CTDRosette");
        foreach(Transform t in rosette)t.gameObject.SetActive(false);
        var ri=rosette.GetComponent<Image>();
        ri.sprite=ImportSprite(SamplingRosettePath);
        ri.color=Color.white;ri.preserveAspect=true;
        Set(rosette.GetComponent<RectTransform>(),new Vector2(0,15),new Vector2(355,495));
        foreach(var view in new[]{c.cockpit,manager.rosetteEntrySequence.cockpit}) {
            view.actionButton.image.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            view.actionButton.image.type=Image.Type.Sliced;
            view.buttonFace.sprite=view.actionButton.image.sprite;view.buttonFace.type=Image.Type.Sliced;
            foreach(var outline in view.bottleOutlines) {
                var icon=outline.transform.Find("Bottle") as RectTransform;icon.sizeDelta=new Vector2(95,180);
            }
        }
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("COCKPIT_POLISH_OK");
    }

    public static void PrepareCleaning()
    {
        AssetDatabase.Refresh();
        var bg=ImportSprite(Art+"preparation_lab.png");var hand=ImportSprite(Art+"gloved_cleaning_hand.png");
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var manager=UnityEngine.Object.FindAnyObjectByType<CTDGameManager>(FindObjectsInactive.Include);
        var clean=manager.cleaningMinigame;
        var panel=manager.cleaningPanel.transform;
        var background=panel.GetComponent<Image>();background.sprite=bg;background.color=Color.white;
        var note=Img("YellowInstructionNote",panel,new Vector2(-600,30),new Vector2(420,540),new Color(1f,.9f,.3f));
        note.rectTransform.localEulerAngles=new Vector3(0,0,3);
        Text("NoteTitle",note.transform,"CLEANING NOTES",new Vector2(0,210),new Vector2(380,50),30).color=new Color(.1f,.14f,.15f);
        PlaceText(clean.explanationText,note.transform,new Vector2(0,70),new Vector2(350,170),25);
        clean.explanationText.color=new Color(.1f,.14f,.15f);
        PlaceText(clean.instructionText,note.transform,new Vector2(0,-130),new Vector2(350,170),26);
        clean.instructionText.color=new Color(.1f,.14f,.15f);
        var title=panel.Find("Title").GetComponent<TMP_Text>();title.color=Color.white;
        var titleBack=Img("TitleBacking",panel,new Vector2(0,450),new Vector2(1350,85),new Color(.01f,.04f,.07f,.85f));
        titleBack.transform.SetSiblingIndex(title.transform.GetSiblingIndex());
        var target=clean.targets[0];
        Set(target.targetRect,new Vector2(-20,0),new Vector2(360,690));
        target.targetRect.GetComponent<Image>().color=new Color(.02f,.05f,.08f,.72f);
        Set(target.equipmentImage.rectTransform,new Vector2(0,45),new Vector2(225,460));
        var tool=clean.manualCleaningGroup.GetComponentsInChildren<CleaningTool>(true).First(t=>t.toolType==CleaningToolType.DecontaminationSolution);
        var image=tool.GetComponent<Image>();image.sprite=hand;image.preserveAspect=true;image.color=Color.white;
        image.material=AssetDatabase.LoadAssetAtPath<Material>(Art+"NiskinKey.mat");
        Set(tool.GetComponent<RectTransform>(),new Vector2(470,-30),new Vector2(480,480));
        foreach(var label in tool.GetComponentsInChildren<TMP_Text>()){label.text="DRAG TO CLEAN";Set(label.rectTransform,new Vector2(0,-225),new Vector2(400,45));label.color=new Color(.02f,.07f,.12f);}
        var rinse=clean.manualCleaningGroup.GetComponentsInChildren<CleaningTool>(true).First(t=>t.toolType==CleaningToolType.SterileWater);
        Set(rinse.GetComponent<RectTransform>(),new Vector2(460,-365),new Vector2(330,85));
        clean.continueButton.transform.SetAsLastSibling();
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("CLEANING_ART_OK");
    }

    public static void CleanMap()
    {
        AssetDatabase.Refresh();
        var sprite=ImportSprite("Assets/Art/Rosette-Deployment/Map/seamount_clean_v2.png");
        var waypointSprite=ImportSprite("Assets/Art/Rosette-Deployment/Extracted/single_waypoint_marker_clean.png");
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var manager=UnityEngine.Object.FindAnyObjectByType<CTDGameManager>(FindObjectsInactive.Include);
        var map=manager.mapHub.GetComponentsInChildren<Image>(true).First(i=>i.name=="Seamount");
        map.sprite=sprite;
        foreach (var button in manager.mapHub.waypointButtons)
        {
            var marker=button.GetComponent<Image>();
            if (marker == null) continue;
            marker.sprite=waypointSprite;
            marker.material=null;
            marker.preserveAspect=true;
        }
        var clean=manager.cleaningMinigame;
        var tool=clean.manualCleaningGroup.GetComponentsInChildren<CleaningTool>(true).First(t=>t.toolType==CleaningToolType.DecontaminationSolution);
        tool.contactPoint=Empty("SpongeContactPoint",tool.transform,new Vector2(-105,100),new Vector2(35,35));
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("MAP_CLEANUP_OK");
    }

    [MenuItem("OceanX/Install Sampling Reaction Experience")]
    public static void InstallSamplingReactionExperience()
    {
        AssetDatabase.Refresh();
        var rosetteSprite=ImportSprite(SamplingRosettePath);
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var manager=UnityEngine.Object.FindAnyObjectByType<CTDGameManager>(FindObjectsInactive.Include);

        var samplingRosette=manager.samplingPanel.transform.Find("CTDRosette");
        if(samplingRosette!=null)
        {
            var image=samplingRosette.GetComponent<Image>();
            image.sprite=rosetteSprite;
            image.color=Color.white;
            image.preserveAspect=true;
            image.material=null;
            image.raycastTarget=false;
            samplingRosette.GetComponent<RectTransform>().sizeDelta=SamplingRosetteHalfSize;
            foreach(Transform child in samplingRosette) child.gameObject.SetActive(false);
        }

        if(manager.recoveryRosette!=null)
        {
            var recoveryImage=manager.recoveryRosette.GetComponent<Image>();
            recoveryImage.sprite=rosetteSprite;
            recoveryImage.color=Color.white;
            recoveryImage.preserveAspect=true;
            recoveryImage.material=null;
            recoveryImage.raycastTarget=false;
            manager.recoveryRosette.sizeDelta=RecoveryRosetteHalfSize;
            foreach(Transform child in manager.recoveryRosette) child.gameObject.SetActive(false);
        }

        const string prefabPath="Assets/Prefabs/SamplingCockpit.prefab";
        var prefabRoot=PrefabUtility.LoadPrefabContents(prefabPath);
        if(prefabRoot!=null)
        {
            var prefabView=prefabRoot.GetComponent<SamplingCockpitView>();
            EnsureBottleStateOverlays(prefabView);
            prefabView.flashCyclesPerSecond=4f;
            EditorUtility.SetDirty(prefabView);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot,prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        EnsureBottleStateOverlays(manager.samplingController.cockpit);
        manager.samplingController.cockpit.flashCyclesPerSecond=4f;
        if(manager.rosetteEntrySequence!=null)
            EnsureBottleStateOverlays(manager.rosetteEntrySequence.cockpit);

        var ocean=EnsureOceanLayers(manager.samplingPanel.transform,manager.samplingController.oceanBackground);
        manager.samplingController.oceanVisuals=ocean;
        EditorUtility.SetDirty(manager.samplingController);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("SAMPLING_REACTION_EXPERIENCE_OK");
    }

    static void EnsureBottleStateOverlays(SamplingCockpitView view)
    {
        if(view==null || view.bottleOutlines==null) return;
        var overlays=new Image[view.bottleOutlines.Length];
        for(int index=0;index<view.bottleOutlines.Length;index++)
        {
            var outline=view.bottleOutlines[index];
            if(outline==null) continue;
            var existing=outline.transform.Find("BottleStateOverlay");
            Image overlay;
            if(existing!=null)
            {
                overlay=existing.GetComponent<Image>();
            }
            else
            {
                var overlayRect=Empty("BottleStateOverlay",outline.transform,Vector2.zero,Vector2.zero);
                overlayRect.anchorMin=Vector2.zero;
                overlayRect.anchorMax=Vector2.one;
                overlayRect.offsetMin=new Vector2(4f,4f);
                overlayRect.offsetMax=new Vector2(-4f,-4f);
                overlay=overlayRect.gameObject.AddComponent<Image>();
                overlay.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                overlay.raycastTarget=false;
                var number=outline.transform.Find("Number");
                overlayRect.SetSiblingIndex(number!=null?number.GetSiblingIndex():outline.transform.childCount-1);
            }
            overlay.color=view.collectedGreen;
            overlay.gameObject.SetActive(false);
            overlays[index]=overlay;
        }
        view.bottleStateOverlays=overlays;
    }

    static SamplingOceanBackground EnsureOceanLayers(Transform panel,Image baseLayer)
    {
        if(baseLayer==null) return null;
        var ocean=baseLayer.GetComponent<SamplingOceanBackground>();
        if(ocean==null) ocean=baseLayer.gameObject.AddComponent<SamplingOceanBackground>();
        var whiteSprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var gradientMaterial=MaterialAsset("Rosette/OceanDepthGradient","Assets/Art/Rosette-Deployment/Backgrounds/SamplingOceanGradient.mat");
        var refractionMaterial=MaterialAsset("Rosette/OceanSurfaceRefraction","Assets/Art/Rosette-Deployment/Backgrounds/SamplingOceanRefraction.mat");

        var gradient=baseLayer.transform.Find("DepthGradientLayer");
        Image gradientImage;
        if(gradient==null)
        {
            gradientImage=Img("DepthGradientLayer",baseLayer.transform,Vector2.zero,Vector2.zero,Color.white);
            Stretch(gradientImage.rectTransform);
        }
        else gradientImage=gradient.GetComponent<Image>();
        gradientImage.sprite=whiteSprite;
        gradientImage.material=gradientMaterial;

        var refraction=baseLayer.transform.Find("SurfaceRefractionLayer");
        Image refractionImage;
        if(refraction==null)
        {
            refractionImage=Img("SurfaceRefractionLayer",baseLayer.transform,Vector2.zero,Vector2.zero,Color.white);
            Stretch(refractionImage.rectTransform);
        }
        else refractionImage=refraction.GetComponent<Image>();
        refractionImage.sprite=whiteSprite;
        refractionImage.material=refractionMaterial;

        var beams=new Image[3];
        string[] beamNames={"LightBeamLeft","LightBeamCenter","LightBeamRight"};
        Vector2[] beamPositions={new Vector2(-540f,360f),new Vector2(0f,430f),new Vector2(520f,345f)};
        Vector2[] beamSizes={new Vector2(220f,820f),new Vector2(270f,880f),new Vector2(210f,760f)};
        float[] beamRotations={-17f,2f,16f};
        for(int index=0;index<beams.Length;index++)
        {
            var beamTransform=baseLayer.transform.Find(beamNames[index]);
            Image beam;
            if(beamTransform==null)
            {
                beam=Img(beamNames[index],baseLayer.transform,beamPositions[index],beamSizes[index],ocean.beamTint);
                beam.rectTransform.localEulerAngles=new Vector3(0f,0f,beamRotations[index]);
            }
            else beam=beamTransform.GetComponent<Image>();
            beam.sprite=whiteSprite;
            beam.raycastTarget=false;
            beams[index]=beam;
        }

        var particleField=baseLayer.transform.Find("SuspendedParticleField");
        RectTransform particleParent;
        if(particleField==null)
        {
            particleParent=Empty("SuspendedParticleField",baseLayer.transform,Vector2.zero,Vector2.zero);
            Stretch(particleParent);
        }
        else particleParent=particleField.GetComponent<RectTransform>();

        const int particleCount=42;
        var particles=new RectTransform[particleCount];
        for(int index=0;index<particleCount;index++)
        {
            string name=$"Particle-{index+1:00}";
            var particleTransform=particleParent.Find(name);
            Image particle;
            if(particleTransform==null)
            {
                float x=Mathf.Repeat(index*317.31f,1820f)-910f;
                float y=Mathf.Repeat(index*173.17f,980f)-490f;
                float size=2.5f+Mathf.Repeat(index*1.73f,5.5f);
                particle=Img(name,particleParent,new Vector2(x,y),new Vector2(size,size),new Color(.68f,.90f,1f,.5f));
            }
            else particle=particleTransform.GetComponent<Image>();
            particle.sprite=whiteSprite;
            particle.raycastTarget=false;
            particles[index]=particle.rectTransform;
        }

        ocean.baseLayer=baseLayer;
        ocean.depthGradientLayer=gradientImage;
        ocean.surfaceRefractionLayer=refractionImage;
        ocean.lightBeams=beams;
        ocean.particleDots=particles;
        return ocean;
    }

    static SamplingCockpitView CreateCockpit()
    {
        var root=new GameObject("SamplingCockpit",typeof(RectTransform),typeof(SamplingCockpitView));
        Set(root.GetComponent<RectTransform>(),Vector2.zero,new Vector2(1920,1080));
        var view=root.GetComponent<SamplingCockpitView>();
        var border=Img("FrameArtwork",root.transform,Vector2.zero,new Vector2(1920,1080),Color.white);
        Stretch(border.rectTransform); border.sprite=frame; border.material=frameMaterial;
        var left=Empty("DepthPanel",root.transform,new Vector2(-853,-5),new Vector2(160,740));
        Text("Title",left,"DEPTH",new Vector2(0,340),new Vector2(140,35),22);
        view.depthLabel=Text("CurrentDepth",left,"0 m",new Vector2(0,288),new Vector2(145,50),34); view.depthLabel.color=Cyan;
        view.gauge=Empty("DepthGauge",left,new Vector2(-23,230),new Vector2(26,540));
        view.gauge.pivot=new Vector2(.5f,1);
        Img("Track",view.gauge,new Vector2(0,-270),new Vector2(3,540),Cyan);
        for(int i=0;i<=10;i++) {
            float y=-54*i;
            Img("Tick"+i,view.gauge,new Vector2(8,y),new Vector2(i%2==0?20:12,2),new Color(.55f,.65f,.7f));
            if(i%2==0) Text("Depth"+i,view.gauge,(i*100)+" m",new Vector2(54,y),new Vector2(68,28),16);
        }
        view.targetBand=Img("TargetDepthBand",view.gauge,Vector2.zero,new Vector2(122,48),new Color(.1f,.7f,1f,.3f)).rectTransform;
        view.marker=Img("CurrentDepthMarker",view.gauge,Vector2.zero,new Vector2(42,6),Cyan).rectTransform;
        foreach(var r in new[]{view.targetBand,view.marker}) {r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,.5f);}
        // Gauge children are positioned relative to its TOP, not its center.
        foreach(Transform t in view.gauge) {var rt=t as RectTransform;rt.anchorMin=rt.anchorMax=new Vector2(.5f,1);}
        view.targetBand.gameObject.SetActive(false);
        var right=Empty("BottlePanel",root.transform,new Vector2(852,-5),new Vector2(165,740));
        view.bottleOutlines=new Image[4]; view.bottleChecks=new TMP_Text[4];
        for(int i=0;i<4;i++) {
            var outline=Img("BottleSlot"+(i+1),right,new Vector2(0,270-i*180),new Vector2(142,172),new Color(.12f,.2f,.25f));
            view.bottleOutlines[i]=outline;
            Img("Inset",outline.transform,Vector2.zero,new Vector2(136,166),new Color(.018f,.035f,.047f));
            var icon=Img("Bottle",outline.transform,new Vector2(12,0),new Vector2(70,162),Color.white);
            icon.sprite=bottle; icon.preserveAspect=true; icon.material=bottleMaterial;
            Text("Number",outline.transform,$"{i+1:00}",new Vector2(-43,60),new Vector2(40,35),24);
            var check=Text("CollectedCheck",outline.transform,"OK",new Vector2(-44,-57),new Vector2(40,26),16);check.color=Cyan;check.gameObject.SetActive(false);view.bottleChecks[i]=check;
        }
        var button=Img("ActionButton",root.transform,new Vector2(0,-430),new Vector2(400,90),Cyan);
        view.actionButton=button.gameObject.AddComponent<Button>();view.actionButton.transition=Selectable.Transition.None;
        button.raycastTarget=true;
        view.buttonFace=Img("ColourFace",button.transform,Vector2.zero,new Vector2(392,82),view.normalBlue);
        view.actionButton.targetGraphic=button;
        view.actionLabel=Text("Label",button.transform,"CLOSE BOTTLE",Vector2.zero,new Vector2(378,68),34);
        view.actionLabel.fontStyle=FontStyles.Bold;
        return view;
    }
    static void PlaceText(TMP_Text t,Transform parent,Vector2 pos,Vector2 size,float font) { t.transform.SetParent(parent,false);Set(t.rectTransform,pos,size);t.fontSize=font;t.gameObject.SetActive(true);t.raycastTarget=false; }
    static Sprite ImportSprite(string path) { var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=4096;ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path); }
    static Material MaterialAsset(string shader,string path) {var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}return m;}
    static RectTransform Empty(string name,Transform parent,Vector2 pos,Vector2 size) {var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();Set(r,pos,size);return r;}
    static Image Img(string name,Transform parent,Vector2 pos,Vector2 size,Color color) {var r=Empty(name,parent,pos,size);var i=r.gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=false;return i;}
    static TMP_Text Text(string name,Transform parent,string text,Vector2 pos,Vector2 size,float font) {var r=Empty(name,parent,pos,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.text=text;t.font=TMP_Settings.defaultFontAsset;t.fontSize=font;t.alignment=TextAlignmentOptions.Center;t.color=Color.white;t.raycastTarget=false;return t;}
    static void Set(RectTransform r,Vector2 pos,Vector2 size) {r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;r.localScale=Vector3.one;}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;}
}
