using UnityEngine;

public class HandManager : MonoBehaviour
{
    public GameObject paddle;
    public Transform leftControllerAnchor;
    public Transform rightControllerAnchor;
    public bool isRightHand { get; private set; } = true;

    void Start()
    {
        SetHand(true); // Default to right hand
    }

    void Update()
    {
        // Left trigger → move paddle to left hand
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
            SetHand(false);

        // Right trigger → move paddle to right hand
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            SetHand(true);
    }

    public void SetHand(bool rightHand)
    {
        isRightHand = rightHand;
        paddle.transform.SetParent(rightHand ? rightControllerAnchor : leftControllerAnchor, false);
        paddle.transform.localPosition = new Vector3(-0.55f, -0.05f, -0.1f);
        paddle.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        paddle.transform.localScale = new Vector3(0.025f, 0.01f, 0.01f);
    }
}