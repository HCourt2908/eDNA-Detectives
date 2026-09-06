using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CockpitChecks
{
    static int step;
    static double since;
    static CTDSamplingController c;
    static bool completed, sawBlue, sawYellow;
    static Vector2 originalPosition;
    static readonly BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    static CockpitChecks(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CTD-Minigame.unity");
        SessionState.SetBool("CockpitChecks",true);
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool("CockpitChecks",false)||!EditorApplication.isPlaying) return;
        try {
            if(step==0) {since=EditorApplication.timeSinceStartup;step=1;return;}
            if(step!=10 && EditorApplication.timeSinceStartup-since<1) return;
            if(step==1) {
                var manager=UnityEngine.Object.FindAnyObjectByType<CTDGameManager>();
                c=manager.samplingController;
                Capture("/tmp/oceanx-map.png");
                manager.mapHub.Hide();manager.cleaningPanel.SetActive(true);manager.cleaningMinigame.Begin();
                Capture("/tmp/oceanx-cleaning.png");
                // Cleaning now uses local foam coverage and is tested through the scene interaction.
                // This cockpit verifier only needs to open the stage without bypassing it.
                manager.cleaningPanel.SetActive(false);manager.samplingPanel.SetActive(true);c.Begin("Verification");
                Require(c.targetDepths.Length==4,"Four sample targets");
                Require(c.cockpit.bottleChecks.Length==4,"Four bottle indicators");
                originalPosition=c.cockpit.GetComponent<RectTransform>().anchoredPosition;
                c.cockpit.GetComponent<RectTransform>().anchoredPosition=originalPosition+new Vector2(7,0);
                Require(!c.cockpit.CueActive,"No cue before the first upcast window");
                Set("currentDepth",950f);Call("CloseBottle");
                Require((int)Get("currentTargetIndex")==0,"Out-of-range click must not collect");
                Set("currentDepth",820f);Call("UpdateTargetBand");Call("HandleTargetApproach");Call("RefreshDisplay");
                c.phaseText.text="UPCAST — collect water as the CTD rises";
                Require(c.cockpit.CueActive,"Target cue active");
                Require(c.closeBottleButton.interactable,"Button enabled in band");
                Require(c.cockpit.targetBand.GetComponent<Image>().color.a>.8f,"Target band prominent");
                since=EditorApplication.timeSinceStartup;step=10;return;
            } else if(step==10) {
                Require(c.cockpit.GetComponent<RectTransform>().anchoredPosition==originalPosition+new Vector2(7,0),"Hand-authored placement persists during updates");
                if(c.cockpit.buttonFace.color==c.cockpit.alertYellow) {sawYellow=true;Capture("/tmp/oceanx-cockpit-alert.png");}
                if(c.cockpit.buttonFace.color==c.cockpit.normalBlue) {sawBlue=true;Capture("/tmp/oceanx-cockpit-ready.png");}
                if(!sawBlue||!sawYellow) {if(EditorApplication.timeSinceStartup-since>4)throw new Exception("Button failed blue/yellow alternation");return;}
                c.cockpit.GetComponent<RectTransform>().anchoredPosition=originalPosition;
                c.SamplingCompleted+=samples=>{Require(samples.Length==4,"Four output records");foreach(var s in samples)Require(s!=null,"No null records");completed=true;};
                for(int i=0;i<4;i++) {
                    Set("currentDepth",(float)c.targetDepths[i]);Call("CloseBottle");
                    Require(c.cockpit.bottleChecks[i].gameObject.activeSelf,"Bottle lights on successful sample");
                    Require(!c.cockpit.CueActive,"Flash stopped on collection");
                }
                Capture("/tmp/oceanx-cockpit-collected.png");
                since=EditorApplication.timeSinceStartup;step=2;
            } else if(step==2 && EditorApplication.timeSinceStartup-since>2) {
                Require(completed,"Completion event fired");
                var manager=UnityEngine.Object.FindAnyObjectByType<CTDGameManager>();
                Require(manager.CurrentState==CTDGameState.Recovering||manager.CurrentState==CTDGameState.Complete,"Stage transition after collection");
                manager.recoveryPanel.SetActive(false);manager.completePanel.SetActive(false);manager.samplingPanel.SetActive(true);c.Begin("Replay");
                foreach(var t in c.cockpit.bottleChecks)Require(!t.gameObject.activeSelf,"Replay clears indicators");
                Require(c.cockpit.GetComponent<RectTransform>().anchoredPosition==originalPosition,"Layout preserved");
                File.WriteAllText("/tmp/oceanx-cockpit-checks.txt","PASS: four targets; invalid-depth rejection; in-range cue and button; live yellow/blue colour alternation; four independent collected indicators; cue stops on success; four complete records; recovery transition; replay reset; layout retained.\n");
                SessionState.SetBool("CockpitChecks",false);Debug.Log("COCKPIT_CHECKS_PASS");EditorApplication.Exit(0);
            }
        } catch(Exception e) {File.WriteAllText("/tmp/oceanx-cockpit-checks.txt",e.ToString());SessionState.SetBool("CockpitChecks",false);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static object Get(string name)=>typeof(CTDSamplingController).GetField(name,Hidden).GetValue(c);
    static void Set(string name,object value)=>typeof(CTDSamplingController).GetField(name,Hidden).SetValue(c,value);
    static void Call(string name)=>typeof(CTDSamplingController).GetMethod(name,Hidden).Invoke(c,null);
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Capture(string path)
    {
        var canvas=c.GetComponentInParent<Canvas>();var camera=Camera.main;
        var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var rt=new RenderTexture(1920,1080,24);camera.targetTexture=rt;
        Canvas.ForceUpdateCanvases();camera.Render();var prev=RenderTexture.active;RenderTexture.active=rt;
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        RenderTexture.active=prev;camera.targetTexture=null;canvas.renderMode=mode;canvas.worldCamera=oldCamera;UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(rt);
    }
}
