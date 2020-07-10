using UnityEngine;

public class ObjectSwitcher : MonoBehaviour
{
    public enum ObjectSwitcherStatus
    {
        None,
        Progress,
        Success,
        Fail,
    };

    public GameObject None;
    public GameObject Progress;
    public GameObject Success;
    public GameObject Fail;

    // Start is called before the first frame update
    void Start()
    {
        None.SetActive(true);
        Progress.SetActive(false);
        Success.SetActive(false);
        Fail.SetActive(false);
    }

    public void SetStatus(ObjectSwitcherStatus status)
    {
        None.SetActive(false);
        Progress.SetActive(false);
        Success.SetActive(false);
        Fail.SetActive(false);

        switch (status)
        {
            case ObjectSwitcherStatus.None:
                None.SetActive(true);
                return;
            case ObjectSwitcherStatus.Progress:
                Progress.SetActive(true);
                return;
            case ObjectSwitcherStatus.Success:
                Success.SetActive(true);
                return;
            case ObjectSwitcherStatus.Fail:
                Fail.SetActive(true);
                return;
        }
    }
}
