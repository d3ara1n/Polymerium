using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Engines.Resolving
{
    public class ResolveResult
    {
        public ResolveResult(Attachment attachment, Exception e)
        {
            Attachment = attachment;
            IsResolvedSuccessfully = false;
            Result = null;
            Exception = new ResolveException(attachment, e);
        }

        public ResolveResult(Attachment attachment, Package package)
        {
            Attachment = attachment;
            IsResolvedSuccessfully = true;
            Result = package;
            Exception = null;
        }

        public bool IsResolvedSuccessfully { get; }

        public Package? Result { get; }

        public ResolveException? Exception { get; }
        public Attachment Attachment { get; }
    }
}