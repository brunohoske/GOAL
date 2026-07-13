package com.goal.app.blocker

import android.accessibilityservice.AccessibilityService
import android.os.Bundle
import android.view.accessibility.AccessibilityEvent
import android.view.accessibility.AccessibilityNodeInfo
import kotlin.random.Random

/**
 * Enforces the block policy in four ways:
 *  1. App blocking — a blocked app comes to the foreground (TYPE_WINDOW_STATE_CHANGED).
 *  2. Site blocking — a known browser shows a blocked domain in its URL bar
 *     (TYPE_WINDOW_CONTENT_CHANGED, throttled). Domains derive from the blocked packages.
 *  3. Shorts-only blocking — YouTube stays usable, but entering the Shorts player (app view
 *     ids) or youtube.com/shorts (browser URL) bounces the user BACK out. Insisting escalates
 *     to the overlay + home.
 *  4. Typing sabotage (chaos mode) — while active, each text change in any app has a ~7%
 *     chance of being replaced by the goal's custom message (TYPE_VIEW_TEXT_CHANGED).
 *
 * On a full block hit it shows a full-screen overlay explaining why and sends the user home.
 * Only the address bar / edited field text is inspected; nothing is stored or transmitted.
 */
class BlockerAccessibilityService : AccessibilityService() {

    companion object {
        // Allows the overlay to notify the running Flutter engine for telemetry.
        var onBlockedAppOpened: ((String) -> Unit)? = null

        private const val BROWSER_CHECK_INTERVAL_MS = 400L
        private const val TYPING_SABOTAGE_CHANCE = 7 // percent, per text-change event

        private const val SHORTS_CHECK_INTERVAL_MS = 300L
        // After a BACK, wait for the transition to settle before re-checking the tree.
        private const val SHORTS_BACK_COOLDOWN_MS = 800L
        // Repeated attempts inside this window count as "insisting"; the Nth gets the overlay.
        private const val SHORTS_STREAK_WINDOW_MS = 10_000L
        private const val SHORTS_MAX_STRIKES = 3

        private const val SHORTS_OVERLAY_TITLE = "Shorts bloqueado"
    }

    private var lastBrowserCheckAt = 0L
    private var lastShortsCheckAt = 0L
    private var shortsCooldownUntil = 0L
    private var shortsHitCount = 0
    private var lastShortsHitAt = 0L

    override fun onAccessibilityEvent(event: AccessibilityEvent?) {
        if (event == null) return
        val packageName = event.packageName?.toString() ?: return

        // Ignore our own UI.
        if (packageName == applicationContext.packageName) return

        val policy = BlockPolicyStore.load(applicationContext)
        if (!policy.hasAnyEnforcement) return

        when (event.eventType) {
            AccessibilityEvent.TYPE_WINDOW_STATE_CHANGED -> {
                if (policy.enabled && policy.packages.contains(packageName)) {
                    block(policy, packageName)
                } else if (policy.enabled) {
                    maybeBlockShorts(packageName, policy)
                    maybeBlockBrowser(packageName, policy)
                }
            }
            AccessibilityEvent.TYPE_WINDOW_CONTENT_CHANGED ->
                if (policy.enabled) {
                    maybeBlockShorts(packageName, policy)
                    maybeBlockBrowser(packageName, policy)
                }
            AccessibilityEvent.TYPE_VIEW_TEXT_CHANGED ->
                if (policy.typingSabotageEnabled) maybeSabotageTyping(event, policy)
            else -> Unit
        }
    }

    /**
     * If the goal blocks only Shorts (not the whole YouTube app), bounce the user out of the
     * Shorts player. The player's view ids only exist while a Short is on screen, so finding
     * one means the user just entered Shorts.
     */
    private fun maybeBlockShorts(packageName: String, policy: BlockPolicyStore.Policy) {
        if (packageName != ShortsBlocklist.YOUTUBE_PACKAGE) return
        if (!ShortsBlocklist.isEnabled(policy.packages)) return

        val now = System.currentTimeMillis()
        if (now < shortsCooldownUntil) return
        if (now - lastShortsCheckAt < SHORTS_CHECK_INTERVAL_MS) return
        lastShortsCheckAt = now

        val root = rootInActiveWindow ?: return
        val inShorts = ShortsBlocklist.playerViewIds.any { id ->
            try {
                root.findAccessibilityNodeInfosByViewId(id)?.isNotEmpty() == true
            } catch (_: Exception) {
                false
            }
        }
        if (inShorts) onShortsHit(policy, "${ShortsBlocklist.PSEUDO_PACKAGE}:app")
    }

