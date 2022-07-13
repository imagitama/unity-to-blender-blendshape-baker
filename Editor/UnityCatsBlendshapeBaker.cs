using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;

using PeanutTools_UnityCatsBlendshapeBaker;

public class UnityCatsBlendshapeBaker : EditorWindow
{   
    public class Result {
        public string meshName;
        public string blendShapeName;
        public float amount;
        public bool bake;
    }

    Vector2 scrollPosition;

    // user input
    Transform rootTransform;
    List<UnityEditor.Animations.AnimatorController> animatorControllers = new List<UnityEditor.Animations.AnimatorController>();
    List<Result> customResults = new List<Result>();

    // output
    List<Result> results = new List<Result>();

    [MenuItem("PeanutTools/CATS Blendshape Baker")]
    public static void ShowWindow()
    {
        var window = GetWindow<UnityCatsBlendshapeBaker>();
        window.titleContent = new GUIContent("CATS Blendshape Baker");
        window.minSize = new Vector2(400, 200);
    }

    void OnGUI() {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        CustomGUI.BoldLabel("CATS Blendshape Baker");
        CustomGUI.ItalicLabel("Export blendshapes for baking using CATS.");

        CustomGUI.LineGap();

        CustomGUI.HorizontalRule();
        
        CustomGUI.LineGap();

        #if VRC_SDK_VRCSDK3
        CustomGUI.BoldLabel("Step 1: Select VRChat avatar");
        CustomGUI.ItalicLabel("Used to extract visemes and blendshape values.");
        #else
        CustomGUI.BoldLabel("Step 1: Select root game object");
        CustomGUI.ItalicLabel("Used to extract blendshape values.");
        #endif
        
        CustomGUI.SmallLineGap();
        
        rootTransform = EditorGUILayout.ObjectField("Object:", rootTransform, typeof(Transform)) as Transform;

        RenderVRCBlendshapes();

        CustomGUI.LineGap();

        CustomGUI.HorizontalRule();
        
        CustomGUI.LineGap();
        
        CustomGUI.BoldLabel("Step 2: Select an animator controller");
        CustomGUI.ItalicLabel("Used to determine if blendshapes should be baked or not.");
        
        CustomGUI.SmallLineGap();

        #if VRC_SDK_VRCSDK3
        CustomGUI.BoldLabel("Detected these animator controllers from VRChat avatar:");
        #endif
        RenderAnimatorControllers();

        #if VRC_SDK_VRCSDK3
        #else
        CustomGUI.SmallLineGap();

        var newAnimatorController = (UnityEditor.Animations.AnimatorController)EditorGUILayout.ObjectField("Add:", null, typeof(UnityEditor.Animations.AnimatorController));

        if (newAnimatorController != null) {
            AddAnimatorController(newAnimatorController);
        }
        #endif

        RenderVRCInputs();
        
        CustomGUI.LineGap();

        CustomGUI.HorizontalRule();

        CustomGUI.LineGap();
        
        CustomGUI.BoldLabel("Step 3: Run");

        CustomGUI.SmallLineGap();

        EditorGUI.BeginDisabledGroup(rootTransform == null || animatorControllers.Count == 0);
        
        CustomGUI.SmallLineGap();
        
        if (CustomGUI.PrimaryButton("Run")) {
            Run();
        }

        RenderResults();

        EditorGUI.EndDisabledGroup();
        
        CustomGUI.SmallLineGap();
        
        CustomGUI.MyLinks("unity-cats-blendshape-baker");
        
        EditorGUILayout.EndScrollView();
    }

    void RenderVRCInputs() {
        #if VRC_SDK_VRCSDK3
        
        CustomGUI.SmallLineGap();

        CustomGUI.BoldLabel("Detected these blendshapes from VRChat avatar:");

        if (customResults.Count > 0) {
            foreach (var customResult in customResults) {
                GUILayout.Label(customResult.meshName + "/" + customResult.blendShapeName);
            }
            
            CustomGUI.SmallLineGap();
        }
        
        #endif
    }

    void RenderVRCBlendshapes() {
        #if VRC_SDK_VRCSDK3

        if (rootTransform == null) {
            return;
        }

        VRC.SDK3.Avatars.Components.VRCAvatarDescriptor vrcAvatarDescriptor = rootTransform.gameObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();

        if (vrcAvatarDescriptor == null) {
            return;
        }

        animatorControllers = new List<UnityEditor.Animations.AnimatorController>();

        foreach (VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.CustomAnimLayer customAnimLayer in vrcAvatarDescriptor.baseAnimationLayers) {
            UnityEditor.Animations.AnimatorController controller = customAnimLayer.animatorController as UnityEditor.Animations.AnimatorController;

            if (controller != null) {
                AddAnimatorController(controller);
            }
        }

        customResults = new List<Result>();

        if (vrcAvatarDescriptor.VisemeSkinnedMesh != null) {
            foreach (var blendShapeName in vrcAvatarDescriptor.VisemeBlendShapes) {
                if (blendShapeName == "") {
                    continue;
                }

                customResults.Add(new Result() {
                    meshName = vrcAvatarDescriptor.VisemeSkinnedMesh.name,
                    blendShapeName = blendShapeName,
                    amount = GetBlendshapeAmountFromMesh(vrcAvatarDescriptor.VisemeSkinnedMesh, blendShapeName),
                    bake = false
                });
            }
        }

        if (vrcAvatarDescriptor.customEyeLookSettings.eyelidsSkinnedMesh != null) {
            foreach (var blendShapeIdx in vrcAvatarDescriptor.customEyeLookSettings.eyelidsBlendshapes) {
                string blendShapeName = vrcAvatarDescriptor.customEyeLookSettings.eyelidsSkinnedMesh.sharedMesh.GetBlendShapeName(blendShapeIdx);

                customResults.Add(new Result() {
                    meshName = vrcAvatarDescriptor.customEyeLookSettings.eyelidsSkinnedMesh.name,
                    blendShapeName = blendShapeName,
                    amount = GetBlendshapeAmountFromMesh(vrcAvatarDescriptor.customEyeLookSettings.eyelidsSkinnedMesh, blendShapeName),
                    bake = false
                });
            }
        }

        #endif
    }

