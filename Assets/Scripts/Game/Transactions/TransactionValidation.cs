using System;
using System.Collections.Generic;

[Serializable]
public sealed class TransactionValidation
{
    public bool Validated;
    public string Code;
    public Dictionary<string, object> Data = new Dictionary<string, object>();
}
