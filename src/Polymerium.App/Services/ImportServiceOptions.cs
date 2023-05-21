using System;
using System.Collections.Generic;
using Polymerium.Abstractions.Importers;

namespace Polymerium.App.Services;

public class ImportServiceOptions
{
    internal IDictionary<string, Type> ImporterTypes { get; } = new Dictionary<string, Type>();

    public ImportServiceOptions Register<T>(string indexFileName)
        where T : ImporterBase
    {
        return Register(indexFileName, typeof(T));
    }

    public ImportServiceOptions Register(string indexFileName, Type type)
    {
        if (ImporterTypes.ContainsKey(indexFileName))
            throw new ArgumentException(
                "Importer with the index file name has been already registered"
            );
        if (!type.IsAssignableTo(typeof(ImporterBase)))
            throw new ArgumentException("Type is not a sub class of ImporterBase");
        ImporterTypes.Add(indexFileName, type);
        return this;
    }
}