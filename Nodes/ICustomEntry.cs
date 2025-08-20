using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Screenplay.Nodes
{
    public interface ICustomEntry : IScreenplayNodeValue
    {
        void Run(HashSet<IPrerequisite> prerequisites, CancellationToken cancellation);
    }
}
