using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public static class AddressablesLoader
{
    public static async Task<T> GetAssets<T>(string key) where T : Object
    {
        var obj = Addressables.LoadAssetAsync<T>(key);
        await obj.Task;
        return obj.Result;
    }
}