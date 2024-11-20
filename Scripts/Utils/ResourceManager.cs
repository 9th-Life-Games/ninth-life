using System.Collections.Generic;
using System.Linq;
using Godot;

namespace NinthLife.scripts.utils;

public static class ResourceManager
{
    private static readonly List<Resource> LoadedResources = new();

    public static T Load<T>(string path) where T : Resource
    {
        T resource = GD.Load<T>(path);
        LoadedResources.Add(resource);
        return resource;
    }

    public static void Cleanup()
    {
        foreach (Resource resource in LoadedResources.Where(resource => resource != null))
        {
            if (resource is Texture2D tex)
            {
                tex.Dispose();
            }
            else
            {
                resource.Dispose();
            }
        }

        LoadedResources.Clear();
    }
}
