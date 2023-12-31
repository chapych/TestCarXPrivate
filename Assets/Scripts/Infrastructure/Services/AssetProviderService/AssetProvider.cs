﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Infrastructure.Services.AssetProviderService
{
    public class AssetProvider : IAssetProvider
    {
        private Dictionary<string, AsyncOperationHandle> m_cashedHandlers = new Dictionary<string, AsyncOperationHandle>();
        private Dictionary<string, AsyncOperationHandle<IList<IResourceLocation>>> m_cashedLocations = new Dictionary<string, AsyncOperationHandle<IList<IResourceLocation>>>();
        private List<AsyncOperationHandle> m_handles = new List<AsyncOperationHandle>();

        public void Initialise()
        {
            Addressables.InitializeAsync();
        }
        public async Task<T> Load<T>(AssetReference assetReference) where T : class
        {
            if (m_cashedHandlers.TryGetValue(assetReference.AssetGUID, out AsyncOperationHandle asyncHandle))
                return asyncHandle.Result as T;
            var handle = Addressables.LoadAssetAsync<T>(assetReference);

            return await LoadAssetWIthCashed(assetReference.AssetGUID, handle);
        }

        public async Task<T> Load<T>(string address) where T : class
        {
            if (m_cashedHandlers.TryGetValue(address, out AsyncOperationHandle asyncHandle))
                return asyncHandle.Result as T;

            var handle = Addressables.LoadAssetAsync<T>(address);

            return await LoadAssetWIthCashed(address, handle);
        }

        public async Task<T[]> LoadAllByKey<T>(string label) where T : class
        {
            var locations = await GetResourceLocations(label);
            var toLoad = locations.Where(x=>x.ResourceType == typeof(T))
                .Select(x => Load<T>(x.PrimaryKey));

            return await Task.WhenAll(toLoad);
        }

        private async Task<IList<IResourceLocation>> GetResourceLocations(string label)
        {
            if (m_cashedLocations.TryGetValue(label, out var operationHandle))
                return operationHandle.Result as IList<IResourceLocation>;
            else
            {
                operationHandle = Addressables.LoadResourceLocationsAsync(label);
                return await LoadLocationWithCashed(label, operationHandle);
            }
        }

        private async Task<IList<IResourceLocation>> LoadLocationWithCashed(string label, AsyncOperationHandle<IList<IResourceLocation>> operationHandle)
        {
            operationHandle.Completed += h => { m_cashedLocations[label] = h; };
            m_handles.Add(operationHandle);

            return await operationHandle.Task;
        }

        private async Task<T> LoadAssetWIthCashed<T>(string address, AsyncOperationHandle<T> handle) where T : class
        {
            handle.Completed += h => { m_cashedHandlers[address] = h; };
            m_handles.Add(handle);

            return await handle.Task;
        }

        public void CleanUp()
        {
            foreach (AsyncOperationHandle handle in m_handles) Addressables.Release(handle);

            foreach (var locations in m_cashedLocations.Values)
                Addressables.Release(locations);

            m_cashedHandlers.Clear();
            m_cashedLocations.Clear();
            m_handles.Clear();
        }
    }
}