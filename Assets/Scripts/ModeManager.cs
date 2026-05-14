using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Meta.XR.MRUtilityKit;

public class ModeManager : MonoBehaviour
{
    [Header("Menus")]
    public GameObject settingsMenu;
    public Transform centerEyeAnchor; // drag CameraRig/TrackingSpace/CenterEyeAnchor here
    public Transform cameraRig; // drag your Camera Rig here

    [Header("Mode Objects")]
    public GameObject virtualCourt;
    public GameObject roomMode; // parent of RoomColliders

    [Header("Passthrough")]
    public OVRPassthroughLayer passthroughLayer;

    [Header("Fade")]
    public Image fadeImage; // fullscreen black UI image for fade effect

    private bool isVirtualMode = false;
    private bool settingsOpen = false;

    void Start()
    {
        // Set initial states
        settingsMenu.SetActive(false);
        virtualCourt.SetActive(false);
        roomMode.SetActive(false);

        // Show the settings menu as the primary interface on start
        StartCoroutine(ShowSettingsMenuWhenReady());
    }

    void Update()
    {
        // Hamburger/menu button on left controller opens/closes settings
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch))
        {
            ToggleSettingsMenu();
        }

        if (settingsMenu.activeSelf)
        {
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                Debug.Log("Virtual Court selected from settings");
                SelectVirtualCourt();
            }
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
            {
                Debug.Log("Real Room selected from settings");
                SelectRealRoom();
            }
        }
    }

    IEnumerator ShowSettingsMenuWhenReady()
    {
        // Wait for OVR tracking to initialize (prevents menu spawning at floor/wrong orientation)
        yield return new WaitForSeconds(2f);
        
        settingsOpen = true;
        settingsMenu.SetActive(true);
        PositionMenuInFrontOfUser(settingsMenu);
    }

    void PositionMenuInFrontOfUser(GameObject menu)
    {
        Vector3 forward = centerEyeAnchor.forward;
        forward.y = 0;
        forward.Normalize();

        menu.transform.position = centerEyeAnchor.position + forward * 1.5f + Vector3.up * 0.1f;
        menu.transform.rotation = Quaternion.LookRotation(forward);
    }

    public void SelectRealRoom()
    {
        StartCoroutine(SwitchMode(false));
    }

    public void SelectVirtualCourt()
    {
        StartCoroutine(SwitchMode(true));
    }

    public void ToggleSettingsMenu()
    {
        settingsOpen = !settingsOpen;
        settingsMenu.SetActive(settingsOpen);
        
        if (settingsOpen)
        {
            PositionMenuInFrontOfUser(settingsMenu);
        }
    }

    IEnumerator SwitchMode(bool toVirtual)
    {
        settingsOpen = false;
        settingsMenu.SetActive(false);

        yield return StartCoroutine(Fade(0f, 1f, 0.5f));

        isVirtualMode = toVirtual;

        if (toVirtual)
        {
            passthroughLayer.enabled = false;
            virtualCourt.SetActive(true);
            roomMode.SetActive(false);
            DisableRoomColliders();

            // Move the entire court to sit at the player's current floor level
            // instead of moving the player to the court
            float floorY = cameraRig.position.y;
            virtualCourt.transform.position = new Vector3(0f, floorY, 0f);

            // Now center the player horizontally over the court baseline
            Vector3 targetHeadPosition = new Vector3(
                centerEyeAnchor.position.x,
                centerEyeAnchor.position.y,
                centerEyeAnchor.position.z
            );
            Vector3 offset = targetHeadPosition - centerEyeAnchor.position;
            cameraRig.position += offset;
        }
        else
        {
            passthroughLayer.enabled = true;
            virtualCourt.SetActive(false);
            roomMode.SetActive(true);

            // Re-enable MRUK-generated colliders
            EnableRoomColliders();
        }

        yield return StartCoroutine(Fade(1f, 0f, 0.5f));
    }

    void DisableRoomColliders()
    {
        // Find all colliders MRUK spawned on room anchors and disable them
        if (MRUK.Instance == null) return;
        foreach (var room in MRUK.Instance.Rooms)
        {
            foreach (var anchor in room.Anchors)
            {
                foreach (var col in anchor.GetComponentsInChildren<Collider>())
                {
                    col.enabled = false;
                }
            }
        }
    }

    void EnableRoomColliders()
    {
        if (MRUK.Instance == null) return;
        foreach (var room in MRUK.Instance.Rooms)
        {
            foreach (var anchor in room.Anchors)
            {
                foreach (var col in anchor.GetComponentsInChildren<Collider>())
                {
                    col.enabled = true;
                }
            }
        }
    }

    IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = toAlpha;
        fadeImage.color = c;
    }
}