const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");
const { test } = require("node:test");

const source = fs.readFileSync(path.join(__dirname, "../ProcessTransaction.js"), "utf8");
const clone = value => JSON.parse(JSON.stringify(value));
const caller = { projectId: "project", playerId: "player" };
const upgrade = { type: "UpgradeSkill", data: { skillId: "slow", expectedLevel: 1 } };

function fixture(overrides = {}, costs = [10, 25]) {
    let state = overrides === null ? null : {
        essence: 100, aether: 7, skills: [{ id: "slow", level: 1 }], values: [{ id: "maxHealth", value: 0 }], ...overrides
    };
    let version = 1;
    let writes = 0;
    let loseResponse = false;
    const api = {
        async getProtectedItems(projectId, playerId, keys) {
            assert.equal(projectId, caller.projectId);
            assert.equal(playerId, caller.playerId);
            assert.equal(keys[0], "transactionState");
            return { data: { results: state ? [{ key: keys[0], value: clone(state), writeLock: String(version) }] : [] } };
        },
        async getPrivateCustomItems(projectId, customId, keys) {
            assert.equal(customId, "transactionRules");
            assert.ok(["skillUpgrades", "valueUpgrades"].includes(keys[0]));
            const rules = keys[0] === "skillUpgrades"
                ? { skills: [{ id: "slow", essenceCosts: costs }] }
                : { values: [{ id: "maxHealth", essenceCosts: costs }] };
            return { data: { results: [{ key: keys[0], value: rules }] } };
        },
        async setProtectedItem(projectId, playerId, item) {
            if (item.writeLock !== String(version))
                throw Error("Write conflict");
            state = clone(item.value);
            version++;
            writes++;
            if (loseResponse) {
                loseResponse = false;
                throw Error("Response lost");
            }
        }
    };
    const sandbox = { module: { exports: {} }, require(name) {
        assert.equal(name, "@unity-services/cloud-save-1.4");
        return { DataApi: class { constructor(context) {
            assert.equal(context, caller);
            return api;
        } } };
    } };
    vm.runInNewContext(source, sandbox);
    return {
        submit: (params = upgrade, context = caller) => sandbox.module.exports({ params, context }),
        state: () => clone(state), writes: () => writes,
        loseResponse: () => { loseResponse = true; }
    };
}

test("server price determines the atomic Essence deduction and level increase", async () => {
    const f = fixture();
    const result = await f.submit({ ...upgrade, data: { ...upgrade.data, cost: 0, playerId: "other", level: 999 } });
    assert.equal(result.Validated, true);
    assert.equal(result.Data.EssenceBalance, 90);
    assert.equal(result.Data.AetherBalance, 7);
    assert.equal(result.Data.Level, 2);
    assert.equal(f.state().skills[0].level, 2);
    await f.submit({ ...upgrade, data: { ...upgrade.data, expectedLevel: 2 } });
    assert.equal(f.state().essence, 65);
    assert.equal(f.state().skills[0].level, 3);
});

test("same expected level cannot charge twice, including after a lost response", async () => {
    const f = fixture();
    f.loseResponse();
    await assert.rejects(f.submit(), /Response lost/);
    const retry = await f.submit();
    assert.equal(retry.Validated, false);
    assert.equal(retry.Code, "LevelChanged");
    assert.equal(retry.Data.Level, 2);
    assert.equal(retry.Data.EssenceBalance, 90);
    assert.equal(f.writes(), 1);
});

test("concurrent upgrades cannot overwrite each other or charge twice", async () => {
    const f = fixture();
    const results = await Promise.allSettled([f.submit(), f.submit()]);
    assert.equal(results.filter(result => result.status === "fulfilled").length, 1);
    assert.equal(f.writes(), 1);
    assert.equal(f.state().essence, 90);
    assert.equal(f.state().skills[0].level, 2);
});

test("unowned, max-level, unaffordable and stale upgrades do not change state", async () => {
    for (const [overrides, params, code] of [
        [{}, { ...upgrade, data: { ...upgrade.data, skillId: "other" } }, "SkillNotOwned"],
        [{ skills: [{ id: "slow", level: 3 }] }, { ...upgrade, data: { ...upgrade.data, expectedLevel: 3 } }, "MaxLevelReached"],
        [{ essence: 9 }, upgrade, "InsufficientEssence"],
        [{}, { ...upgrade, data: { ...upgrade.data, expectedLevel: 2 } }, "LevelChanged"]
    ]) {
        const f = fixture(overrides);
        const before = f.state();
        const result = await f.submit(params);
        assert.equal(result.Validated, false);
        assert.equal(result.Code, code);
        assert.deepEqual(f.state(), before);
        assert.equal(f.writes(), 0);
    }
});

test("GetState returns a snapshot without skill-specific input or saves", async () => {
    const f = fixture();
    const result = await f.submit({ type: "GetState", data: {} });
    assert.equal(result.Validated, true);
    assert.equal(result.Data.Skills[0].level, 1);
    assert.equal(result.Data.EssenceBalance, 100);
    assert.equal(f.writes(), 0);
});

test("invalid input, account data and cost configuration fail without saves", async () => {
    const f = fixture();
    await assert.rejects(f.submit(upgrade, {}), /Sign in/);
    await assert.rejects(f.submit({ ...upgrade, type: "AddCurrency" }), /Unknown/);
    await assert.rejects(f.submit({ ...upgrade, data: { ...upgrade.data, skillId: "bad/id" } }), /skill ID/);
    await assert.rejects(f.submit({ ...upgrade, data: { ...upgrade.data, expectedLevel: 1.5 } }), /current level/);
    await assert.rejects(fixture(null).submit(), /Provision/);
    await assert.rejects(fixture({ essence: -1 }).submit(), /invalid/);
    await assert.rejects(fixture({}, [-1]).submit(), /Configure/);
    assert.equal(f.writes(), 0);
});


test("persistent value upgrades use the same envelope with different data and results", async () => {
    const f = fixture();
    const params = { type: "UpgradeValue", data: { key: "maxHealth", expectedValue: 0 } };
    const result = await f.submit(params);
    assert.equal(result.Validated, true);
    assert.equal(result.Data.Key, "maxHealth");
    assert.equal(result.Data.Value, 1);
    assert.equal(result.Data.EssenceBalance, 90);
    assert.equal(result.Data.Level, undefined);
    assert.equal(f.state().skills[0].level, 1);
    assert.equal(f.state().values[0].value, 1);
    assert.equal((await f.submit(params)).Code, "ValueChanged");
    assert.equal(f.writes(), 1);
});

test("invalid, missing, maxed or unaffordable persistent upgrades never save", async () => {
    const params = { type: "UpgradeValue", data: { key: "maxHealth", expectedValue: 0 } };
    for (const [overrides, request, code] of [
        [{}, { ...params, data: { key: "other", expectedValue: 0 } }, "ValueNotFound"],
        [{ essence: 9 }, params, "InsufficientEssence"],
        [{ values: [{ id: "maxHealth", value: 2 }] },
            { ...params, data: { key: "maxHealth", expectedValue: 2 } }, "MaxValueReached"]
    ]) {
        const f = fixture(overrides);
        assert.equal((await f.submit(request)).Code, code);
        assert.equal(f.writes(), 0);
    }
    await assert.rejects(fixture().submit({ ...params, data: { key: "maxHealth", expectedValue: -1 } }), /current value/);
});

test("the common protocol rejects non-object payloads", async () => {
    const f = fixture();
    for (const data of [null, [], "payload"])
        await assert.rejects(f.submit({ type: "GetState", data }), /JSON object/);
    assert.equal(f.writes(), 0);
});
