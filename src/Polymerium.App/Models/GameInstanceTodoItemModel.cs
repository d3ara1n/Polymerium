using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Abstractions.ExtraData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Models
{
    public class GameInstanceTodoItemModel : ObservableObject
    {
        private static void MakeEdit(GameInstanceTodoItemModel? item)
        {
            if (item != null)
                item.IsEditMode = true;
        }
        private static void MakeUnedit(GameInstanceTodoItemModel? item)
        {
            if (item != null)
                item.IsEditMode = false;
        }

        private static IRelayCommand<GameInstanceTodoItemModel> editCommand = new RelayCommand<GameInstanceTodoItemModel>(MakeEdit);
        private static IRelayCommand<GameInstanceTodoItemModel> completeEditCommand = new RelayCommand<GameInstanceTodoItemModel>(MakeUnedit);

        private bool isEditMode = false;

        public static GameInstanceTodoItemModel CreateEdit() => new GameInstanceTodoItemModel(new TodoItem("...", DateTimeOffset.Now, null)) { IsEditMode = true };

        public GameInstanceTodoItemModel(TodoItem inner) => Inner = inner;
        public TodoItem Inner { get; }
        public bool IsChecked { get => Inner.CompletedAt != null; set { Inner.CompletedAt = value ? DateTimeOffset.Now : null; OnPropertyChanged(); } }

        public bool IsEditMode
        {
            get => isEditMode;
            set
            {
                isEditMode = value;
                OnPropertyChanged();
            }
        }

        public IRelayCommand<GameInstanceTodoItemModel> EditCommand => editCommand;
        public IRelayCommand<GameInstanceTodoItemModel> CompleteEditCommand => completeEditCommand;
    }
}
