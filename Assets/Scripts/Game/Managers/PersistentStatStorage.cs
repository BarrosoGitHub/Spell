using System.Threading.Tasks;
using UnityEngine;

public abstract class PersistentStatStorage : MonoBehaviour
{
    public abstract Task<PersistentStatData> LoadAsync(string playerId);
    public abstract Task SaveAsync(string playerId, PersistentStatData data);
}
