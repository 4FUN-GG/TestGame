using UnityEngine;
using UnityEngine.UI;

public class ExitButton : MonoBehaviour
{
    public GameObject areYouSureGO;

    public void ShowConfirmScreen()
    {
        if (areYouSureGO != null)
        {
            areYouSureGO.SetActive(!areYouSureGO.activeInHierarchy);
        }
    }

    public void HideConfirmScreen()
    {
        if (areYouSureGO != null)
        {
            areYouSureGO.SetActive(false);
        }
    }

    public void Exit()
    {
        Application.Quit();
    }
}
