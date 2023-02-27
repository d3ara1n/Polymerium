using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Polymerium.Abstractions;
using Polymerium.Core.Models.CurseForge.Eternal;
using Wupoo;

namespace Polymerium.Core.Helpers;

public static class CurseForgeHelper
{
    // NOTE: CurseForge-ApiKey 注册账号就有，再分发前请替换 ApiKey
    // TODO: Api key 都应该自 Configuration/Option 模式输入
    private const string API_KEY = "$2a$10$cjd5uExXA6oMi3lSnylNC.xsFJiujI8uQ/pV1eGltFe/hlDO2mjzm";
    private const string ENDPOINT = "https://api.curseforge.com";

    private static async Task<Option<T>> GetResourceAsync<T>(string service, string? dataKey,
        CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return Option<T>.None();
        var found = false;
        T? result = default;
        await Wapoo.Wohoo(ENDPOINT + service)
            .WithHeader("x-api-key", API_KEY)
            .ForJsonResult<JObject>(x =>
            {
                if (string.IsNullOrWhiteSpace(dataKey))
                {
                    result = x.ToObject<T>();
                    found = true;
                }
                else
                {
                    var path = dataKey.Split('.');
                    var node = x as JToken;
                    foreach (var key in path)
                        if (node is JObject obj && obj.ContainsKey(key))
                            node = obj[key];
                        else
                            break;

                    result = node!.ToObject<T>();
                    found = true;
                }
            })
            .FetchAsync();
        return found ? Option<T>.Some(result!) : Option<T>.None();
    }

    public static async Task<Option<string>> GetModDownloadUrlAsync(int projectId, int fileId,
        CancellationToken token = default)
    {
        var service = $"/v1/mods/{projectId}/files/{fileId}/download-url";
        return await GetResourceAsync<string>(service, "data", token);
    }

    public static async Task<Option<EternalModFile>> GetModFileInfoAsync(int projectId, int fileId,
        CancellationToken token = default)
    {
        var service = $"/v1/mods/{projectId}/files/{fileId}";
        return await GetResourceAsync<EternalModFile>(service, "data", token);
    }
}