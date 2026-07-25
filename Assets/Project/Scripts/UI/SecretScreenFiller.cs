using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SecretScreenFiller : MonoBehaviour
    {
        [SerializeField] private List<Sprite> _superSecretSprites = new List<Sprite>();

        public void SpawnSecretSprite()
        {
            if (_superSecretSprites.Count == 0) return;

            int randomSpriteIndex = Random.Range(0, _superSecretSprites.Count - 1);
            Vector3 viewportPoint = new Vector3(Random.value, Random.value, Camera.main.nearClipPlane);

            Vector3 worldPoint = Camera.main.ViewportToWorldPoint(viewportPoint);
            worldPoint.z = 0f;

            GameObject obj = new GameObject("SecretObject");
            obj.transform.SetParent(transform);
            obj.transform.position = worldPoint;
            obj.transform.localScale = Vector3.one;
            obj.layer = LayerMask.NameToLayer("UI");

            Image image = obj.AddComponent<Image>();
            image.sprite = _superSecretSprites[randomSpriteIndex];
            image.rectTransform.sizeDelta = image.sprite.rect.size / 10;
            image.rectTransform.Rotate(0, 0, Random.Range(0, 360));
            image.raycastTarget = false;
        }
    }
}
