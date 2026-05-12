using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ModeManager : MonoBehaviour
{
    [Header("Menus")]
    public GameObject launchMenu;
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
        launchMenu.SetActive(true);
        settingsMenu.SetActive(false);
        virtualCourt.SetActive(false);
        roomMode.SetActive(false);

        StartCoroutine(ShowLaunchMenuWhenReady());
    }

    void Update()
    {
        // Hamburger/menu button on left controller opens/closes settings
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            if (launchMenu.activeSelf) return; // don't open settings during launch menu
            ToggleSettingsMenu();
        }
    }

    IEnumerator ShowLaunchMenuWhenReady()
    {
        // Wait for OVR tracking to initialize
        yield return new WaitForSeconds(2f);
        PositionMenuInFrontOfUser(launchMenu);
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
            PositionMenuInFrontOfUser(settingsMenu);
    }

    IEnumerator SwitchMode(bool toVirtual)
    {
        // Close menus
        launchMenu.SetActive(false);
        settingsMenu.SetActive(false);

        // Fade to black
        yield return StartCoroutine(Fade(0f, 1f, 0.5f));

        isVirtualMode = toVirtual;

        if (toVirtual)
        {
            // Disable passthrough, enable virtual court
            passthroughLayer.enabled = false;
            virtualCourt.SetActive(true);
            roomMode.SetActive(false);

            // Position user at center of court
            cameraRig.position = new Vector3(0, 0, 0);
        }
        else
        {
            // Enable passthrough, disable virtual court
            passthroughLayer.enabled = true;
            virtualCourt.SetActive(false);
            roomMode.SetActive(true);
        }

        // Fade back in
        yield return StartCoroutine(Fade(1f, 0f, 0.5f));
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