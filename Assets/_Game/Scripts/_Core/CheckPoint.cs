using UnityEngine;

namespace DatScript
{
    public class Checkpoint : MonoBehaviour
    {
        [Header("Settings")]
        public Transform spawnPointTransform;

        private bool isActivated = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isActivated)
            {
                Vector3 savePos = transform.position;

                if (spawnPointTransform != null)
                {
                    savePos = spawnPointTransform.position;
                }

                GameManager.instance.SetCheckpoint(savePos);

                isActivated = true;

                ActivateVisuals();
            }
        }

        private void ActivateVisuals()
        {
            Debug.Log("Checkpoint " + gameObject.name + " Activated!");
        }
    }
}