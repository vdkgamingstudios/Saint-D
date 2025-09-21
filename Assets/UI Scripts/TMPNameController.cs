using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UnistrokeGestureRecognition.Example
{
    public sealed class TMPNameController : MonoBehaviour
    {
        private static readonly Color _transparent = new(1, 1, 1, 0);

        [SerializeField] private TextMeshProUGUI _text;

        private void Awake()
        {
            Clear();
        }

        public void Clear()
        {
            StopAllCoroutines();

            _text.text = "";
            _text.color = _transparent;
        }

        public void Set(string name)
        {
            _text.text = name;
            StartCoroutine(ShowNameAndHide());
        }

        private IEnumerator ShowNameAndHide()
        {
            const float speed = 0.3f;
            float t = 0;

            while (t < 1f)
            {
                _text.color = Color.Lerp(_transparent, Color.white, t);
                t += Time.deltaTime * speed;
                yield return null;
            }
        }
    }
}
