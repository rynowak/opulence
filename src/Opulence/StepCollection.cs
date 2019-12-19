using System.Collections.ObjectModel;

namespace Opulence
{
    public sealed class StepCollection : Collection<Step>
    {
        public T? GetT<T>() where T : Step
        {
            for (var i = 0; i < Count; i++)
            {
                var item = Items[i];
                if (item is T step)
                {
                    return step;
                }
            }

            return null;
        }
    }
}