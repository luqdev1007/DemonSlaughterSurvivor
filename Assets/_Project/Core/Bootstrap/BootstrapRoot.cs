using UnityEngine;

namespace DemonSlaughter.Core.Bootstrap
{
    public sealed class BootstrapRoot : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("BootstrapRoot Awake");

            DontDestroyOnLoad(gameObject);
        }
    }
}