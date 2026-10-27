using UnityEngine;

// Select an enum in the Inspector, then connect PlaySfx to a UnityEvent
// (Button.onClick, animation event, etc.).
public sealed class SoundPlayer : MonoBehaviour
{
    [SerializeField] private ESfx sfx = ESfx.UI_Click;

    public void PlaySfx()
    {
        if (SoundManager.HasInstance)
            SoundManager.Instance.PlaySfx(sfx);
    }
}
