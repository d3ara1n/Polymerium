using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Polymerium.Abstractions.Accounts;

namespace Polymerium.App.Data;

public class AccountModel : RefinedModelBase<IGameAccount>
{
    private readonly byte[] POLY_SIGNED = { 114, 5, 14, 191, 98, 10 };
    public override Uri Location { get; } = new("poly-file:///accounts.json");

    public string TypeName { get; set; }
    public byte[] Juice { get; set; }

    public override void Apply(IGameAccount data)
    {
        TypeName = data.GetType().AssemblyQualifiedName;
        var text = JsonConvert.SerializeObject(data);
        Juice = ProtectedData.Protect(Encoding.UTF8.GetBytes(text), POLY_SIGNED, DataProtectionScope.CurrentUser);
    }

    public override IGameAccount Extract()
    {
        var data = ProtectedData.Unprotect(Juice, POLY_SIGNED, DataProtectionScope.CurrentUser);
        var json = Encoding.UTF8.GetString(data);
        var obj = Type.GetType(TypeName);
        var instance = Activator.CreateInstance(obj) as IGameAccount;
        JsonConvert.PopulateObject(json, instance);
        return instance;
    }
}