    void RenderAnimatorControllers() {
        if (animatorControllers.Count == 0) {
            return;
        }

        foreach (var animatorController in animatorControllers) {
            GUILayout.BeginHorizontal();

            string path = AssetDatabase.GetAssetPath(animatorController);
            GUILayout.Label(path);

            #if VRC_SDK_VRCSDK3
            #else
            if (CustomGUI.TinyButton("x")) {
                RemoveAnimatorController(animatorController);
            }
            #endif
            
            GUILayout.EndHorizontal();
        }        
    }

    void AddAnimatorController(UnityEditor.Animations.AnimatorController newAnimatorController) {
        var newAnimatorControllers = animatorControllers.ToList();
        newAnimatorControllers.Add(newAnimatorController);
        animatorControllers = newAnimatorControllers;
    }

    void RemoveAnimatorController(UnityEditor.Animations.AnimatorController animatorControllerToRemove) {
        var newAnimatorControllers = animatorControllers.ToList();
        newAnimatorControllers.Remove(animatorControllerToRemove);
        animatorControllers = newAnimatorControllers;
    }

    string GetResultsAsText() {
        string output = "";

        var actualResults = results.ToList();
        
        foreach (var customResult in customResults) {
            var existingItem = actualResults.Find(x => x.meshName == customResult.meshName && x.blendShapeName == customResult.blendShapeName);

            if (existingItem != null) {
                existingItem.bake = false;
                continue;
            }
        }

        foreach (var result in actualResults) {
            if (output != "") {
                output += "\n";
            }

            output += result.meshName + "/" + result.blendShapeName + " " + result.amount + " " + (result.bake ? "Y" : "N");
        }

        return output;
    }

    void RenderResults() {
        if (results.Count == 0) {
            return;
        }

        CustomGUI.SmallLineGap();
        CustomGUI.BoldLabel("Output:");
        CustomGUI.SmallLineGap();

        var output = GetResultsAsText();

        GUILayout.TextArea(output);
        
        CustomGUI.SmallLineGap();

        if (CustomGUI.StandardButton("Export To File...")) {
            ExportToFile();
        }
    }

    void ExportToFile() {
        var output = GetResultsAsText();

        string absolutePath = EditorUtility.OpenFolderPanel("Select a folder to create file in", Application.dataPath, "");

        if (absolutePath == "") {
            return;
        }

        string pathToFile = absolutePath + "\\unity-cats-blendshape-baker-output.txt";

        File.WriteAllText(pathToFile, output);
    }

    string GetBlendshapeNameFromPropertyName(string propertyName) {
        return propertyName.Replace("blendShape.", "");
    }

    float GetBlendshapeAmountFromMesh(SkinnedMeshRenderer skinnedMeshRenderer, string blendShapeName) {
        int blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);
        return skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex);
    }

    float GetBlendshapeAmountFromPath(string pathToFind, string blendShapeName) {
        var skinnedMeshRenderer = rootTransform.Find(pathToFind).gameObject.GetComponent<SkinnedMeshRenderer>();
        return GetBlendshapeAmountFromMesh(skinnedMeshRenderer, blendShapeName);
    }

    void Run() {
        Debug.Log("Running...");

        List<Result> newResults = new List<Result>();

        foreach (Transform child in rootTransform) {
            var skinnedMeshRenderer = child.gameObject.GetComponent<SkinnedMeshRenderer>();

            if (skinnedMeshRenderer == null) {
                continue;
            }

            for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                string blendShapeName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);
                float amount = skinnedMeshRenderer.GetBlendShapeWeight(i);

                newResults.Add(new Result() {
                    meshName = child.gameObject.name,
                    blendShapeName = blendShapeName,
                    amount = amount,
                    bake = true
                });
            }
        }

        foreach (var animatorController in animatorControllers) {
            AnimationClip[] animationClips = animatorController.animationClips;

            Debug.Log("Found " + animationClips.Length + " animations in controller " + animatorController.name + "");

            foreach (var animationClip in animationClips) {
                foreach (var binding in AnimationUtility.GetCurveBindings(animationClip))
                {
                    if (!binding.propertyName.Contains("blendShape") || binding.path.Contains("/")) {
                        continue;
                    }

                    string meshName = binding.path;
                    string blendShapeName = GetBlendshapeNameFromPropertyName(binding.propertyName);

                    var existingItem = newResults.Find(x => x.meshName == meshName && x.blendShapeName == blendShapeName);

                    if (existingItem != null) {
                        existingItem.bake = false;
                        continue;
                    }

                    newResults.Add(new Result() {
                        meshName = meshName,
                        blendShapeName = blendShapeName,
                        amount = GetBlendshapeAmountFromPath(binding.path, blendShapeName),
                        bake = false
                    });
                }
            }
        }

        Debug.Log("Found " + newResults.Count + " blendshape animations");

        results = newResults;
    }
}