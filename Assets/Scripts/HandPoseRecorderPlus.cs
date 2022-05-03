using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandPosing;
using Oculus.Interaction.HandPosing.Visuals;
using Oculus.Interaction.Input;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public class HandPoseRecorderPlus : MonoBehaviour
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

    [SerializeField]
    private GrabTypeFlags _grabTypeFlags = GrabTypeFlags.All;

    [SerializeField, Optional]
    [Tooltip("Collection for storing generated HandGrabInteractables during Play-Mode, so they can be restored in Edit-Mode")]
    private HandGrabInteractableDataCollection _posesCollection = null;

    [Header("1a - Go to Play mode and record your poses one time")]
    [SerializeField]
    private KeyCode _recordKey = KeyCode.Space;

    [Header("1b - Go to Play mode and record your poses on a timer")]
    [SerializeField]
    private KeyCode _recordToggleFrequencyKey = KeyCode.R;

    [SerializeField]
    private int _recordFrequency = 2;

    [SerializeField]
    private TextMeshProUGUI _recordTimerText;

    private float _recordFrequencyTimer;
    private bool _recordFrequencyStarted = false;

    [Header("2- Store your poses before exiting Play mode: ")]
        
    [InspectorButton("SaveToAsset")]
    [SerializeField]
    private string _storePoses;

    [Header("3-Now load the poses in Edit mode to tweak and persist them: ")]
    [InspectorButton("LoadFromAsset")]
    [SerializeField]
    private string _loadPoses;

    private void Awake()
    {
        _recordTimerText.text = _recordFrequency.ToString("0");

        if (_ghostProvider == null)
        {
            HandGhostProvider.TryGetDefault(out _ghostProvider);
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(_recordKey))
        {
            RecordPose();
        }

        if (Input.GetKeyDown(_recordToggleFrequencyKey))
        {
            _recordFrequencyStarted = true;
            _recordFrequencyTimer = _recordFrequency;
            Logger.Instance.LogInfo($"Recording frequency initiated");
        }

        if (_recordFrequencyStarted)
        {
            if (_recordFrequencyTimer > 0)
            {
                _recordFrequencyTimer -= Time.deltaTime;
                _recordTimerText.text = $"{Mathf.CeilToInt(_recordFrequencyTimer)}";
            }
            else
            {
                Logger.Instance.LogInfo($"Recording hand pose for game object {_recordable.name}");
                
                RecordPose();

                // reset frequency values
                _recordFrequencyStarted = false;
                _recordFrequencyTimer = _recordFrequency;
            }
        }
    }


    public void RecordPose()
    {
        Logger.Instance.LogInfo($"Recording hand pose for game object {_recordable.name}");

        if (_handGrabInteractor == null
            || _handGrabInteractor.Hand == null)
        {
            Logger.Instance.LogError("Missing HandGrabInteractor. Ensure you are in PLAY mode!");
            return;
        }

        if (_recordable == null)
        {
            Logger.Instance.LogError("Missing Recordable");
            return;
        }

        HandPose trackedHandPose = TrackedPose();
        if (trackedHandPose == null)
        {
            Logger.Instance.LogError("Tracked Pose could not be retrieved");
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
        HandGrabInteractable interactable = HandGrabInteractable
            .Create(_recordable.transform, _recordable.GetComponent<Rigidbody>(),
            _recordable.GetComponent<Grabbable>(), _grabTypeFlags);

        HandGrabPointData pointData = new HandGrabPointData()
        {
            handPose = rawPose,
            scale = 1f,
            gripPose = snapPoint,
        };
        return interactable.LoadPoint(pointData);
    }


    private HandGrabInteractable LoadHandGrabInteractable(Oculus.Interaction.HandPosing.HandGrabInteractableData data)
    {
        HandGrabInteractable interactable = HandGrabInteractable
            .Create(_recordable.transform, _recordable.GetComponent<Rigidbody>(),
            _recordable.GetComponent<Grabbable>(), _grabTypeFlags);

        interactable.LoadData(data);
        return interactable;
    }

    private void LoadFromAsset()
    {
        if (_posesCollection != null)
        {
            foreach (Oculus.Interaction.HandPosing.HandGrabInteractableData handPose in _posesCollection.InteractablesData)
            {
                LoadHandGrabInteractable(handPose);
            }
        }
    }

    private void SaveToAsset()
    {
        List<Oculus.Interaction.HandPosing.HandGrabInteractableData> savedPoses = new List<Oculus.Interaction.HandPosing.HandGrabInteractableData>();
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
