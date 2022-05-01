using Oculus.Interaction;
using Oculus.Interaction.HandPosing;
using Oculus.Interaction.HandPosing.Visuals;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class HandPoseRecorderPlus : ScriptableWizard
{   
    [Header("Reference interactor used for Hand Grab:")]
    [SerializeField]
    private HandGrabInteractor _handGrabInteractor;
    [Header("Root of the object to record the pose to:")]
    [SerializeField]
    private GameObject _recordable;

    [SerializeField, Optional]
    [Tooltip("Prototypes of the static hands (ghosts) that visualize holding poses")]
    private HandGhostProvider _ghostProvider;

    [SerializeField, Optional]
    [Tooltip("Collection for storing generated HandGrabInteractables during Play-Mode, so they can be restored in Edit-Mode")]
    private HandGrabInteractableDataCollection _posesCollection = null;

    [Header("1- Go to Play mode and record your poses: ")]
    [SerializeField]
    private KeyCode _recordKey = KeyCode.Space;
    [InspectorButton("RecordPose", 80)]
    [SerializeField]
    private string _recordPose;

    [Header("2- Store your poses before exiting Play mode: ")]
        
    [InspectorButton("SaveToAsset")]
    [SerializeField]
    private string _storePoses;

    [Header("3-Now load the poses in Edit mode to tweak and persist them: ")]
    [InspectorButton("LoadFromAsset")]
    [SerializeField]
    private string _loadPoses;

    [MenuItem("OculusUtils/Interaction/Hand Pose Recorder")]
    private static void CreateWizard()
    {
        HandPoseRecorderPlus wizard = DisplayWizard<HandPoseRecorderPlus>("Hand Pose Recorder", "Close");
        wizard.helpString = "Auto-Generate HandGrabInteractables with HandPoses in PlayMode, then Store and retrieve them in EditMode to persist and tweak them.";
    }

    private void Awake()
    {
        if (_ghostProvider == null)
        {
            HandGhostProvider.TryGetDefault(out _ghostProvider);
        }
    }

    protected override bool DrawWizardGUI()
    {
        if (_handGrabInteractor == null)
        {
            //TODO unity might lose this reference (But still present it in the inspector) 
            //during Play-Edit mode changes
            _handGrabInteractor = null;
        }

        Event e = Event.current;
        if (e.type == EventType.KeyDown
            && e.keyCode == _recordKey)
        {
            RecordPose();
        }

        return base.DrawWizardGUI();
    }

    public void RecordPose()
    {
        if (_handGrabInteractor == null
            || _handGrabInteractor.Hand == null)
        {
            Debug.LogError("Missing HandGrabInteractor. Ensure you are in PLAY mode!", this);
            return;
        }

        if (_recordable == null)
        {
            Debug.LogError("Missing Recordable", this);
            return;
        }

        HandPose trackedHandPose = TrackedPose();
        if (trackedHandPose == null)
        {
            Debug.LogError("Tracked Pose could not be retrieved", this);
            return;
        }
        Pose gripPoint = _recordable.transform.RelativeOffset(_handGrabInteractor.transform);
        HandGrabPoint point = AddHandGrabPoint(trackedHandPose, gripPoint);
        AttachGhost(point);
    }

    private HandPose TrackedPose()
    {
        if (!_handGrabInteractor.Hand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
        {
            return null;
        }
        HandPose result = new HandPose(_handGrabInteractor.Hand.Handedness);
        for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; ++i)
        {
            HandJointId jointID = FingersMetadata.HAND_JOINT_IDS[i];
            result.JointRotations[i] = localJoints[jointID].rotation;
        }
        return result;
    }

    private void AttachGhost(HandGrabPoint point)
    {
        if (_ghostProvider != null)
        {
            HandGhost ghostPrefab = _ghostProvider.GetHand(_handGrabInteractor.Hand.Handedness);
            HandGhost ghost = GameObject.Instantiate(ghostPrefab, point.transform);
            ghost.SetPose(point);
        }
    }

    public HandGrabPoint AddHandGrabPoint(HandPose rawPose, Pose snapPoint)
    {
        HandGrabInteractable interactable = HandGrabInteractable.Create(_recordable.transform);
        HandGrabPointData pointData = new HandGrabPointData()
        {
            handPose = rawPose,
            scale = 1f,
            gripPose = snapPoint,
        };
        return interactable.LoadPoint(pointData);
    }


    private HandGrabInteractable LoadHandGrabInteractable(HandGrabInteractableData data)
    {
        HandGrabInteractable interactable = HandGrabInteractable.Create(_recordable.transform);
        interactable.LoadData(data);
        return interactable;
    }

    private void LoadFromAsset()
    {
        if (_posesCollection != null)
        {
            foreach (HandGrabInteractableData handPose in _posesCollection.InteractablesData)
            {
                LoadHandGrabInteractable(handPose);
            }
        }
    }

    private void SaveToAsset()
    {
        List<HandGrabInteractableData> savedPoses = new List<HandGrabInteractableData>();
        foreach (HandGrabInteractable snap in _recordable.GetComponentsInChildren<HandGrabInteractable>(false))
        {
            savedPoses.Add(snap.SaveData());
        }
        if (_posesCollection == null)
        {
            GenerateCollectionAsset();
        }
        _posesCollection?.StoreInteractables(savedPoses);
    }

    private void GenerateCollectionAsset()
    {
        _posesCollection = ScriptableObject.CreateInstance<HandGrabInteractableDataCollection>();
        string parentDir = Path.Combine("Assets", "HandGrabInteractableDataCollection");
        if (!Directory.Exists(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }
        string name = _recordable != null ? _recordable.name : "Auto";
        AssetDatabase.CreateAsset(_posesCollection, Path.Combine(parentDir, $"{name}_HandGrabCollection.asset"));
        AssetDatabase.SaveAssets();
    }
}
