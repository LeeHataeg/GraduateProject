using UnityEngine;

[DisallowMultipleComponent]
public sealed class DestroyTraceTag : MonoBehaviour
{
    [TextArea] public string killer;   // ex) SomeFile.cs::SomeFunc:123
    public string reason;              // 선택사항
    public string scene;
    public int atFrame;
    [TextArea(6, 12)] public string stack; // 호출 시점의 스택
}
