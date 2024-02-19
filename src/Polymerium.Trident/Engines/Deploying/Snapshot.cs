using System.Collections.ObjectModel;

namespace Polymerium.Trident.Engines.Deploying
{
    public class Snapshot : Collection<Entity>
    {
        public static Snapshot Take(string directory)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory {directory} not found");
            }

            Snapshot snapshot = new();
            Queue<DirectoryInfo> subs = new();
            subs.Enqueue(new DirectoryInfo(directory));
            while (subs.TryDequeue(out DirectoryInfo? dir))
            {
                DirectoryInfo[] dirs = dir.GetDirectories();
                foreach (DirectoryInfo d in dirs)
                {
                    subs.Enqueue(d);
                }

                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    if (file.LinkTarget != null)
                    {
                        snapshot.Add(new Entity(file.FullName, file.LinkTarget));
                    }
                }
            }

            return snapshot;
        }

        public static void Populate(string directory, IList<Entity> toPopulate)
        {
            Snapshot current = Take(directory);
            Collection<Entity> entities = new(toPopulate);
            foreach (Entity exist in current)
            {
                Entity? final = entities.FirstOrDefault(x => x.Path == exist.Path);
                if (final != null)
                {
                    if (!exist.Target.Equals(final.Target, StringComparison.InvariantCultureIgnoreCase))
                    {
                        File.Delete(exist.Path);
                        File.CreateSymbolicLink(final.Path, final.Path);
                    }

                    entities.Remove(final);
                }
                else
                {
                    File.Delete(exist.Path);
                }
            }

            foreach (Entity remain in entities)
            {
                string? dir = Path.GetDirectoryName(remain.Path);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.CreateSymbolicLink(remain.Path, remain.Target);
            }
        }
    }
}