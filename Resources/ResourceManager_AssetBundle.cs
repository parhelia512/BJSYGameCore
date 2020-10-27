﻿using UnityEngine;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using JetBrains.Annotations;
using System.IO;

namespace BJSYGameCore
{
    public partial class ResourceManager
    {
        #region 公共成员
        public Object loadFromResources(string path)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 从Resources中加载资源。
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public Object loadFromResources(ResourceInfo info)
        {
            Object obj;
            if (loadFromCache(info.path, out obj))
                return obj;
            obj = Resources.Load(info.path);
            if (obj == null)
                throw new ArgumentException("info.path 路径有误，无法loadFromAssetBundle");
            saveToCache(info.path, obj);
            return obj;
        }
        /// <summary>
        /// 从AssetBundle中加载资源。
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public Object loadFromAssetBundle(ResourceInfo info)
        {
            //直接去UObject缓存找UObject
            if (!loadFromCache<Object>(info.path, out var obj))
            {
                //上一步找不到，就在AB包缓存去找info的AB包并Load出UObject
                if (!loadAssetBundleFromCache(info.bundleName, out var infoBundle))
                {
                    //上一步找不到，就去加载info的AB包，
                    //在这之前要加载依赖，在UObject缓存找manifest
                    if (!loadFromCache<AssetBundleManifest>(resourcesInfo.manifestInfo.path, out var manifest))
                    {
                        //上一步找不到，就在AB包缓存去找manifest的AB包并Load出manifest
                        if (!loadAssetBundleFromCache(resourcesInfo.manifestInfo.bundleName, out var manifestBundle))
                        {
                            manifestBundle = loadBundleFromFileOrWeb(resourcesInfo.manifestInfo.bundleName);
                            if (manifestBundle == null)
                            {
                                Debug.LogError("加载AssetBundleManifest失败");
                                return null;
                            }
                            else
                            {
                                saveBundleToCache(resourcesInfo.manifestInfo.bundleName, manifestBundle);
                            }
                        }
                        manifest = manifestBundle.LoadAsset<AssetBundleManifest>(resourcesInfo.manifestInfo.path);
                    }
                    //加载依赖项
                    var dependencies = manifest.GetDirectDependencies(info.bundleName);
                    List<string> dependenceList = null;
                    if (dependencies != null && dependencies.Length > 0)
                    {
                        foreach (var dependence in dependencies)
                        {
                            AssetBundle dependencedBundle = loadBundleFromFileOrWeb(dependence);
                            if (dependencedBundle == null)
                            {
                                Debug.LogError("加载" + info.bundleName + "的依赖项" + dependence + "失败");
                                return null;
                            }
                            else
                            {
                                //记录依赖
                                if (dependenceList == null)
                                    dependenceList = new List<string>();
                                dependenceList.Add(dependence);
                            }
                        }
                    }
                    infoBundle = loadBundleFromFileOrWeb(info.bundleName);
                    if (infoBundle == null)
                    {
                        Debug.LogError($"加载{info.path}失败");
                        return null;
                    }
                    else
                    {
                        var cacheItem = saveBundleToCache(info.bundleName, infoBundle);
                        cacheItem.dependenceList = dependenceList;
                    }
                }
                obj = infoBundle.LoadAsset(info.path);
                if (obj == null)
                {
                    Debug.LogError($"从AB包加载{info.path}失败");
                    return null;
                }
                else { saveToCache(info.path, obj); }
            }
            return obj;
        }
        public Object loadFromAssetBundle(string path)
        {
            throw new NotImplementedException();
        }
#if UNITY_EDITOR
        /// <summary>
        /// 从AssetDatabase中加载资源。
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public Object loadFromAssetDatabase(ResourceInfo info)
        {
            Object obj = null;
            if (loadFromCache<Object>(info.path, out obj)) { return obj; }
            obj = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(info.path);
            if (obj == null) { throw new ArgumentException("info.path 路径有误，无法loadFromAssetBundle"); }
            saveToCache(info.path, obj);
            return obj;
        }
#endif


