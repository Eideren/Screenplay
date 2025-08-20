using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Screenplay.Nodes
{
    public interface ICustomEntry : IScreenplayNodeValue
    {
        Awaitable Run(HashSet<IPrerequisite> prerequisites, CancellationToken cancellation);
    }
}
