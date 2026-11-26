const { DataApi } = require("@unity-services/cloud-save-1.4");
const CURRENCIES_KEY = "currencies";
const STATS_KEY = "persistentStats";
const MAX_INT = 2147483647;

function isAmount(value) {
    return Number.isInteger(value) && value >= 0 && value <= MAX_INT;
}

function reply(validated, code, currencies, key, value) {
    return {
        Validated: validated,
        Code: code,
        Data: { EssenceBalance: currencies.essence, AetherBalance: currencies.aether, Key: key, Value: value }
    };
}

module.exports = async ({ params, context }) => {
    if (!context.playerId)
        throw Error("Sign in before submitting transactions.");
    if (params.type !== "PersistentStat")
        throw Error("Unknown transaction type.");
    const data = params.data;
    if (!data || typeof data !== "object" || Array.isArray(data) ||
        typeof data.key !== "string" || !/^[A-Za-z][A-Za-z0-9]{0,63}$/.test(data.key) ||
        !isAmount(data.expectedValue))
        throw Error("Supply a persistent stat key and its current level.");

    const api = new DataApi(context);
    const loaded = await api.getProtectedItems(context.projectId, context.playerId, [CURRENCIES_KEY, STATS_KEY]);
    const currencyItem = loaded.data.results.find(item => item.key === CURRENCIES_KEY);
    const statsItem = loaded.data.results.find(item => item.key === STATS_KEY);
    if (!currencyItem || !currencyItem.writeLock || !statsItem || !statsItem.writeLock)
        throw Error("Provision the player's protected currencies and persistentStats records first.");

    const currencies = currencyItem.value;
    const stats = statsItem.value;
    if (!currencies || !isAmount(currencies.essence) || !isAmount(currencies.aether) ||
        !stats || typeof stats !== "object" || Array.isArray(stats) || !Object.values(stats).every(isAmount))
        throw Error("The player's cloud currency or stat data is invalid.");
    const level = Object.prototype.hasOwnProperty.call(stats, data.key) ? stats[data.key] : 0;
    if (level !== data.expectedValue)
        return reply(false, "ValueChanged", currencies, data.key, level);

    const rules = await api.getPrivateCustomItems(context.projectId, "transactionRules", ["persistentStatUpgrades"]);
    const config = rules.data.results.find(item => item.key === "persistentStatUpgrades");
    const costs = config && config.value && Object.prototype.hasOwnProperty.call(config.value, data.key)
        ? config.value[data.key] : null;
    if (!Array.isArray(costs) || !costs.every(isAmount))
        throw Error("Configure server Essence costs for persistent stat " + data.key);
    if (level >= costs.length || level === MAX_INT)
        return reply(false, "MaxValueReached", currencies, data.key, level);
    if (currencies.essence < costs[level])
        return reply(false, "InsufficientEssence", currencies, data.key, level);

    currencies.essence -= costs[level];
    stats[data.key] = level + 1;
    // A conflict on either key rejects the entire batch: never charge without the upgrade.
    await api.setProtectedItemBatch(context.projectId, context.playerId, {
        data: [
            { key: CURRENCIES_KEY, value: currencies, writeLock: currencyItem.writeLock },
            { key: STATS_KEY, value: stats, writeLock: statsItem.writeLock }
        ]
    });
    return reply(true, "Accepted", currencies, data.key, stats[data.key]);
};

module.exports.params = {
    type: { type: "String", required: true },
    data: { type: "JSON", required: true }
};
