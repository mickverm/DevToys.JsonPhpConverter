﻿using DevToys.Api;
using System.ComponentModel.Composition;

namespace Mickverm.DevToys.JsonPhpConverter.Resources;


[Export(typeof(IResourceAssemblyIdentifier))]
[Name(nameof(ResourceAssemblyIdentifier))]
internal sealed class ResourceAssemblyIdentifier : IResourceAssemblyIdentifier
{
    public ValueTask<FontDefinition[]> GetFontDefinitionsAsync()
    {
        throw new NotImplementedException();
    }
}
