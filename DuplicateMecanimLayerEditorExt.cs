using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;


namespace MecanimLayerDuplicator
{
    public class DuplicateMecanimLayerEditorExt : EditorWindow
    {
        bool showDebug_EntryTransitions = false;
        bool showDebug_AnyStateTransitions = false;
        bool showDebug_StateTransitions = false;
        bool showDebug_StateMachineTransitions = false;

        public static string AssetPath { get { return assetPath; } }
        static string assetPath = string.Empty;
        AnimatorControllerLayer src_Layer;
        AnimatorStateMachine src_MainStateMachine;
        AnimatorStateMachine new_MainStateMachine;

        public List<MyTuple<AnimatorStateMachine, AnimatorStateMachine>> all_StateMachines;
        public List<MyTuple<AnimatorState, AnimatorState>> all_States;

        #region GUI
        TwoPaneSplitView splitView;
        ListView leftPanel;
        ListView rightPanel;
        Button refreshBtn;
        Button duplicateBtn;
        TextField searchTxtField;
        Button searchForStrBtn;
        
        List<AnimatorController> allAnimators;
        AnimatorController animCtrl;
        int layerIdx = -1;
        string nameSearch = "";



        [MenuItem("Tools/Mecanim Layer Duplicator")]
        public static void ShowWindow()
        {
            EditorWindow w = GetWindow<DuplicateMecanimLayerEditorExt>();
            w.titleContent = new GUIContent("Duplicate Mecanim Layer");
        }


        void CreateGUI()
        {
            // declaration
            leftPanel = new ListView();
            rightPanel = new ListView();
            splitView = new TwoPaneSplitView(1, 400f, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(new Label("\n\tSelect Animator Controller and a layer to duplicate\n"));

            // buttons declaration
            refreshBtn = new Button(() => RefreshList());
            refreshBtn.text = "REFRESH LIST";
            duplicateBtn = new Button(() => DuplicateSelectedLayer());
            duplicateBtn.text = "DUPLICATE SELECTED LAYER";
            searchTxtField = new TextField("Input State name here: ");
            searchForStrBtn = new Button(() => LookForSpecifiedName());
            searchForStrBtn.text = "Search for State name ^";

            // attaching elements
            splitView.Add(leftPanel);
            splitView.Add(rightPanel);
            rootVisualElement.Add(splitView);
            rootVisualElement.Add(new Label("\n"));
            rootVisualElement.Add(refreshBtn);
            rootVisualElement.Add(duplicateBtn);
            rootVisualElement.Add(new Label("\n"));
            rootVisualElement.Add(searchTxtField);
            rootVisualElement.Add(searchForStrBtn);

            // get all animators
            LoadProjectAnimators();

            // animators list populator/delegate
            leftPanel.makeItem = () => new Label();
            leftPanel.bindItem = (item, index) =>
            {
                if (index < allAnimators.Count)
                    (item as Label).text = allAnimators[index].name;
            };
            leftPanel.itemsSource = allAnimators;
            leftPanel.selectionChanged += OnAnimControllerSelected;

            // animation layers list populator/delegate
            animCtrl = new AnimatorController();
            animCtrl.layers = new AnimatorControllerLayer[0];
            rightPanel.itemsSource = animCtrl.layers;
            rightPanel.makeItem = () => new Label();
            rightPanel.bindItem = (e, i) =>
            {
                if (animCtrl.layers != null && i < animCtrl.layers.Length)
                    (e as Label).text = animCtrl.layers[i].name;
            };
            rightPanel.selectionChanged += OnLayerSelected;
        }

        void LoadProjectAnimators()
        {
            string[] allAnimatorsGUIDS = AssetDatabase.FindAssets("t:AnimatorController");
            allAnimators = new List<AnimatorController>();
            for (int i = 0; i < allAnimatorsGUIDS.Length; i++)
            {
                allAnimators.Add(AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(allAnimatorsGUIDS[i])));
            }
        }

        void LoadLayersToRightPanel()
        {
            if (animCtrl != null && animCtrl.layers.Length > 0)
            {
                rightPanel.itemsSource = animCtrl.layers;
                rightPanel.Rebuild();
            }
        }