        /// <summary>
        /// 从AssetBundle中加载资源。
        /// </summary>
        /// <param name="abInfo">AssetBundle相关信息</param>
        /// <param name="path">资源路径，格式为包名/资源相对路径</param>
        /// <returns></returns>
        public Object loadFromAssetBundle(ResourcesInfo abInfo, string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("资源地址不能为空", nameof(path));
            int index = path.Replace('\\', '/').IndexOf('/');
            if (index < 0)
                throw new ArgumentException(path + "不是有效的资源地址格式", nameof(path));
            string bundleName = path.Substring(0, index).ToLower();
            AssetBundleInfoItem bundleInfo = abInfo.bundleList.Find(b => b.bundleName == bundleName);
            if (bundleInfo != null)
            {
                string assetPath = path.Substring(index + 1, path.Length - index - 1);
                ResourceInfo assetInfo = bundleInfo.assetList.Find(a => a.path == assetPath);
                if (assetInfo != null)
                {
                    AssetBundle bundle = loadBundle(abInfo, bundleInfo);
                    Object asset = bundle.LoadAsset(assetInfo.assetPath);
                    saveToCache(path, asset);
                    return asset;
                }
            }
            return null;
        }
        public AssetBundle loadAssetBundle(ResourceInfo info)
        {
            throw new NotImplementedException();
        }
        #endregion
        #region 私有成员
        ResourceInfo getResourceInfo(string path)
        {
            throw new NotImplementedException();
        }
        #endregion
        AssetBundle loadBundle(ResourcesInfo bundleInfo, AssetBundleInfoItem itemInfo)
        {
            //尝试从缓存中加载
            if (loadAssetBundleFromCache(itemInfo.bundleName, out var bundle))
            {
                return bundle;
            }
            //没有缓存，先获取Bundle依赖性
            if (!loadAssetBundleFromCache(bundleInfo.manifest_old.bundleName, out var manifestBundle))
            {
                manifestBundle = loadBundleFromFileOrWeb(bundleInfo.manifest_old);
                if (manifestBundle == null)
                {
                    Debug.LogError("加载AssetBundleManifest失败");
                    return null;
                }
                saveBundleToCache(bundleInfo.manifest_old.bundleName, manifestBundle);
            }
            AssetBundleManifest manifest = manifestBundle.LoadAsset<AssetBundleManifest>(bundleInfo.manifest_old.assetList[0].assetPath);
            //加载依赖Bundle
            var dependencies = manifest.GetDirectDependencies(itemInfo.bundleName);
            List<string> dependenceList = null;
            if (dependencies != null && dependencies.Length > 0)
            {
                foreach (var dependence in dependencies)
                {
                    AssetBundle dependencedBundle = loadBundle(bundleInfo, bundleInfo.bundleList.Find(b => b.bundleName == dependence));
                    if (dependencedBundle == null)
                    {
                        Debug.LogError("加载" + itemInfo.bundleName + "的依赖项" + dependence + "失败");
                        return null;
                    }
                    else
                    {
                        //记录依赖
                        if (dependenceList == null)
                            dependenceList = new List<string>();
                        dependenceList.Add(dependence);
                    }
                }
            }
            bundle = loadBundleFromFileOrWeb(itemInfo);
            if (bundle != null)
            {
                var cacheItem = saveBundleToCache(itemInfo.bundleName, bundle);
                cacheItem.dependenceList = dependenceList;
                return bundle;
            }
            else
                return null;
        }
        AssetBundle loadBundleFromFileOrWeb(AssetBundleInfoItem info)
        {
            //TODO:AssetBundle加载的Web实现。
            return AssetBundle.LoadFromFile(info.path);
        }
        AssetBundle loadBundleFromFileOrWeb(string bundleName)
        {
            return AssetBundle.LoadFromFile(Path.Combine(resourcesInfo.bundleOutputPath, bundleName));
        }
        BundleCacheItem saveBundleToCache(string name, AssetBundle bundle)
        {
            BundleCacheItem cacheItem = new BundleCacheItem() { bundle = bundle };
            bundleCacheDic[name] = cacheItem;
            return cacheItem;
        }
        Dictionary<string, BundleCacheItem> bundleCacheDic { get; } = new Dictionary<string, BundleCacheItem>();
        [Serializable]
        class BundleCacheItem
        {
            public AssetBundle bundle = null;
            public List<string> dependenceList = null;
        }
    }
}
