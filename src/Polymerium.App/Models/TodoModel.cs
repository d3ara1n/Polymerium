using Polymerium.App.Extensions;
using Trident.Abstractions;

namespace Polymerium.App.Models
{
    public record TodoModel
    {
        public TodoModel(Profile.RecordData.Todo inner)
        {
            Inner = inner;
            Completed = inner.ToBindable(x => x.Completed, (x, v) => x.Completed = v);
            Text = inner.ToBindable(x => x.Text, (x, v) => x.Text = v);
        }

        public Profile.RecordData.Todo Inner { get; }
        public Bindable<Profile.RecordData.Todo, bool> Completed { get; }
        public Bindable<Profile.RecordData.Todo, string> Text { get; }
    }
}