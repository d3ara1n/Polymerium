﻿using Polymerium.App.Extensions;
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
        public AccountModel(IAccount inner, Bindable<AccountManager, string?> defaultUuid, ICommand setAsDefault,
            ICommand remove)
        {
            Inner = inner;
            switch (inner)
            {
                case MicrosoftAccount:
                    Color1 = Color.FromArgb(255, 131, 158, 255);
                    Color2 = Color.FromArgb(255, 121, 255, 207);
                    TypeName = "Microsoft";
                    FaceUrl = $"https://starlightskins.lunareclipse.studio/render/pixel/{Inner.Uuid}/face";
                    SkinUrl = $"https://starlightskins.lunareclipse.studio/render/default/{Inner.Uuid}/face";
                    break;
                case FamilyAccount:
                    Color1 = Color.FromArgb(255, 253, 160, 133);
                    Color2 = Color.FromArgb(255, 246, 211, 101);
                    TypeName = "Family Guy";
                    FaceUrl = $"https://starlightskins.lunareclipse.studio/render/pixel/{Inner.Username}/face";
                    SkinUrl = $"https://starlightskins.lunareclipse.studio/render/default/{Inner.Username}/face";
                    break;
                case AuthlibAccount:
                    Color1 = Color.FromArgb(255, 251, 194, 235);
                    Color2 = Color.FromArgb(255, 166, 193, 238);
                    TypeName = "Authlib";
                    FaceUrl = "";
                    SkinUrl = "";
                    break;
                default:
                    throw new NotImplementedException();
            }

            SetAsDefaultCommand = setAsDefault;
            RemoveCommand = remove;
            IsDefault = defaultUuid.ToReactive(x => Inner.Uuid == x);
        }

        public IAccount Inner { get; }
        public Color Color1 { get; }
        public Color Color2 { get; }
        public string TypeName { get; }
        public string FaceUrl { get; }
        public string SkinUrl { get; }

        public Reactive<AccountManager, string?, bool> IsDefault { get; }

        public ICommand SetAsDefaultCommand { get; }
        public ICommand RemoveCommand { get; }
    }
}