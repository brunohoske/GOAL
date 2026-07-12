package com.goal.app.blocker

import android.content.Context

/**
 * Persists the active block policy so blocking survives app kill / device reboot.
 * Simple SharedPreferences storage (a single policy at a time).
 */
object BlockPolicyStore {
    private const val PREFS = "goal_block_policy"
    private const val KEY_ENABLED = "enabled"
    private const val KEY_PACKAGES = "packages"
    private const val KEY_TARGET_PCT = "targetPct"
    private const val KEY_CURRENT_PCT = "currentPct"
    private const val KEY_XP_REMAINING = "xpRemaining"
    private const val KEY_GOAL_TITLE = "goalTitle"

    data class Policy(
        val enabled: Boolean,
        val packages: Set<String>,
        val targetPct: Int,
        val currentPct: Int,
        val xpRemaining: Int,
        val goalTitle: String,
    )

    fun save(context: Context, policy: Policy) {
        context.prefs().edit()
            .putBoolean(KEY_ENABLED, policy.enabled)
            .putStringSet(KEY_PACKAGES, policy.packages)
            .putInt(KEY_TARGET_PCT, policy.targetPct)
            .putInt(KEY_CURRENT_PCT, policy.currentPct)
            .putInt(KEY_XP_REMAINING, policy.xpRemaining)
            .putString(KEY_GOAL_TITLE, policy.goalTitle)
            .apply()
    }

    fun clear(context: Context) {
        context.prefs().edit().clear().apply()
    }

    fun load(context: Context): Policy {
        val p = context.prefs()
        return Policy(
            enabled = p.getBoolean(KEY_ENABLED, false),
            packages = p.getStringSet(KEY_PACKAGES, emptySet()) ?: emptySet(),
            targetPct = p.getInt(KEY_TARGET_PCT, 0),
            currentPct = p.getInt(KEY_CURRENT_PCT, 0),
            xpRemaining = p.getInt(KEY_XP_REMAINING, 0),
            goalTitle = p.getString(KEY_GOAL_TITLE, "") ?: "",
        )
    }

    private fun Context.prefs() = getSharedPreferences(PREFS, Context.MODE_PRIVATE)
}
