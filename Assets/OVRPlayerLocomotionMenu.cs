using Oculus.Interaction;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(OVRPlayerControllerWithHandPoses))]
public class OVRPlayerLocomotionMenu : MonoBehaviour
{
    [SerializeField]
    private OVRCameraRig cameraRig;

    [SerializeField]
    private GameObject targetHand;

    [SerializeField]
    private GameObject locomotionMenu;

    [SerializeField]
    private Vector3 offsetFromHand = new Vector3(0.5f, 0.5f, 0.5f);

    private OVRPlayerControllerWithHandPoses playerControllerWithHandPoses;

    // menuState
    [SerializeField]
    private InteractableUnityEventWrapper onLocomotionAction;
    private bool locomotionOn = true;

    private void Awake()
    {
        playerControllerWithHandPoses = GetComponent<OVRPlayerControllerWithHandPoses>();
        
        onLocomotionAction.WhenSelect.AddListener(() =>
        {
            locomotionOn = !locomotionOn;
            var locomotionMenuOption = onLocomotionAction.GetComponentInChildren<TextMeshPro>();
            var locomotionMenuState = locomotionOn ? "ON" : "OFF";
            locomotionMenuOption.text = $"LOCOMOTION {locomotionMenuState}";
            playerControllerWithHandPoses.EnableLinearMovement = locomotionOn;

            Logger.Instance.LogInfo($"Locomotion state changed to: {locomotionMenuState}");
        });
    }

    public void LocomotionVisibility(bool state)
    {
        locomotionMenu.SetActive(state);
    }

    private void Update()
    {
        locomotionMenu.transform.position = targetHand.transform.position + offsetFromHand;
        locomotionMenu.transform.rotation = Quaternion.LookRotation(locomotionMenu.transform.position - cameraRig.centerEyeAnchor.transform.position, Vector3.up);
    }

}
    