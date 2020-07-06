using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.Events;

public class TabbedDialog : MonoBehaviour
{
    public GameObject[] Panels;
    public InteractableToggleCollection tabs;

    private UnityAction _selectionChanged;

    public RadioSelectionChanged SelectedChanged = new RadioSelectionChanged();

    private void OnSelectedChanged()
    {
        // Retrieve the current Index here...
        var newId = SyncPanelToCurrentIndex();
        if (!string.IsNullOrEmpty(newId))
            SelectedChanged.Invoke(newId);
    }

    private void OnEnable()
    {
        if (_selectionChanged == null)
            _selectionChanged = OnSelectedChanged;

        tabs.OnSelectionEvents.AddListener(_selectionChanged);
        SyncPanelToCurrentIndex();
    }

    string SyncPanelToCurrentIndex()
    {
        int idx = tabs.CurrentIndex;
        if (idx < 0 && idx >= Panels.Length)
            return string.Empty;

        foreach (var panel in Panels)
        {
            panel.SetActive(false);
        }

        Panels[idx].SetActive(true);
        return Panels[idx].GetComponent<ProviderId>().Id;
    }
    private void OnDisable()
    {
        tabs.OnSelectionEvents.RemoveListener(_selectionChanged);
    }

    // Start is called before the first frame update
    void Start()
    {
        _selectionChanged = new UnityAction(OnSelectedChanged);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