    /**
     * Shared reaction for Shorts hits (app or browser): BACK keeps the user inside YouTube —
     * only the Short is denied. Insisting ([SHORTS_MAX_STRIKES] hits within the streak window)
     * escalates to the explanatory overlay + home.
     */
    private fun onShortsHit(policy: BlockPolicyStore.Policy, source: String) {
        val now = System.currentTimeMillis()
        shortsCooldownUntil = now + SHORTS_BACK_COOLDOWN_MS
        if (now - lastShortsHitAt > SHORTS_STREAK_WINDOW_MS) shortsHitCount = 0
        lastShortsHitAt = now
        shortsHitCount++

        onBlockedAppOpened?.invoke(source)
        if (shortsHitCount >= SHORTS_MAX_STRIKES) {
            shortsHitCount = 0
            BlockOverlay.show(
                applicationContext,
                policy,
                title = SHORTS_OVERLAY_TITLE,
                message = "Vídeos podem, Shorts não. Faltam ${policy.xpRemaining} XP " +
                    "em \"${policy.goalTitle}\".",
            )
            performGlobalAction(GLOBAL_ACTION_HOME)
        } else {
            performGlobalAction(GLOBAL_ACTION_BACK)
        }
    }

    /** If [packageName] is a known browser showing a blocked domain (or a Shorts URL when
     *  Shorts-only blocking is on), enforce the block. */
    private fun maybeBlockBrowser(packageName: String, policy: BlockPolicyStore.Policy) {
        val urlBarId = BrowserBlocklist.browsers[packageName] ?: return
        val domains = BrowserBlocklist.domainsFor(policy.packages)
        val shortsOnly = ShortsBlocklist.isEnabled(policy.packages)
        if (domains.isEmpty() && !shortsOnly) return

        // Content events fire constantly while browsing; check at most every 400ms.
        val now = System.currentTimeMillis()
        if (now - lastBrowserCheckAt < BROWSER_CHECK_INTERVAL_MS) return
        lastBrowserCheckAt = now

        val root = rootInActiveWindow ?: return
        val urlText = try {
            root.findAccessibilityNodeInfosByViewId(urlBarId)?.firstOrNull()?.text?.toString()
        } catch (_: Exception) {
            null
        } ?: return

        val host = BrowserBlocklist.hostOf(urlText) ?: return
        if (BrowserBlocklist.isBlockedHost(host, domains)) {
            block(policy, "$packageName:$host")
            return
        }
        if (shortsOnly && now >= shortsCooldownUntil && ShortsBlocklist.isShortsUrl(urlText)) {
            onShortsHit(policy, "${ShortsBlocklist.PSEUDO_PACKAGE}:web")
        }
    }

    /**
     * Chaos mode: on a random ~7% of keystrokes, replace the edited field's text with the
     * goal's custom message (with {xp}/{nome} already resolved by the backend). Only touches
     * editable fields; the caret jumps to the end so the user keeps fighting it.
     */
    private fun maybeSabotageTyping(event: AccessibilityEvent, policy: BlockPolicyStore.Policy) {
        val message = policy.typingSabotageText
        if (message.isBlank()) return
        if (Random.nextInt(100) >= TYPING_SABOTAGE_CHANCE) return

        val node = event.source ?: return
        if (!node.isEditable) return
        // Avoid an infinite loop: if it already shows our message, leave it.
        if (node.text?.toString() == message) return

        try {
            val args = Bundle().apply {
                putCharSequence(AccessibilityNodeInfo.ACTION_ARGUMENT_SET_TEXT_CHARSEQUENCE, message)
            }
            node.performAction(AccessibilityNodeInfo.ACTION_SET_TEXT, args)
        } catch (_: Exception) {
            // Some apps use custom inputs that reject SET_TEXT — ignore.
        }
    }

    private fun block(policy: BlockPolicyStore.Policy, source: String) {
        BlockOverlay.show(applicationContext, policy)
        onBlockedAppOpened?.invoke(source)
        performGlobalAction(GLOBAL_ACTION_HOME)
    }

    override fun onInterrupt() { /* no-op */ }
}
