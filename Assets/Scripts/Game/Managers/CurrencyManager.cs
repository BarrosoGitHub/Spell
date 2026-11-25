using UnityEngine;

public sealed class CurrencyManager : MonoBehaviour
{
    [SerializeField] private EssenceCurrency essence = new EssenceCurrency();

    public EssenceCurrency Essence => essence;
}
