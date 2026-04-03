using System.Collections;
using TMPro;
using UnityEngine;

namespace MonsterWorldLike.UI
{
    public class ToastNotifier : MonoBehaviour
    {
        [SerializeField] private TMP_Text toastText;
        [SerializeField] private float duration = 2f;

        private Coroutine active;

        public void Show(string message)
        {
            if (toastText == null)
            {
                return;
            }

            if (active != null)
            {
                StopCoroutine(active);
            }

            active = StartCoroutine(ShowRoutine(message));
        }

        private IEnumerator ShowRoutine(string message)
        {
            toastText.gameObject.SetActive(true);
            toastText.text = message;
            yield return new WaitForSecondsRealtime(duration);
            toastText.gameObject.SetActive(false);
            active = null;
        }
    }
}
