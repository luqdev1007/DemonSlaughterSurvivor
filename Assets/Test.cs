using UnityEngine;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;

public class Test : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.position = new Vector3(0, 10, 0);
        UniTask task = new UniTask();
        IContainerBuilder test;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
