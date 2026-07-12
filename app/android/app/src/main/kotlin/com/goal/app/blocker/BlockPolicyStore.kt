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
    private const val KEY_OVERLAY_NAG = "randomOverlay"
    private const val KEY_TYPING_NAG = "typingSabotage"
    private const val KEY_TYPING_TEXT = "typingText"

    data class Policy(
        val enabled: Boolean,
        val packages: Set<String>,
        val targetPct: Int,
        val currentPct: Int,
        val xpRemaining: Int,
        val goalTitle: String,
        val randomOverlayEnabled: Boolean = false,
        val typingSabotageEnabled: Boolean = false,
        val typingSabotageText: String = "",
    ) {
        /** True when any enforcement (app block or a nag) is active. */
        val hasAnyEnforcement: Boolean
            get() = enabled || randomOverlayEnabled || typingSabotageEnabled
    }

    fun save(context: Context, policy: Policy) {
        context.prefs().edit()
            .putBoolean(KEY_ENABLED, policy.enabled)
            .putStringSet(KEY_PACKAGES, policy.packages)
            .putInt(KEY_TARGET_PCT, policy.targetPct)
            .putInt(KEY_CURRENT_PCT, policy.currentPct)
            .putInt(KEY_XP_REMAINING, policy.xpRemaining)
            .putString(KEY_GOAL_TITLE, policy.goalTitle)
            .putBoolean(KEY_OVERLAY_NAG, policy.randomOverlayEnabled)
            .putBoolean(KEY_TYPING_NAG, policy.typingSabotageEnabled)
            .putString(KEY_TYPING_TEXT, policy.typingSabotageText)
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
            randomOverlayEnabled = p.getBoolean(KEY_OVERLAY_NAG, false),
            typingSabotageEnabled = p.getBoolean(KEY_TYPING_NAG, false),
            typingSabotageText = p.getString(KEY_TYPING_TEXT, "") ?: "",
        )
    }

    private fun Context.prefs() = getSharedPreferences(PREFS, Context.MODE_PRIVATE)
}