        void OnAnimControllerSelected(IEnumerable<object> selectedAnimator)
        {
            layerIdx = -1;
            rightPanel.selectedIndex = layerIdx;

            IEnumerator<object> enumerator = selectedAnimator.GetEnumerator();
            if (enumerator.MoveNext())
            {
                animCtrl = enumerator.Current as AnimatorController;
                LoadLayersToRightPanel();
            }
        }


        void OnLayerSelected(IEnumerable<object> selectedLayer)
        {
            IEnumerator<object> enumerator = selectedLayer.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var layer = enumerator.Current as AnimatorControllerLayer;
                if (layer != null)
                {
                    for (int i = 0; i < animCtrl.layers.Length; i++)
                    {
                        if (animCtrl.layers[i].name == layer.name)
                        {
                            layerIdx = i;
                            return;
                        }
                    }

                    Debug.LogError("Failed to get layer index of layer: " + layer.name);
                }
            }
        }

        void RefreshList()
        {
            LoadProjectAnimators();
            animCtrl = new AnimatorController();
            layerIdx = -1;
            rightPanel.selectedIndex = layerIdx;
            leftPanel.selectedIndex = layerIdx;
            rightPanel.Rebuild();
            leftPanel.Rebuild();
        }

        void DuplicateSelectedLayer()
        {
            if (!EntryValidityCheck()) return;

            // INIT DATA FIELDS
            //Debug.Log("Duplicate layer: INIT");
            src_Layer = animCtrl.layers[layerIdx];
            src_MainStateMachine = src_Layer.stateMachine;
            string newLayerName = src_Layer.name + "_COPY";

            all_StateMachines = new List<MyTuple<AnimatorStateMachine, AnimatorStateMachine>>();
            all_States = new List<MyTuple<AnimatorState, AnimatorState>>();


            // DUPLICATE MAIN STATE MACHINE
            //Debug.Log("DUPLICATE MAIN STATE MACHINE");
            new_MainStateMachine = new AnimatorStateMachine();
            new_MainStateMachine.anyStatePosition = src_MainStateMachine.anyStatePosition;
            new_MainStateMachine.entryPosition = src_MainStateMachine.entryPosition;
            new_MainStateMachine.exitPosition = src_MainStateMachine.exitPosition;
            new_MainStateMachine.hideFlags = src_MainStateMachine.hideFlags;
            new_MainStateMachine.parentStateMachinePosition = src_MainStateMachine.parentStateMachinePosition;
            all_StateMachines.Add(new MyTuple<AnimatorStateMachine, AnimatorStateMachine>(src_MainStateMachine, new_MainStateMachine));

            try
            {
                // DUPLICATE LAYER
                //Debug.Log("DUPLICATE LAYER");
                AnimatorControllerLayer newLayer = new AnimatorControllerLayer();
                newLayer.avatarMask = src_Layer.avatarMask;
                newLayer.blendingMode = src_Layer.blendingMode;
                newLayer.defaultWeight = src_Layer.defaultWeight;
                newLayer.iKPass = src_Layer.iKPass;
                newLayer.syncedLayerAffectsTiming = src_Layer.syncedLayerAffectsTiming;
                newLayer.syncedLayerIndex = src_Layer.syncedLayerIndex;
                newLayer.name = animCtrl.MakeUniqueLayerName(newLayerName);
                newLayer.stateMachine = new_MainStateMachine;
                newLayer.stateMachine.name = newLayerName;
                AssetDatabase.AddObjectToAsset(newLayer.stateMachine, AssetPath);
                //EditorUtility.SetDirty(animCtrl);
                //AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(assetPath));
                animCtrl.AddLayer(newLayer);


                // COPY STATES & MACHINES
                //Debug.Log("COPY STAES & MACHINES");
                CopySource_StateMachines_Recurisvely(src_MainStateMachine, new_MainStateMachine);
                CopySource_DefaultStates();
                CopySource_Behaviours();

                // COPY TRANSITIONS
                //Debug.Log("COPY TRANSITIONS");
                CopySource_StateTransitions();
                CopySource_StateMachineTransitions();
                CopySource_StateMachineToStateMachineTransitions();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }


            // CLEAR DATA
            layerIdx = -1;
            src_Layer = null;
            src_MainStateMachine = null;

            all_StateMachines = null;
            all_States = null;

            GC.Collect();

            LoadLayersToRightPanel();
        }

