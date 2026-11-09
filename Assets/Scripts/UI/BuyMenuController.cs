using UnityEngine;

public class BuyMenuController : MonoBehaviour
{
    public void Close()
    {
        if (GameplayUIManager.Instance != null)
        {
            GameplayUIManager.Instance.CloseShop();
        }
    }
}