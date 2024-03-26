using Polymerium.App.Extensions;
using Polymerium.Trident.Accounts;
using Polymerium.Trident.Services;
using System;
using System.Windows.Input;
using Trident.Abstractions;
using Windows.UI;

namespace Polymerium.App.Models
{
    public record AccountModel
    {
        public IAccount Inner { get; }
        public Color Color1 { get; }
        public Color Color2 { get; }
        public string TypeName { get; }
        public string SkinUrl { get; }

        public Reactive<AccountManager, string?, bool> IsDefault { get; }

        public ICommand SetAsDefaultCommand { get; }
        public ICommand RemoveCommand { get; }

        public AccountModel(IAccount inner, Bindable<AccountManager, string?> defaultUuid, ICommand setAsDefault, ICommand remove)
        {
            Inner = inner;
            switch (inner)
            {
                case MicrosoftAccount:
                    Color1 = Color.FromArgb(255, 131, 158, 255);
                    Color2 = Color.FromArgb(255, 121, 255, 207);
                    TypeName = "Microsoft";
                    SkinUrl = $"https://starlightskins.lunareclipse.studio/render/default/{Inner.Uuid}/face";
                    break;
                case FamilyAccount:
                    Color1 = Color.FromArgb(255, 253, 160, 133);
                    Color2 = Color.FromArgb(255, 246, 211, 101);
                    TypeName = "Family Guy";
                    SkinUrl = $"https://starlightskins.lunareclipse.studio/render/default/{Inner.Username}/face";
                    break;
                case AuthlibAccount:
                    Color1 = Color.FromArgb(255, 251, 194, 235);
                    Color2 = Color.FromArgb(255, 166, 193, 238);
                    TypeName = "Authlib-Injector";
                    SkinUrl = "";
                    break;
                default:
                    throw new NotImplementedException();
            }
            SetAsDefaultCommand = setAsDefault;
            RemoveCommand = remove;
            IsDefault = defaultUuid.ToReactive(x => Inner.Uuid == x);
        }
    }
}