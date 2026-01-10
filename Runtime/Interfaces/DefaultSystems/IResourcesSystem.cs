using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Azathrix.Framework.Interfaces.DefaultSystems
{
    /// <summary>
    /// 资源系统接口，扩展基础加载功能
    /// </summary>
    public interface IResourcesSystem : IResourcesLoader, ISystem
    {
        /// <summary>
        /// 实例化GameObject
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        UniTask<GameObject> InstantiateAsync(string key, Transform parent = null);
        
        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        UniTask LoadSceneAsync(string key, LoadSceneMode mode);
    }
}