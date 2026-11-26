using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;

public sealed class TransactionManager
{
    public Task<TransactionValidation> SubmitPersistentStatAsync(string key, int expectedValue)
    {
        return SubmitAsync(new TransactionContext
        {
            Type = "PersistentStat",
            Data = new Dictionary<string, object>
            {
                { "key", key },
                { "expectedValue", expectedValue }
            }
        });
    }

    private Task<TransactionValidation> SubmitAsync(TransactionContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (string.IsNullOrWhiteSpace(context.Type) || context.Data == null)
            throw new ArgumentException("Supply a transaction type and data.", nameof(context));
        if (UnityServices.State != ServicesInitializationState.Initialized ||
            !AuthenticationService.Instance.IsSignedIn)
            throw new InvalidOperationException("Sign in before submitting transactions.");

        return CloudCodeService.Instance.CallEndpointAsync<TransactionValidation>(
            "ProcessTransaction", new Dictionary<string, object>
            {
                { "type", context.Type },
                { "data", context.Data }
            });
    }
}
