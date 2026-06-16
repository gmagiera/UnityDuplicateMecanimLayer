# UnityDuplicateMecanimLayer
Editor tool for Unity to quickly duplicate Mecanim Animator layers with all transitions and states intact.

It not only copies scripts (StateMachine Behaviours) instances attached to states, but also creates a copy of each Blend Tree that layer uses instead of only copying a reference to them.


# How to use
Download DuplicateMecanimLayerEditorExt.cs C# code file and put it in the Editor directory, somewhere in your project. Allow Unity to recompile your solution.

In the menu bar of Unity Editor select Tools > Mecanim Layer Duplicator. You get a pop-up window with all AnimatorControllers listed on the left.

Select an AnimatorController that has the layer you want to duplicate. All of its layers should appear on the right.

Select layer you want to duplicate on the right and press the duplicate button. Unity may become unresponsive for a few seconds (it depends on the complexion of selected layer).

A duplicated layer will appear as the bottom of the layer list with "_COPY" suffix in the name.

DONE
