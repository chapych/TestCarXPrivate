using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace Infrastructure.Services.AssetProviderService
{
    public interface IAssetProvider
    {
        Task<T> Load<T>(AssetReference assetReference) where T : class;
        Task<T[]> LoadAllByKey<T>(string label) where T : class;
        Task<T> Load<T>(string address) where T : class;
        void CleanUp();
        void Initialise();
    }
}