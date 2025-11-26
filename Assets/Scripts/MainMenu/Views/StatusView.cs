using TMPro;
using UnityEngine;

public class StatusView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    public void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[MainMenuUI] {message}");
    }
}