        void LookForSpecifiedName()
        {
            if (!EntryValidityCheck()) return;

            src_Layer = animCtrl.layers[layerIdx];
            src_MainStateMachine = src_Layer.stateMachine;

            nameSearch = searchTxtField.text;
            Debug.Log("Searching for name: " + nameSearch);

            CheckAllStateMachinesForName_Recursively(src_MainStateMachine);
            Debug.Log("Name search ended");
        }

        bool EntryValidityCheck()
        {
            if (layerIdx == -1)
            {
                Debug.LogError("First, select a layer to reassign clips!");
                return false;
            }

            assetPath = AssetDatabase.GetAssetPath(animCtrl);
            //Debug.Log("assetPath: " + assetPath);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("Animator assetPath is empty!");
                return false;
            }

            return true;
        }
        #endregion



        #region Load temp data for layer duplicator
        void CopySource_StateMachines_Recurisvely(AnimatorStateMachine parentSrcStateMachine, AnimatorStateMachine parentNewStateMachine)
        {
            CopySource_States(parentSrcStateMachine, parentNewStateMachine);

            if (parentSrcStateMachine.stateMachines != null)
            {
                for (int i = 0; i < parentSrcStateMachine.stateMachines.Length; i++)
                {
                    AnimatorStateMachine newStateMachine = parentSrcStateMachine.stateMachines[i].stateMachine.Copy();
                    parentNewStateMachine.AddStateMachine(newStateMachine, parentSrcStateMachine.stateMachines[i].position);

                    var childStateMachinePair = new MyTuple<AnimatorStateMachine, AnimatorStateMachine>(parentSrcStateMachine.stateMachines[i].stateMachine, newStateMachine);
                    all_StateMachines.Add(childStateMachinePair);

                    AssetDatabase.AddObjectToAsset(newStateMachine, AssetPath);

                    CopySource_StateMachines_Recurisvely(parentSrcStateMachine.stateMachines[i].stateMachine, newStateMachine);
                }
            }

            // DEBUG
            if (showDebug_AnyStateTransitions)
            {
                if (parentSrcStateMachine.anyStateTransitions.Length > 0)
                    Debug.LogError("AnyState transitions in " + parentSrcStateMachine.name + ": " + parentSrcStateMachine.anyStateTransitions.Length);

                for (int i = 0; i < parentSrcStateMachine.anyStateTransitions.Length; i++)
                {
                    if (parentSrcStateMachine.anyStateTransitions[i].destinationStateMachine != null)
                        Debug.Log("AnyState stateMachine: " + parentSrcStateMachine.anyStateTransitions[i].destinationStateMachine.name);

                    if (parentSrcStateMachine.anyStateTransitions[i].destinationState != null)
                        Debug.Log("AnyState state: " + parentSrcStateMachine.anyStateTransitions[i].destinationState.name);
                }
            }

            // DEBUG
            if (showDebug_EntryTransitions)
            {
                if (parentSrcStateMachine.entryTransitions.Length > 0)
                    Debug.LogError("Entry transitions in " + parentSrcStateMachine.name + ": " + parentSrcStateMachine.entryTransitions.Length);

                for (int i = 0; i < parentSrcStateMachine.entryTransitions.Length; i++)
                {
                    if (parentSrcStateMachine.entryTransitions[i].destinationStateMachine != null)
                        Debug.Log("Entry stateMachine: " + parentSrcStateMachine.entryTransitions[i].destinationStateMachine.name);

                    if (parentSrcStateMachine.entryTransitions[i].destinationState != null)
                        Debug.Log("Entry state: " + parentSrcStateMachine.entryTransitions[i].destinationState.name);
                }
            }
        }

        void CopySource_States(AnimatorStateMachine parentSrcStateMachine, AnimatorStateMachine parentNewStateMachine)
        {
            //ChildAnimatorState[] newChildStates = new ChildAnimatorState[parentSrcStateMachine.states.Length];
            for (int i = 0; i < parentSrcStateMachine.states.Length; i++)
            {
                AnimatorState newState = parentSrcStateMachine.states[i].state.Copy();
                parentNewStateMachine.AddState(newState, parentSrcStateMachine.states[i].position);

                var childStatePair = new MyTuple<AnimatorState, AnimatorState>(parentSrcStateMachine.states[i].state, newState);
                all_States.Add(childStatePair);

                AssetDatabase.AddObjectToAsset(newState, AssetPath);

                // DEBUG
                if (showDebug_StateTransitions)
                {
                    if (parentSrcStateMachine.states[i].state.transitions.Length > 0)
                        Debug.LogError("Transitions in state " + parentSrcStateMachine.states[i].state.name + ": " + parentSrcStateMachine.states[i].state.transitions.Length);

                    for (int j = 0; j < parentSrcStateMachine.states[i].state.transitions.Length; j++)
                    {
                        if (parentSrcStateMachine.states[i].state.transitions[j].destinationStateMachine != null)
                            Debug.Log("To stateMachine: " + parentSrcStateMachine.states[i].state.transitions[j].destinationStateMachine.name);

                        if (parentSrcStateMachine.states[i].state.transitions[j].destinationState != null)
                            Debug.Log("To state: " + parentSrcStateMachine.states[i].state.transitions[j].destinationState.name);

                        if (parentSrcStateMachine.states[i].state.transitions[j].isExit)
                            Debug.Log("To exit");
                    }
                }
            }
            //parentNewStateMachine.states = newChildStates;
        }

        void CopySource_DefaultStates()
        {
            for (int i = 0; i < all_StateMachines.Count; i++)
            {
                if (all_StateMachines[i].src.defaultState != null)
                {
                    var foundState = all_States.Find(e => e.src == all_StateMachines[i].src.defaultState);
                    if (foundState != null)
                        all_StateMachines[i].copy.defaultState = foundState.copy;
                    else
                        Debug.LogError("Default state not found for: " + all_StateMachines[i].src.name);
                }
            }
        }

        void CopySource_Behaviours()
        {
            for (int i = 0; i < all_StateMachines.Count; i++)
            {
                for (int j = 0; j < all_StateMachines[i].src.behaviours.Length; j++)
                {
                    StateMachineBehaviour newStateMachineBehaviour = all_StateMachines[i].copy.AddStateMachineBehaviour(all_StateMachines[i].src.behaviours[j].GetType());
                    EditorUtility.CopySerialized(all_StateMachines[i].src.behaviours[j], newStateMachineBehaviour);
                    //AssetDatabase.AddObjectToAsset(newStateMachineBehaviour, DuplicateMecanimLayerEditorExt.AssetPath);
                }

                for (int j = 0; j < all_StateMachines[i].src.states.Length; j++)
                {
                    for (int k = 0; k < all_StateMachines[i].src.states[j].state.behaviours.Length; k++)
                    {
                        StateMachineBehaviour newStateBehaviour = all_StateMachines[i].copy.states[j].state.AddStateMachineBehaviour(all_StateMachines[i].src.states[j].state.behaviours[k].GetType());
                        EditorUtility.CopySerialized(all_StateMachines[i].src.states[j].state.behaviours[k], newStateBehaviour);
                        //AssetDatabase.AddObjectToAsset(newStateBehaviour, DuplicateMecanimLayerEditorExt.AssetPath);
                    }
                }
            }
        }

        void CopySource_StateTransitions()
        {
            for (int i = 0; i < all_States.Count; i++)
            {
                for (int j = 0; j < all_States[i].src.transitions.Length; j++)
                {
                    AnimatorStateTransition stateTransition = all_States[i].src.transitions[j].Copy(this);
                    all_States[i].copy.AddTransition(stateTransition);
                    AssetDatabase.AddObjectToAsset(stateTransition, AssetPath);
                }
            }
        }

        void CopySource_StateMachineTransitions()
        {
            for (int i = 0; i < all_StateMachines.Count; i++)
            {
                for (int j = 0; j < all_StateMachines[i].src.entryTransitions.Length; j++)
                {
                    AnimatorTransition entryTransition = all_StateMachines[i].src.entryTransitions[j].Copy(this);
                    AnimatorTransition[] entryTransitions = all_StateMachines[i].copy.entryTransitions;
                    ArrayUtility.Add(ref entryTransitions, entryTransition);
                    all_StateMachines[i].copy.entryTransitions = entryTransitions;
                    //all_StateMachines[i].item2.SetStateMachineTransitions(all_StateMachines[i].item2, entryTransitions);
                    AssetDatabase.AddObjectToAsset(entryTransition, AssetPath);
                }

                for (int j = 0; j < all_StateMachines[i].src.anyStateTransitions.Length; j++)
                {
                    AnimatorStateTransition anyStateTransition = all_StateMachines[i].src.anyStateTransitions[j].Copy(this);
                    AnimatorStateTransition[] anyStateTransitions = all_StateMachines[i].copy.anyStateTransitions;
                    ArrayUtility.Add(ref anyStateTransitions, anyStateTransition);
                    all_StateMachines[i].copy.anyStateTransitions = anyStateTransitions;
                    AssetDatabase.AddObjectToAsset(anyStateTransition, AssetPath);
                }

                // DEBUG
                if (showDebug_EntryTransitions & all_StateMachines[i].src.entryTransitions.Length > 0)
                    Debug.Log("Entry: " + all_StateMachines[i].src.entryTransitions.Length);

                if (showDebug_AnyStateTransitions & all_StateMachines[i].src.anyStateTransitions.Length > 0)
                    Debug.Log("AnyState: " + all_StateMachines[i].src.anyStateTransitions.Length);
            }
        }

        void CopySource_StateMachineToStateMachineTransitions()
        {
            AnimatorStateMachine srcFromMachine;
            AnimatorStateMachine srcToMachine;
            AnimatorStateMachine newFromMachine;
            AnimatorStateMachine newToMachine;
            for (int i = 0; i < all_StateMachines.Count; i++)
            {
                srcFromMachine = all_StateMachines[i].src;
                newFromMachine = all_StateMachines[i].copy;
                for (int j = 0; j < all_StateMachines.Count; j++)
                {
                    srcToMachine = all_StateMachines[j].src;
                    newToMachine = all_StateMachines[j].copy;

                    AnimatorTransition[] srcTransitions = srcFromMachine.GetStateMachineTransitions(srcToMachine);
                    if (srcTransitions.Length == 0)
                        continue;
                    else if (showDebug_StateMachineTransitions)
                        Debug.LogError("StateMachine transitions in " + srcFromMachine.name + ": " + srcTransitions.Length);

                    AnimatorTransition[] newTransitions = new AnimatorTransition[0];
                    for (int k = 0; k < srcTransitions.Length; k++)
                    {
                        AnimatorTransition transition = srcTransitions[k].Copy(this);
                        ArrayUtility.Add(ref newTransitions, transition);
                        AssetDatabase.AddObjectToAsset(transition, AssetPath);
                    }
                    newFromMachine.SetStateMachineTransitions(newToMachine, newTransitions);

                    // DEBUG
                    if (showDebug_StateMachineTransitions)
                    {
                        for (int k = 0; k < newTransitions.Length; i++)
                        {
                            if (newTransitions[k].destinationStateMachine != null)
                                Debug.Log("All to stateMachine: " + newTransitions[k].destinationStateMachine.name);

                            if (newTransitions[k].destinationState != null)
                                Debug.Log("All to state: " + newTransitions[k].destinationState.name);
                        }
                    }
                }
            }
        }
        #endregion

        #region Search for name
        void CheckAllStateMachinesForName_Recursively(AnimatorStateMachine givenStateMachine)
        {
            for (int i = 0; i < givenStateMachine.states.Length; i++)
            {
                if (CheckForName(givenStateMachine.states[i].state.name))
                    Debug.Log("State name found in: " + givenStateMachine.name + "\n" + givenStateMachine.states[i].state.name);

                for (int j = 0; j < givenStateMachine.anyStateTransitions.Length; j++)
                {
                    if (CheckForName(givenStateMachine.anyStateTransitions[j].name))
                        Debug.Log("Any state transition name found in: " + givenStateMachine.name + "\n" + givenStateMachine.anyStateTransitions[j].name);
                }

                for (int j = 0; j < givenStateMachine.entryTransitions.Length; j++)
                {
                    if (CheckForName(givenStateMachine.entryTransitions[j].name))
                        Debug.Log("Entry state transition found in: " + givenStateMachine.name + "\n" + givenStateMachine.entryTransitions[j].name);
                }

                for (int j = 0; j < givenStateMachine.states[i].state.transitions.Length; j++)
                {
                    if (CheckForName(givenStateMachine.states[i].state.transitions[j].name))
                        Debug.Log("Transition name found in: " + givenStateMachine.states[i].state.name + "\n" + givenStateMachine.states[i].state.transitions[j].name);
                }
            }

            if (givenStateMachine.stateMachines != null)
            {
                for (int i = 0; i < givenStateMachine.stateMachines.Length; i++)
                {
                    CheckAllStateMachinesForName_Recursively(givenStateMachine.stateMachines[i].stateMachine);
                }
            }
        }

        bool CheckForName(string givenName)
        {
            return (!string.IsNullOrEmpty(givenName) && givenName.ContainsInvariantCultureIgnoreCase(nameSearch));
        }
        #endregion
    }

    public static class AnimatorExt
    {
        public static AnimatorState Copy(this AnimatorState src)
        {
            AnimatorState newState = new AnimatorState();
            newState.cycleOffset = src.cycleOffset;
            newState.cycleOffsetParameter = src.cycleOffsetParameter;
            newState.cycleOffsetParameterActive = src.cycleOffsetParameterActive;
            newState.hideFlags = src.hideFlags;
            newState.iKOnFeet = src.iKOnFeet;
            newState.mirror = src.mirror;
            newState.mirrorParameter = src.mirrorParameter;
            newState.mirrorParameterActive = src.mirrorParameterActive;
            newState.name = src.name;
            newState.speed = src.speed;
            newState.speedParameter = src.speedParameter;
            newState.speedParameterActive = src.speedParameterActive;
            newState.tag = src.tag;
            newState.timeParameter = src.timeParameter;
            newState.timeParameterActive = src.timeParameterActive;
            newState.writeDefaultValues = src.writeDefaultValues;

            if (src.motion != null)
            {
                if (src.motion.GetType() == typeof(BlendTree))
                {
                    // blend tree, always a copy
                    BlendTree srcBlendTree = (BlendTree)src.motion;
                    BlendTree newBlendTree = new BlendTree();
                    EditorUtility.CopySerialized(srcBlendTree, newBlendTree);
                    newBlendTree.hideFlags = src.hideFlags;
                    newState.motion = newBlendTree;
                    AssetDatabase.AddObjectToAsset(newBlendTree, DuplicateMecanimLayerEditorExt.AssetPath);
                }
                else if (src.motion.GetType() == typeof(AnimationClip))
                {
                    // animation clip
                    AnimationClip srcClip = (AnimationClip)src.motion;
                    string motionAssetPath = AssetDatabase.GetAssetPath(src.motion);
                    if (string.IsNullOrEmpty(motionAssetPath) | motionAssetPath == DuplicateMecanimLayerEditorExt.AssetPath)
                    {
                        // motion included directly into AnimatorController and needs a copy
                        AnimationClip newClip = new AnimationClip();
                        EditorUtility.CopySerialized(srcClip, newClip);
                        newState.motion = newClip;
                        AssetDatabase.AddObjectToAsset(newClip, DuplicateMecanimLayerEditorExt.AssetPath);
                    }
                    else
                    {
                        // motion ref from assets
                        newState.motion = srcClip;
                    }
                }
                else
                {
                    // other motion type?
                    Debug.LogError("Unknown motion type in state: " + src.name);
                    newState.motion = src.motion;
                }
            }

            // Updated later
            newState.transitions = new AnimatorStateTransition[0];
            newState.behaviours = new StateMachineBehaviour[0];

            return newState;
        }

        public static AnimatorStateMachine Copy(this AnimatorStateMachine src)
        {
            AnimatorStateMachine newStateMachine = new AnimatorStateMachine();
            newStateMachine.anyStatePosition = src.anyStatePosition;
            newStateMachine.entryPosition = src.entryPosition;
            newStateMachine.exitPosition = src.exitPosition;
            newStateMachine.hideFlags = src.hideFlags;
            newStateMachine.name = src.name;
            newStateMachine.parentStateMachinePosition = src.parentStateMachinePosition;

            // Updated later
            newStateMachine.states = new ChildAnimatorState[0];
            newStateMachine.stateMachines = new ChildAnimatorStateMachine[0];
            newStateMachine.entryTransitions = new AnimatorTransition[0];
            newStateMachine.anyStateTransitions = new AnimatorStateTransition[0];
            newStateMachine.behaviours = new StateMachineBehaviour[0];
            // newStateMachine.defaultState

            return newStateMachine;
        }

        public static AnimatorCondition Copy(this AnimatorCondition src)
        {
            AnimatorCondition newCondition = new AnimatorCondition();
            newCondition.mode = src.mode;
            newCondition.parameter = src.parameter;
            newCondition.threshold = src.threshold;
            return newCondition;
        }

        public static AnimatorStateTransition Copy(this AnimatorStateTransition src, DuplicateMecanimLayerEditorExt duplicatorInst)
        {
            AnimatorStateTransition newStateTransition = new AnimatorStateTransition();

            AnimatorCondition[] newConditions = new AnimatorCondition[src.conditions.Length];
            for (int i = 0; i < src.conditions.Length; i++)
            {
                newConditions[i] = src.conditions[i].Copy();
            }
            newStateTransition.conditions = newConditions;

            newStateTransition.canTransitionToSelf = src.canTransitionToSelf;
            newStateTransition.duration = src.duration;
            newStateTransition.exitTime = src.exitTime;
            newStateTransition.hasExitTime = src.hasExitTime;
            newStateTransition.hasFixedDuration = src.hasFixedDuration;
            newStateTransition.hideFlags = src.hideFlags;
            newStateTransition.interruptionSource = src.interruptionSource;
            newStateTransition.isExit = src.isExit;
            newStateTransition.mute = src.mute;
            newStateTransition.name = src.name;
            newStateTransition.offset = src.offset;
            newStateTransition.orderedInterruption = src.orderedInterruption;
            newStateTransition.solo = src.solo;

            bool stateNotFound = false;
            bool machineNotFound = false;
            if (src.destinationState != null)
            {
                var statePairFound = duplicatorInst.all_States.Find(e => e.src == src.destinationState);
                if (statePairFound != null)
                    newStateTransition.destinationState = statePairFound.copy;
                else
                    stateNotFound = true;
            }

            if (src.destinationStateMachine != null)
            {
                var machinePairFound = duplicatorInst.all_StateMachines.Find(e => e.src == src.destinationStateMachine);
                if (machinePairFound != null)
                    newStateTransition.destinationStateMachine = machinePairFound.copy;
                else
                    machineNotFound = true;
            }

            if (machineNotFound & stateNotFound & !src.isExit)
                Debug.LogError("No destination found");

            return newStateTransition;
        }

        public static AnimatorTransition Copy(this AnimatorTransition src, DuplicateMecanimLayerEditorExt duplicatorInst)
        {
            AnimatorTransition newTransition = new AnimatorTransition();

            AnimatorCondition[] newConditions = new AnimatorCondition[src.conditions.Length];
            for (int i = 0; i < src.conditions.Length; i++)
            {
                newConditions[i] = src.conditions[i].Copy();
            }
            newTransition.conditions = newConditions;

            newTransition.hideFlags = src.hideFlags;
            newTransition.isExit = src.isExit;
            newTransition.mute = src.mute;
            newTransition.name = src.name;
            newTransition.solo = src.solo;

            bool stateNotFound = false;
            bool machineNotFound = false;
            if (src.destinationState != null)
            {
                var statePairFound = duplicatorInst.all_States.Find(e => e.src == src.destinationState);
                if (statePairFound != null)
                    newTransition.destinationState = statePairFound.copy;
                else
                    stateNotFound = true;
            }

            if (src.destinationStateMachine != null)
            {
                var machinePairFound = duplicatorInst.all_StateMachines.Find(e => e.src == src.destinationStateMachine);
                if (machinePairFound != null)
                    newTransition.destinationStateMachine = machinePairFound.copy;
                else
                    machineNotFound = true;

            }

            if (machineNotFound & stateNotFound & !src.isExit)
                Debug.LogError("No destination found");

            return newTransition;
        }
    }

    public class MyTuple<T1, T2>
    {
        public T1 src;
        public T2 copy;

        public MyTuple(T1 givenItem1, T2 givenItem2)
        {
            src = givenItem1;
            copy = givenItem2;
        }
    }
}