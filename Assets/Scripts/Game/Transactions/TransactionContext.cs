using System;
using System.Collections.Generic;

[Serializable]
public sealed class TransactionContext
{
    public string Type;
    public Dictionary<string, object> Data = new Dictionary<string, object>();
}
