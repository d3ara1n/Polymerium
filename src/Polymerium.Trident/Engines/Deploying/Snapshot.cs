using System.Collections.ObjectModel;

namespace Polymerium.Trident.Engines.Deploying
{
    public class Snapshot : Collection<Snapshot.Entity>
    {
        public static Snapshot Take(string directory)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory {directory} not found");
            }

            var snapshot = new Snapshot();
            var subs = new Queue<DirectoryInfo>();
            subs.Enqueue(new(directory));
            while (subs.TryDequeue(out var dir))
            {
                var dirs = dir.GetDirectories();
                foreach (var d in dirs)
                {
                    subs.Enqueue(d);
                }

                var files = dir.GetFiles();
                foreach (var file in files)
                {
                    if (file.LinkTarget != null)
                    {
                        snapshot.Add(new(file.FullName, file.LinkTarget));
                    }
                }
            }

            return snapshot;
        }

        public static void Apply(string directory, IReadOnlyList<Entity> toPopulate)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var current = Take(directory);
            var entities = new List<Entity>(toPopulate);
            foreach (var exist in current)
            {
                var final = entities.FirstOrDefault(x => x.Path == exist.Path);
                if (final != null)
                {
                    if (!exist.Target.Equals(final.Target))
                    {
                        File.Delete(exist.Path);
                        File.CreateSymbolicLink(final.Path, final.Target);
                    }

                    entities.Remove(final);
                }
                else
                {
                    File.Delete(exist.Path);
                }
            }

            foreach (var remain in entities)
            {
                var dir = Path.GetDirectoryName(remain.Path);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (!File.Exists(remain.Path))
                // 有些包会内嵌文件的同时引用该附件导致文件重复
                // 这里的原则为以现有文件为准
                {
                    File.CreateSymbolicLink(remain.Path, remain.Target);
                }
            }
        }

        #region Nested type: Entity

        public record Entity(string Path, string Target);

        #endregion
    }
}
